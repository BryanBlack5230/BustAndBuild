#nullable enable
using System;
using System.Globalization;

namespace BarkingBird.Runtime.Infrastructure.Formulas
{
    /// <summary>
    /// Recursive-descent parser for balance formulas.
    /// Supports + - * / ^, parentheses, {i} parameter tokens, constants (pi, e)
    /// and functions: sqrt, abs, sin, cos, tan, log, floor, ceil, round,
    /// min, max, pow, clamp.
    /// Unary minus binds tighter than '^': -2^2 evaluates to 4.
    /// </summary>
    public static class FormulaParser
    {
        public static bool TryEvaluate(string formula, double[]? parameters, out double result, out string? error)
        {
            result = 0;
            error = null;

            if (string.IsNullOrEmpty(formula))
            {
                error = "Empty formula";
                return false;
            }

            try
            {
                var state = new ParserState(formula, parameters ?? Array.Empty<double>());
                result = state.ParseExpression();
                state.SkipWhitespace();

                if (state.Pos < state.Input.Length)
                {
                    error = $"Unexpected character '{state.Input[state.Pos]}' at position {state.Pos}";
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static double Evaluate(string formula, double[]? parameters)
        {
            if (!TryEvaluate(formula, parameters, out var result, out var error))
                throw new FormulaParseException(error!);
            return result;
        }

        /// <summary>
        /// Highest {i} token index used by the formula, or -1 when none.
        /// </summary>
        public static int GetMaxSlotIndex(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return -1;

            var max = -1;
            var i = 0;
            while (i < formula.Length)
            {
                if (formula[i] == '{')
                {
                    var end = formula.IndexOf('}', i + 1);
                    if (end > i + 1 && int.TryParse(formula.Substring(i + 1, end - i - 1), out var index) && index > max)
                        max = index;

                    i = end >= 0 ? end + 1 : i + 1;
                }
                else
                {
                    i++;
                }
            }

            return max;
        }

        private struct ParserState
        {
            public readonly string Input;
            public int Pos;

            private readonly double[] _parameters;

            public ParserState(string input, double[] parameters)
            {
                Input = input;
                _parameters = parameters;
                Pos = 0;
            }

            public void SkipWhitespace()
            {
                while (Pos < Input.Length && char.IsWhiteSpace(Input[Pos]))
                    Pos++;
            }

            private char Peek()
            {
                SkipWhitespace();
                return Pos < Input.Length ? Input[Pos] : '\0';
            }

            private char Advance()
            {
                var c = Input[Pos];
                Pos++;
                return c;
            }

            public double ParseExpression()
            {
                var left = ParseTerm();
                while (true)
                {
                    var c = Peek();
                    if (c == '+') { Advance(); left += ParseTerm(); }
                    else if (c == '-') { Advance(); left -= ParseTerm(); }
                    else break;
                }

                return left;
            }

            private double ParseTerm()
            {
                var left = ParsePower();
                while (true)
                {
                    var c = Peek();
                    if (c == '*') { Advance(); left *= ParsePower(); }
                    else if (c == '/') { Advance(); left /= ParsePower(); }
                    else break;
                }

                return left;
            }

            private double ParsePower()
            {
                var baseValue = ParseUnary();
                if (Peek() == '^')
                {
                    Advance();
                    var exponent = ParsePower();
                    return Math.Pow(baseValue, exponent);
                }

                return baseValue;
            }

            private double ParseUnary()
            {
                if (Peek() == '-')
                {
                    Advance();
                    return -ParseUnary();
                }

                if (Peek() == '+')
                {
                    Advance();
                    return ParseUnary();
                }

                return ParseAtom();
            }

            private double ParseAtom()
            {
                SkipWhitespace();
                if (Pos >= Input.Length)
                    throw new FormulaParseException("Unexpected end of expression");

                var c = Input[Pos];

                if (char.IsDigit(c) || c == '.')
                    return ParseNumber();

                if (c == '{')
                    return ParseParameter();

                if (c == '(')
                {
                    Advance();
                    var value = ParseExpression();
                    SkipWhitespace();
                    if (Pos >= Input.Length || Input[Pos] != ')')
                        throw new FormulaParseException("Missing closing parenthesis");
                    Advance();
                    return value;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    var name = ParseIdentifier();
                    SkipWhitespace();

                    if (Pos < Input.Length && Input[Pos] == '(')
                    {
                        Advance();
                        return ParseFunctionCall(name);
                    }

                    return ResolveConstant(name);
                }

                throw new FormulaParseException($"Unexpected character '{c}' at position {Pos}");
            }

            private double ParseNumber()
            {
                var start = Pos;
                while (Pos < Input.Length && (char.IsDigit(Input[Pos]) || Input[Pos] == '.'))
                    Pos++;

                var numberText = Input.Substring(start, Pos - start);
                if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    throw new FormulaParseException($"Invalid number '{numberText}' at position {start}");
                return value;
            }

            private double ParseParameter()
            {
                var start = Pos;
                Advance();
                var digitsStart = Pos;
                while (Pos < Input.Length && char.IsDigit(Input[Pos]))
                    Pos++;

                if (Pos == digitsStart || Pos >= Input.Length || Input[Pos] != '}')
                    throw new FormulaParseException($"Malformed parameter token at position {start}");

                if (!int.TryParse(Input.Substring(digitsStart, Pos - digitsStart),
                        NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                    throw new FormulaParseException($"Malformed parameter token at position {start}");
                Advance();

                if (index >= _parameters.Length)
                    throw new FormulaParseException($"No value bound to parameter {{{index}}}");

                return _parameters[index];
            }

            private string ParseIdentifier()
            {
                var start = Pos;
                while (Pos < Input.Length && (char.IsLetterOrDigit(Input[Pos]) || Input[Pos] == '_'))
                    Pos++;
                return Input.Substring(start, Pos - start).ToLowerInvariant();
            }

            private double ParseFunctionCall(string name)
            {
                var args = new double[4];
                var argCount = 0;

                if (Peek() != ')')
                {
                    args[argCount++] = ParseExpression();
                    while (Peek() == ',')
                    {
                        Advance();
                        if (argCount >= args.Length)
                            throw new FormulaParseException($"Too many arguments for function '{name}'");
                        args[argCount++] = ParseExpression();
                    }
                }

                SkipWhitespace();
                if (Pos >= Input.Length || Input[Pos] != ')')
                    throw new FormulaParseException($"Missing closing parenthesis for function '{name}'");
                Advance();

                return EvalFunction(name, args, argCount);
            }

            private static double EvalFunction(string name, double[] args, int count)
            {
                switch (name)
                {
                    case "sqrt":
                        Expect(name, count, 1);
                        return Math.Sqrt(args[0]);
                    case "abs":
                        Expect(name, count, 1);
                        return Math.Abs(args[0]);
                    case "sin":
                        Expect(name, count, 1);
                        return Math.Sin(args[0]);
                    case "cos":
                        Expect(name, count, 1);
                        return Math.Cos(args[0]);
                    case "tan":
                        Expect(name, count, 1);
                        return Math.Tan(args[0]);
                    case "log":
                        Expect(name, count, 1);
                        return Math.Log(args[0]);
                    case "floor":
                        Expect(name, count, 1);
                        return Math.Floor(args[0]);
                    case "ceil":
                        Expect(name, count, 1);
                        return Math.Ceiling(args[0]);
                    case "round":
                        Expect(name, count, 1);
                        return Math.Round(args[0]);
                    case "min":
                        Expect(name, count, 2);
                        return Math.Min(args[0], args[1]);
                    case "max":
                        Expect(name, count, 2);
                        return Math.Max(args[0], args[1]);
                    case "pow":
                        Expect(name, count, 2);
                        return Math.Pow(args[0], args[1]);
                    case "clamp":
                        Expect(name, count, 3);
                        return Math.Max(args[1], Math.Min(args[2], args[0]));
                    default:
                        throw new FormulaParseException($"Unknown function '{name}'");
                }
            }

            private static void Expect(string functionName, int actual, int expected)
            {
                if (actual != expected)
                    throw new FormulaParseException(
                        $"Function '{functionName}' expects {expected} argument(s), got {actual}");
            }

            private static double ResolveConstant(string name)
            {
                switch (name)
                {
                    case "pi": return Math.PI;
                    case "e": return Math.E;
                    default:
                        throw new FormulaParseException($"Unknown constant '{name}'");
                }
            }
        }
    }

    public class FormulaParseException : Exception
    {
        public FormulaParseException(string message) : base(message)
        {
        }
    }
}
