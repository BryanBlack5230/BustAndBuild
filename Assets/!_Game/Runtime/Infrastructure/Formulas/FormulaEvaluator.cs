#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BarkingBird.Runtime.Infrastructure.Formulas
{
    /// <summary>
    /// Evaluates a formula string by binding its {i} slot tokens to numeric members
    /// (field, property or parameterless method) of an owner object via reflection.
    /// Member accessors are cached per (type, member name).
    /// Intended for config-time balance math (Wealth, Danger, costs) — not for
    /// per-frame hot paths.
    /// </summary>
    public static class FormulaEvaluator
    {
        private const BindingFlags AllMembers =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.FlattenHierarchy;

        private const BindingFlags DeclaredHiddenMembers =
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.DeclaredOnly;

        private static readonly HashSet<Type> NumericTypes = new()
        {
            typeof(int), typeof(float), typeof(double), typeof(long),
            typeof(decimal), typeof(byte), typeof(short),
            typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte)
        };

        private enum MemberKind : byte { Field, Property, Method }

        private readonly struct MemberAccessor
        {
            private readonly MemberKind _kind;
            private readonly FieldInfo? _field;
            private readonly PropertyInfo? _property;
            private readonly MethodInfo? _method;
            private readonly bool _isStatic;

            public MemberAccessor(FieldInfo field)
            {
                _kind = MemberKind.Field;
                _field = field;
                _property = null;
                _method = null;
                _isStatic = field.IsStatic;
            }

            public MemberAccessor(PropertyInfo property)
            {
                _kind = MemberKind.Property;
                _field = null;
                _property = property;
                _method = null;
                _isStatic = property.GetGetMethod(true)!.IsStatic;
            }

            public MemberAccessor(MethodInfo method)
            {
                _kind = MemberKind.Method;
                _field = null;
                _property = null;
                _method = method;
                _isStatic = method.IsStatic;
            }

            public double Read(object owner)
            {
                var target = _isStatic ? null : owner;
                switch (_kind)
                {
                    case MemberKind.Field: return Convert.ToDouble(_field!.GetValue(target));
                    case MemberKind.Property: return Convert.ToDouble(_property!.GetValue(target));
                    case MemberKind.Method: return Convert.ToDouble(_method!.Invoke(target, null));
                    default: return 0;
                }
            }
        }

        private static readonly Dictionary<(Type, string), MemberAccessor> AccessorCache = new();

        public static double Evaluate(string formula, FormulaSlot[] slots, object owner)
        {
            var parameters = ResolveParameters(slots, owner);
            return FormulaParser.Evaluate(formula, parameters);
        }

        public static bool TryEvaluate(string formula, FormulaSlot[] slots, object owner,
            out double result, out string? error)
        {
            result = 0;
            error = null;

            try
            {
                var parameters = ResolveParameters(slots, owner);
                return FormulaParser.TryEvaluate(formula, parameters, out result, out error);
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        private static double[] ResolveParameters(FormulaSlot[] slots, object owner)
        {
            if (slots == null || slots.Length == 0)
                return Array.Empty<double>();

            var type = owner.GetType();
            var parameters = new double[slots.Length];

            for (var i = 0; i < slots.Length; i++)
            {
                var name = slots[i].MemberName;
                if (string.IsNullOrEmpty(name))
                    continue;
                parameters[i] = ReadCached(type, owner, name);
            }

            return parameters;
        }

        private static double ReadCached(Type type, object owner, string name)
        {
            var key = (type, name);
            if (!AccessorCache.TryGetValue(key, out var accessor))
            {
                accessor = BuildAccessor(type, name);
                AccessorCache[key] = accessor;
            }

            return accessor.Read(owner);
        }

        private static MemberAccessor BuildAccessor(Type type, string name)
        {
            var field = FindField(type, name);
            if (field != null && IsNumeric(field.FieldType))
                return new MemberAccessor(field);

            var property = FindProperty(type, name);
            if (property != null && property.CanRead && IsNumeric(property.PropertyType))
                return new MemberAccessor(property);

            var method = FindMethod(type, name);
            if (method != null && method.GetParameters().Length == 0 && IsNumeric(method.ReturnType))
                return new MemberAccessor(method);

            throw new InvalidOperationException($"Numeric member '{name}' not found on type '{type.Name}'");
        }

        // FlattenHierarchy does not surface private members of base types,
        // so each lookup falls back to walking the base chain with DeclaredOnly.
        private static FieldInfo? FindField(Type type, string name)
        {
            var result = type.GetField(name, AllMembers);
            if (result != null)
                return result;

            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                result = baseType.GetField(name, DeclaredHiddenMembers);
                if (result != null)
                    return result;
                baseType = baseType.BaseType;
            }

            return null;
        }

        private static PropertyInfo? FindProperty(Type type, string name)
        {
            var result = type.GetProperty(name, AllMembers);
            if (result != null)
                return result;

            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                result = baseType.GetProperty(name, DeclaredHiddenMembers);
                if (result != null)
                    return result;
                baseType = baseType.BaseType;
            }

            return null;
        }

        private static MethodInfo? FindMethod(Type type, string name)
        {
            var result = type.GetMethod(name, AllMembers, null, Type.EmptyTypes, null);
            if (result != null)
                return result;

            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                result = baseType.GetMethod(name, DeclaredHiddenMembers, null, Type.EmptyTypes, null);
                if (result != null)
                    return result;
                baseType = baseType.BaseType;
            }

            return null;
        }

        private static bool IsNumeric(Type type) => NumericTypes.Contains(type);
    }
}
