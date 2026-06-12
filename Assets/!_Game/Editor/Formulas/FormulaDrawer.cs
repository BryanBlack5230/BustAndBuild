#nullable enable
using System.Collections;
using System.Reflection;
using BarkingBird.Runtime.Infrastructure.Formulas;
using UnityEditor;
using UnityEngine;

namespace BarkingBird.Editor
{
    /// <summary>
    /// Drawer for <see cref="FormulaAttribute"/> string fields: live result/error
    /// preview, the formula text field, and one row per {i} slot with a numeric-member
    /// dropdown and an editor-only test value.
    /// </summary>
    [CustomPropertyDrawer(typeof(FormulaAttribute))]
    public sealed class FormulaDrawer : PropertyDrawer
    {
        private const float SlotLabelWidth = 30f;
        private const float TestFieldWidth = 60f;
        private const float ColumnGap = 2f;
        private const float HelpBoxLines = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return EditorGUIUtility.singleLineHeight;

            var line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var helpBox = EditorGUIUtility.singleLineHeight * HelpBoxLines + EditorGUIUtility.standardVerticalSpacing;
            var slotCount = FormulaParser.GetMaxSlotIndex(property.stringValue) + 1;
            return helpBox + line * (1 + slotCount);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "[Formula] is valid on string fields only");
                return;
            }

            var formulaAttribute = (FormulaAttribute)attribute;
            var slotsProperty = FindSiblingProperty(property, formulaAttribute.SlotsFieldName);
            if (slotsProperty == null || !slotsProperty.isArray)
            {
                EditorGUI.HelpBox(position,
                    $"FormulaSlot[] field '{formulaAttribute.SlotsFieldName}' not found next to '{property.name}'",
                    MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var slotCount = FormulaParser.GetMaxSlotIndex(property.stringValue) + 1;
            if (slotsProperty.arraySize != slotCount)
                slotsProperty.arraySize = slotCount;

            var y = position.y;
            var helpBoxHeight = EditorGUIUtility.singleLineHeight * HelpBoxLines;
            DrawResultBox(new Rect(position.x, y, position.width, helpBoxHeight), property, slotsProperty, slotCount);
            y += helpBoxHeight + EditorGUIUtility.standardVerticalSpacing;

            var formulaRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            property.stringValue = EditorGUI.TextField(formulaRect, label, property.stringValue);
            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            DrawSlots(position, y, property, slotsProperty, slotCount);

            EditorGUI.EndProperty();
        }

        private static void DrawResultBox(Rect rect, SerializedProperty formulaProperty,
            SerializedProperty slotsProperty, int slotCount)
        {
            var formula = formulaProperty.stringValue;
            if (string.IsNullOrEmpty(formula))
            {
                EditorGUI.HelpBox(rect, "Empty formula", MessageType.Info);
                return;
            }

            var parameters = new double[slotCount];
            for (var i = 0; i < slotCount && i < slotsProperty.arraySize; i++)
            {
                var element = slotsProperty.GetArrayElementAtIndex(i);
                var testProperty = element.FindPropertyRelative(nameof(FormulaSlot.TestValue));
                if (testProperty != null)
                    parameters[i] = testProperty.floatValue;
            }

            if (FormulaParser.TryEvaluate(formula, parameters, out var result, out var error))
                EditorGUI.HelpBox(rect, $"Result: {result:G6}", MessageType.Info);
            else
                EditorGUI.HelpBox(rect, $"Error: {error}", MessageType.Error);
        }

        private void DrawSlots(Rect position, float y, SerializedProperty formulaProperty,
            SerializedProperty slotsProperty, int slotCount)
        {
            if (slotCount <= 0)
                return;

            var owner = ResolveOwnerObject(formulaProperty) ?? formulaProperty.serializedObject.targetObject;
            var resolved = FormulaReflectionResolver.Resolve(owner.GetType());
            var lineHeight = EditorGUIUtility.singleLineHeight;

            for (var i = 0; i < slotCount; i++)
            {
                var element = slotsProperty.GetArrayElementAtIndex(i);
                var memberNameProperty = element.FindPropertyRelative(nameof(FormulaSlot.MemberName));
                var testValueProperty = element.FindPropertyRelative(nameof(FormulaSlot.TestValue));

                var labelRect = new Rect(position.x, y, SlotLabelWidth, lineHeight);
                EditorGUI.LabelField(labelRect, $"{{{i}}}");

                var popupWidth = position.width - SlotLabelWidth - TestFieldWidth - ColumnGap * 2f;
                var popupRect = new Rect(position.x + SlotLabelWidth + ColumnGap, y, popupWidth, lineHeight);

                var currentIndex = FormulaReflectionResolver.FindMemberIndex(
                    resolved.MemberNames, memberNameProperty.stringValue);
                var newIndex = EditorGUI.Popup(popupRect, currentIndex, resolved.PopupKeys);
                if (newIndex != currentIndex && newIndex >= 0)
                    memberNameProperty.stringValue = resolved.MemberNames[newIndex];

                var testRect = new Rect(position.x + position.width - TestFieldWidth, y, TestFieldWidth, lineHeight);
                testValueProperty.floatValue = EditorGUI.FloatField(testRect, testValueProperty.floatValue);

                y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private static SerializedProperty? FindSiblingProperty(SerializedProperty property, string name)
        {
            var path = property.propertyPath;
            var lastDot = path.LastIndexOf('.');
            if (lastDot < 0)
                return property.serializedObject.FindProperty(name);

            var siblingPath = path.Substring(0, lastDot + 1) + name;
            return property.serializedObject.FindProperty(siblingPath);
        }

        // Walks propertyPath from the target object so formulas nested in
        // serializable classes/arrays resolve members of their declaring object,
        // not of the root asset.
        private static object? ResolveOwnerObject(SerializedProperty property)
        {
            object? current = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data[", "[");
            var elements = path.Split('.');

            for (var i = 0; i < elements.Length - 1 && current != null; i++)
                current = GetPathSegment(current, elements[i]);

            return current;
        }

        private static object? GetPathSegment(object source, string element)
        {
            var bracket = element.IndexOf('[');
            if (bracket < 0)
                return GetFieldValue(source, element);

            var fieldName = element.Substring(0, bracket);
            var index = int.Parse(element.Substring(bracket + 1, element.Length - bracket - 2));
            if (GetFieldValue(source, fieldName) is not IList list || index >= list.Count)
                return null;
            return list[index];
        }

        private static object? GetFieldValue(object source, string fieldName)
        {
            for (var type = source.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.GetValue(source);
            }

            return null;
        }
    }
}
