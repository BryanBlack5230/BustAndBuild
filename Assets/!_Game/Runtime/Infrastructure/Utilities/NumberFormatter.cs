#nullable enable
using System;
using System.Globalization;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    /// <summary>
    /// Abbreviates large numbers for UI (1500 -> "1.5K") and parses them back.
    /// Rounding rule: values the player OWNS round down (never show more than they have);
    /// COSTS and targets round up (never show less than is required). This holds for
    /// negative values too — rounding down goes toward negative infinity.
    /// Adapted from UGFW (MIT, © 2026 MAK).
    /// </summary>
    public static class NumberFormatter
    {
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Q" };

        /// <summary>Formats a number as e.g. "1.5K", "2.5M", "1B". Trailing zeros are trimmed ("2.0K" -> "2K").</summary>
        /// <param name="number">The number to format.</param>
        /// <param name="maxDecimals">Maximum decimal places to show.</param>
        /// <param name="roundDown">True (default) floors the shown value — use for owned resources. False ceils — use for costs.</param>
        public static string FormatAbbreviated(this int number, int maxDecimals = 1, bool roundDown = true)
            => FormatAbbreviated((long)number, maxDecimals, roundDown);

        /// <inheritdoc cref="FormatAbbreviated(int,int,bool)"/>
        public static string FormatAbbreviated(this long number, int maxDecimals = 1, bool roundDown = true)
        {
            if (number > -1000 && number < 1000)
                return number.ToString(CultureInfo.InvariantCulture);

            return FormatAbbreviated((double)number, maxDecimals, roundDown);
        }

        /// <inheritdoc cref="FormatAbbreviated(int,int,bool)"/>
        public static string FormatAbbreviated(this float number, int maxDecimals = 1, bool roundDown = true)
            => FormatAbbreviated((double)number, maxDecimals, roundDown);

        /// <inheritdoc cref="FormatAbbreviated(int,int,bool)"/>
        public static string FormatAbbreviated(this double number, int maxDecimals = 1, bool roundDown = true)
        {
            if (double.IsNaN(number) || double.IsInfinity(number))
                throw new ArgumentException($"Cannot format invalid value {number}", nameof(number));
            if (maxDecimals < 0)
                throw new ArgumentException($"maxDecimals must be non-negative, got {maxDecimals}", nameof(maxDecimals));

            double abs = Math.Abs(number);

            // Below 1000: always whole numbers, no suffix.
            if (abs < 1000)
            {
                double whole = roundDown ? Math.Floor(number) : Math.Ceiling(number);
                return whole.ToString("0", CultureInfo.InvariantCulture);
            }

            int suffixIndex = 0;
            double scaled = abs;
            while (scaled >= 1000 && suffixIndex < Suffixes.Length - 1)
            {
                scaled /= 1000;
                suffixIndex++;
            }

            // Floor/ceil the SIGNED value at maxDecimals so the displayed value never
            // over/under-states (flooring a negative value goes more negative).
            if (number < 0)
                scaled = -scaled;
            double factor = Math.Pow(10, maxDecimals);
            scaled = roundDown ? Math.Floor(scaled * factor) / factor : Math.Ceiling(scaled * factor) / factor;

            // Ceiling can land exactly on the next unit (999999 -> "1M", not "1000K").
            if (Math.Abs(scaled) >= 1000 && suffixIndex < Suffixes.Length - 1)
            {
                scaled /= 1000;
                suffixIndex++;
            }

            string format = maxDecimals > 0 ? "0." + new string('#', maxDecimals) : "0";
            string formatted = scaled.ToString(format, CultureInfo.InvariantCulture);
            return $"{formatted}{Suffixes[suffixIndex]}";
        }

        /// <summary>Parses "1.5K" -> 1500, "2.5M" -> 2500000, "42" -> 42. Throws on unparsable input.</summary>
        public static long ParseAbbreviated(string input)
        {
            if (!TryParseAbbreviated(input, out long value))
                throw new FormatException($"Cannot parse abbreviated number \"{input}\"");

            return value;
        }

        /// <inheritdoc cref="ParseAbbreviated"/>
        public static bool TryParseAbbreviated(string? input, out long value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string text = input!.Trim().ToUpperInvariant();

            int suffixIndex = 0;
            string numberPart = text;
            for (int i = Suffixes.Length - 1; i >= 1; i--)
            {
                if (text.EndsWith(Suffixes[i], StringComparison.Ordinal))
                {
                    suffixIndex = i;
                    numberPart = text.Substring(0, text.Length - Suffixes[i].Length);
                    break;
                }
            }

            if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                return false;

            value = (long)(number * Math.Pow(1000, suffixIndex));
            return true;
        }
    }
}
