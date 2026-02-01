using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HelloDev.Input.Editor
{
    /// <summary>
    /// Custom editor for InputButtonWithPrompt that provides a binding dropdown
    /// and shows the coordinator's relationship to child components.
    /// </summary>
    [CustomEditor(typeof(InputButtonWithPrompt))]
    public class InputButtonWithPromptEditor : UnityEditor.Editor
    {
        private SerializedProperty _actionReferenceProperty;
        private SerializedProperty _bindingIdProperty;
        private SerializedProperty _iconProviderProperty;
        private SerializedProperty _inputActionButtonProperty;
        private SerializedProperty _inputPromptDisplayProperty;
        private SerializedProperty _onActionPerformedProperty;
        private SerializedProperty _enableDebugLoggingProperty;

        private GUIContent[] _bindingOptions;
        private string[] _bindingOptionValues;
        private int _selectedBindingIndex;

        private static readonly GUIContent s_BindingLabel = new GUIContent("Binding",
            "Select which binding of the action to display");

        private static readonly GUIContent s_FindChildrenButton = new GUIContent("Find Children",
            "Search for InputActionButton and InputPromptDisplay in children");

        private static readonly GUIContent s_SyncButton = new GUIContent("Sync to Children",
            "Manually sync ActionReference and BindingId to child components");

        private void OnEnable()
        {
            _actionReferenceProperty = serializedObject.FindProperty("actionReference");
            _bindingIdProperty = serializedObject.FindProperty("bindingId");
            _iconProviderProperty = serializedObject.FindProperty("iconProvider");
            _inputActionButtonProperty = serializedObject.FindProperty("inputActionButton");
            _inputPromptDisplayProperty = serializedObject.FindProperty("inputPromptDisplay");
            _onActionPerformedProperty = serializedObject.FindProperty("onActionPerformed");
            _enableDebugLoggingProperty = serializedObject.FindProperty("enableDebugLogging");

            RefreshBindingOptions();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var coordinator = (InputButtonWithPrompt)target;

            // Info box explaining the coordinator pattern
            EditorGUILayout.HelpBox(
                "Coordinator: Syncs ActionReference to child InputActionButton and InputPromptDisplay. " +
                "Configure display options (text format, icons) on the child InputPromptDisplay.",
                MessageType.Info);

            EditorGUILayout.Space();

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

            // Child References section
            EditorGUILayout.LabelField("Child References", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                // Show status indicators
                var hasActionButton = _inputActionButtonProperty.objectReferenceValue != null;
                var hasPromptDisplay = _inputPromptDisplayProperty.objectReferenceValue != null;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(_inputActionButtonProperty);
                    DrawStatusIcon(hasActionButton);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(_inputPromptDisplayProperty);
                    DrawStatusIcon(hasPromptDisplay);
                }

                EditorGUILayout.Space(2);

                // Utility buttons
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(s_FindChildrenButton))
                    {
                        Undo.RecordObject(coordinator, "Find Child Components");
                        coordinator.FindChildComponents();
                        EditorUtility.SetDirty(coordinator);
                    }

                    using (new EditorGUI.DisabledScope(!Application.isPlaying))
                    {
                        if (GUILayout.Button(s_SyncButton))
                        {
                            coordinator.SyncToChildren();
                        }
                    }
                }

                // Warning if children not found
                if (!hasActionButton && !hasPromptDisplay)
                {
                    EditorGUILayout.HelpBox(
                        "No child components found. Add InputActionButton and/or InputPromptDisplay as children, " +
                        "then click 'Find Children'.",
                        MessageType.Warning);
                }
                else if (!hasActionButton)
                {
                    EditorGUILayout.HelpBox(
                        "No InputActionButton found. Input shortcuts won't work without it.",
                        MessageType.Info);
                }
                else if (!hasPromptDisplay)
                {
                    EditorGUILayout.HelpBox(
                        "No InputPromptDisplay found. Key binding won't be displayed without it.",
                        MessageType.Info);
                }
            }

            EditorGUILayout.Space();

            // Events section
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_onActionPerformedProperty);
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

        private void DrawStatusIcon(bool isValid)
        {
            var iconContent = isValid
                ? EditorGUIUtility.IconContent("TestPassed")
                : EditorGUIUtility.IconContent("TestNormal");

            GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(EditorGUIUtility.singleLineHeight));
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
