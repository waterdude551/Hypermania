using System;
using UnityEditor;
using UnityEngine;

namespace Utils.EnumArray
{
    [CustomPropertyDrawer(typeof(EnumArray<,>), true)]
    public class EnumArrayDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valuesProp = property.FindPropertyRelative("values");
            if (valuesProp == null)
                return EditorGUIUtility.singleLineHeight;

            float h = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return h;

            for (int i = 0; i < valuesProp.arraySize; i++)
                h += EditorGUI.GetPropertyHeight(valuesProp.GetArrayElementAtIndex(i), true) + 2f;

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valuesProp = property.FindPropertyRelative("values");
            if (valuesProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Missing values[]");
                return;
            }

            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label,
                true
            );

            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;

            Type enumType = GetEnumType(property);
            if (enumType == null || !enumType.IsEnum)
            {
                EditorGUI.indentLevel--;
                EditorGUI.LabelField(
                    new Rect(
                        position.x,
                        position.y + EditorGUIUtility.singleLineHeight,
                        position.width,
                        EditorGUIUtility.singleLineHeight
                    ),
                    "EnumArray",
                    "Could not resolve enum type."
                );
                return;
            }

            EnsureArraySize(valuesProp, enumType);

            string[] names = Enum.GetNames(enumType);

            float y = position.y + EditorGUIUtility.singleLineHeight + 2f;
            for (int i = 0; i < valuesProp.arraySize; i++)
            {
                var elem = valuesProp.GetArrayElementAtIndex(i);
                float elemH = EditorGUI.GetPropertyHeight(elem, true);
                EditorGUI.PropertyField(
                    new Rect(position.x, y, position.width, elemH),
                    elem,
                    new GUIContent(names[i]),
                    true
                );
                y += elemH + 2f;
            }

            EditorGUI.indentLevel--;
        }

        private static void EnsureArraySize(SerializedProperty valuesProp, Type enumType)
        {
            int n = Enum.GetNames(enumType).Length;
            if (valuesProp.arraySize != n)
                valuesProp.arraySize = n;
        }

        private static Type GetEnumType(SerializedProperty property)
        {
            // fieldInfo is the EnumArray<TEnum, TValue> field; extract TEnum
            var t = property.serializedObject.targetObject.GetType();
            var fi = t.GetField(
                property.propertyPath.Split('.')[0],
                System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic
            );
            if (fi == null)
                return null;

            var ft = fi.FieldType; // EnumArray<CharacterState, HitboxData>
            if (!ft.IsGenericType)
                return null;
            return ft.GetGenericArguments()[0];
        }
    }
}
