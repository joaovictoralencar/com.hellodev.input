using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HelloDev.Input.Editor
{
    /// <summary>
    /// Custom editor for InputButtonWithPrompt that provides a binding dropdown.
    /// </summary>
    [CustomEditor(typeof(InputButtonWithPrompt))]
    public class InputButtonWithPromptEditor : UnityEditor.Editor
    {
        private SerializedProperty _actionReferenceProperty;
        private SerializedProperty _bindingIdProperty;
        private SerializedProperty _iconProviderProperty;
        private SerializedProperty _bindingTextProperty;
        private SerializedProperty _bindingIconProperty;
        private SerializedProperty _textFormatProperty;
        private SerializedProperty _preferIconProperty;
        private SerializedProperty _exclusiveDisplayProperty;
        private SerializedProperty _respectCanvasGroupProperty;
        private SerializedProperty _onActionPerformedProperty;
        private SerializedProperty _updateBindingUIEventProperty;
        private SerializedProperty _enableDebugLoggingProperty;

        private GUIContent[] _bindingOptions;
        private string[] _bindingOptionValues;
        private int _selectedBindingIndex;

        private static readonly GUIContent s_BindingLabel = new GUIContent("Binding",
            "Select which binding of the action to display and respond to");

        private void OnEnable()
        {
            _actionReferenceProperty = serializedObject.FindProperty("actionReference");
            _bindingIdProperty = serializedObject.FindProperty("bindingId");
            _iconProviderProperty = serializedObject.FindProperty("iconProvider");
            _bindingTextProperty = serializedObject.FindProperty("bindingText");
            _bindingIconProperty = serializedObject.FindProperty("bindingIcon");
            _textFormatProperty = serializedObject.FindProperty("textFormat");
            _preferIconProperty = serializedObject.FindProperty("preferIcon");
            _exclusiveDisplayProperty = serializedObject.FindProperty("exclusiveDisplay");
            _respectCanvasGroupProperty = serializedObject.FindProperty("respectCanvasGroup");
            _onActionPerformedProperty = serializedObject.FindProperty("onActionPerformed");
            _updateBindingUIEventProperty = serializedObject.FindProperty("updateBindingUIEvent");
            _enableDebugLoggingProperty = serializedObject.FindProperty("enableDebugLogging");

            RefreshBindingOptions();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Input Action section
            EditorGUILayout.LabelField("Input Action", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_actionReferenceProperty);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    RefreshBindingOptions();
                }

                // Binding dropdown
                if (_bindingOptions != null && _bindingOptions.Length > 0)
                {
                    var newSelectedBinding = EditorGUILayout.Popup(s_BindingLabel, _selectedBindingIndex, _bindingOptions);
                    if (newSelectedBinding != _selectedBindingIndex)
                    {
                        _selectedBindingIndex = newSelectedBinding;
                        _bindingIdProperty.stringValue = _bindingOptionValues[newSelectedBinding];
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select an action to see available bindings", MessageType.Info);
                }
            }

            EditorGUILayout.Space();

            // Icon Provider section
            EditorGUILayout.LabelField("Icon Provider", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_iconProviderProperty);
            }

            EditorGUILayout.Space();

            // UI References section
            EditorGUILayout.LabelField("UI References", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_bindingTextProperty);
                EditorGUILayout.PropertyField(_bindingIconProperty);
            }

            EditorGUILayout.Space();

            // Display Options section
            EditorGUILayout.LabelField("Display Options", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_textFormatProperty);
                EditorGUILayout.PropertyField(_preferIconProperty);
                EditorGUILayout.PropertyField(_exclusiveDisplayProperty);
            }

            EditorGUILayout.Space();

            // Button Options section
            EditorGUILayout.LabelField("Button Options", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_respectCanvasGroupProperty);
            }

            EditorGUILayout.Space();

            // Events section
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_onActionPerformedProperty);
                EditorGUILayout.PropertyField(_updateBindingUIEventProperty);
            }

            EditorGUILayout.Space();

            // Debug section
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_enableDebugLoggingProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshBindingOptions()
        {
            var actionReference = (InputActionReference)_actionReferenceProperty.objectReferenceValue;
            var action = actionReference?.action;

            if (action == null)
            {
                _bindingOptions = Array.Empty<GUIContent>();
                _bindingOptionValues = Array.Empty<string>();
                _selectedBindingIndex = -1;
                return;
            }

            var bindings = action.bindings;
            var bindingCount = bindings.Count;

            _bindingOptions = new GUIContent[bindingCount];
            _bindingOptionValues = new string[bindingCount];
            _selectedBindingIndex = 0;

            var currentBindingId = _bindingIdProperty.stringValue;

            for (var i = 0; i < bindingCount; i++)
            {
                var binding = bindings[i];
                var id = binding.id.ToString();
                var hasBindingGroups = !string.IsNullOrEmpty(binding.groups);

                var displayOptions = InputBinding.DisplayStringOptions.DontUseShortDisplayNames |
                                    InputBinding.DisplayStringOptions.IgnoreBindingOverrides;
                if (!hasBindingGroups)
                    displayOptions |= InputBinding.DisplayStringOptions.DontOmitDevice;

                var displayString = action.GetBindingDisplayString(i, displayOptions);

                if (binding.isPartOfComposite)
                    displayString = $"{ObjectNames.NicifyVariableName(binding.name)}: {displayString}";

                displayString = displayString.Replace('/', '\\');

                if (hasBindingGroups)
                {
                    var asset = action.actionMap?.asset;
                    if (asset != null)
                    {
                        var controlSchemes = string.Join(", ",
                            binding.groups.Split(InputBinding.Separator)
                                .Select(x => asset.controlSchemes.FirstOrDefault(c => c.bindingGroup == x).name));

                        displayString = $"{displayString} ({controlSchemes})";
                    }
                }

                _bindingOptions[i] = new GUIContent(displayString);
                _bindingOptionValues[i] = id;

                if (currentBindingId == id)
                    _selectedBindingIndex = i;
            }
        }
    }
}
