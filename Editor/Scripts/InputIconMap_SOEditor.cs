using UnityEngine;
using UnityEditor;

namespace HelloDev.Input.Editor
{
    /// <summary>
    /// Custom editor for InputIconMap_SO with preview functionality.
    /// </summary>
    [CustomEditor(typeof(InputIconMap_SO))]
    public class InputIconMap_SOEditor : UnityEditor.Editor
    {
        private SerializedProperty _deviceLayoutNameProp;
        private SerializedProperty _mappingsProp;

        private const float IconPreviewSize = 32f;
        private const float RowHeight = 40f;

        private void OnEnable()
        {
            _deviceLayoutNameProp = serializedObject.FindProperty("deviceLayoutName");
            _mappingsProp = serializedObject.FindProperty("mappings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Device layout name
            EditorGUILayout.PropertyField(_deviceLayoutNameProp);
            EditorGUILayout.Space();

            // Mappings header with count
            EditorGUILayout.LabelField($"Icon Mappings ({_mappingsProp.arraySize})", EditorStyles.boldLabel);

            // Draw each mapping with preview
            for (int i = 0; i < _mappingsProp.arraySize; i++)
            {
                DrawMappingElement(i);
            }

            EditorGUILayout.Space();

            // Add/Remove buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Mapping", GUILayout.Height(24)))
            {
                _mappingsProp.InsertArrayElementAtIndex(_mappingsProp.arraySize);
                var newElement = _mappingsProp.GetArrayElementAtIndex(_mappingsProp.arraySize - 1);
                newElement.FindPropertyRelative("controlPath").stringValue = "";
                newElement.FindPropertyRelative("icon").objectReferenceValue = null;
                newElement.FindPropertyRelative("fallbackText").stringValue = "";
            }

            if (_mappingsProp.arraySize > 0 && GUILayout.Button("Remove Last", GUILayout.Height(24)))
            {
                _mappingsProp.DeleteArrayElementAtIndex(_mappingsProp.arraySize - 1);
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMappingElement(int index)
        {
            var element = _mappingsProp.GetArrayElementAtIndex(index);
            var controlPathProp = element.FindPropertyRelative("controlPath");
            var iconProp = element.FindPropertyRelative("icon");
            var fallbackTextProp = element.FindPropertyRelative("fallbackText");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // Icon preview on the left
            var iconRect = GUILayoutUtility.GetRect(IconPreviewSize, IconPreviewSize, GUILayout.Width(IconPreviewSize));
            var icon = iconProp.objectReferenceValue as Sprite;

            if (icon != null)
            {
                DrawSpritePreview(iconRect, icon);
            }
            else
            {
                // Draw placeholder with fallback text
                EditorGUI.DrawRect(iconRect, new Color(0.2f, 0.2f, 0.2f));
                var text = fallbackTextProp.stringValue;
                if (!string.IsNullOrEmpty(text))
                {
                    var style = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    GUI.Label(iconRect, text.Length > 3 ? text.Substring(0, 3) : text, style);
                }
            }

            // Fields on the right
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Path", GUILayout.Width(40));
            EditorGUILayout.PropertyField(controlPathProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Icon", GUILayout.Width(40));
            EditorGUILayout.PropertyField(iconProp, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.LabelField("Text", GUILayout.Width(30));
            EditorGUILayout.PropertyField(fallbackTextProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Remove button
            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(32)))
            {
                _mappingsProp.DeleteArrayElementAtIndex(index);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return;

            var tex = sprite.texture;
            var texCoords = new Rect(
                sprite.rect.x / tex.width,
                sprite.rect.y / tex.height,
                sprite.rect.width / tex.width,
                sprite.rect.height / tex.height
            );

            GUI.DrawTextureWithTexCoords(rect, tex, texCoords);
        }
    }
}
