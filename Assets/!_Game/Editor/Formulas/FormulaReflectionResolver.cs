#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BarkingBird.Editor
{
    /// <summary>
    /// Collects the numeric members (fields, properties, parameterless methods)
    /// of a type for the formula slot dropdown, grouped into popup categories.
    /// </summary>
    internal static class FormulaReflectionResolver
    {
        internal sealed class ResolvedMembers
        {
            public readonly string[] PopupKeys;
            public readonly string[] MemberNames;

            public ResolvedMembers(string[] popupKeys, string[] memberNames)
            {
                PopupKeys = popupKeys;
                MemberNames = memberNames;
            }
        }

        private enum MemberCategory
        {
            Field,
            Property,
            Method,
            Constant
        }

        private enum MemberOrigin
        {
            Own,
            Inherited
        }

        private struct MemberEntry
        {
            public string Name;
            public MemberCategory Category;
            public MemberOrigin Origin;
            public Type ReturnType;
        }

        private const BindingFlags DeclaredMembers =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.DeclaredOnly;

        private static readonly HashSet<Type> NumericTypes = new()
        {
            typeof(int), typeof(float), typeof(double), typeof(long),
            typeof(decimal), typeof(byte), typeof(short),
            typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte)
        };

        private static readonly Dictionary<Type, ResolvedMembers> Cache = new();

        internal static ResolvedMembers Resolve(Type type)
        {
            if (Cache.TryGetValue(type, out var cached))
                return cached;

            var entries = CollectMembers(type);
            var keys = new string[entries.Count];
            var names = new string[entries.Count];

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var category = entry.Category == MemberCategory.Constant
                    ? "Constants"
                    : $"{entry.Category}s — {(entry.Origin == MemberOrigin.Own ? "Own" : "Inherited")}";
                keys[i] = $"{category}/{entry.Name} : {entry.ReturnType.Name}";
                names[i] = entry.Name;
            }

            var result = new ResolvedMembers(keys, names);
            Cache[type] = result;
            return result;
        }

        internal static int FindMemberIndex(string[] memberNames, string memberName)
        {
            if (string.IsNullOrEmpty(memberName))
                return -1;

            for (var i = 0; i < memberNames.Length; i++)
            {
                if (memberNames[i] == memberName)
                    return i;
            }

            return -1;
        }

        private static List<MemberEntry> CollectMembers(Type type)
        {
            var result = new List<MemberEntry>();
            var seen = new HashSet<string>();

            CollectFromType(type, MemberOrigin.Own, result, seen);

            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object)
                   && baseType != typeof(MonoBehaviour) && baseType != typeof(ScriptableObject))
            {
                CollectFromType(baseType, MemberOrigin.Inherited, result, seen);
                baseType = baseType.BaseType;
            }

            return result;
        }

        private static void CollectFromType(Type type, MemberOrigin origin,
            List<MemberEntry> result, HashSet<string> seen)
        {
            foreach (var field in type.GetFields(DeclaredMembers))
            {
                if (!seen.Add(field.Name))
                    continue;
                if (!IsNumericType(field.FieldType))
                    continue;
                // Compiler-generated backing fields start with '<'.
                if (field.Name.StartsWith("<"))
                    continue;
                if (origin == MemberOrigin.Inherited && field.IsPrivate)
                    continue;

                var isConstant = field.IsLiteral || (field.IsStatic && field.IsInitOnly);
                result.Add(new MemberEntry
                {
                    Name = field.Name,
                    Category = isConstant ? MemberCategory.Constant : MemberCategory.Field,
                    Origin = origin,
                    ReturnType = field.FieldType
                });
            }

            foreach (var property in type.GetProperties(DeclaredMembers))
            {
                if (!seen.Add(property.Name))
                    continue;
                if (!property.CanRead)
                    continue;
                if (!IsNumericType(property.PropertyType))
                    continue;
                if (origin == MemberOrigin.Inherited)
                {
                    var getter = property.GetGetMethod(true);
                    if (getter != null && getter.IsPrivate)
                        continue;
                }

                result.Add(new MemberEntry
                {
                    Name = property.Name,
                    Category = MemberCategory.Property,
                    Origin = origin,
                    ReturnType = property.PropertyType
                });
            }

            foreach (var method in type.GetMethods(DeclaredMembers))
            {
                if (!seen.Add(method.Name))
                    continue;
                if (method.GetParameters().Length != 0)
                    continue;
                if (!IsNumericType(method.ReturnType))
                    continue;
                if (method.IsSpecialName)
                    continue;
                if (origin == MemberOrigin.Inherited && method.IsPrivate)
                    continue;

                result.Add(new MemberEntry
                {
                    Name = method.Name,
                    Category = MemberCategory.Method,
                    Origin = origin,
                    ReturnType = method.ReturnType
                });
            }
        }

        private static bool IsNumericType(Type type) => NumericTypes.Contains(type);

        [InitializeOnLoadMethod]
        private static void ClearCache() => Cache.Clear();
    }
}
