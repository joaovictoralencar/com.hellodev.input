using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace HelloDev.Input
{
    /// <summary>
    /// UI component that displays the current binding for an input action.
    /// Automatically shows the correct icon/text based on the binding's device layout.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Follows Unity's Input System sample pattern: the device layout is determined
    /// per-binding via <c>GetBindingDisplayString()</c>, not from a global tracker.
    /// </para>
    /// <para>
    /// This means if you have multiple bindings (keyboard + gamepad), each will
    /// display correctly based on its own device, not a "current" device.
    /// </para>
    /// </remarks>
    public class InputPromptDisplay : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input Action")]
        [Tooltip("Reference to the action to display binding for")]
        [SerializeField] private InputActionReference actionReference;

        [Tooltip("Which binding to display (-1 = auto-select based on control scheme)")]
        [SerializeField] private int bindingIndex = -1;

        [Tooltip("Control scheme to filter by (e.g., 'Gamepad', 'Keyboard&Mouse'). Leave empty to show first binding.")]
        [SerializeField] private string controlSchemeFilter;

        [Header("Icon Provider")]
        [Tooltip("ScriptableObject that provides icon maps for different device layouts")]
        [SerializeField] private InputIconProvider_SO iconProvider;

        [Header("UI References")]
        [Tooltip("Text component for displaying binding text (can be null if using icon only)")]
        [SerializeField] private TMP_Text promptText;

        [Tooltip("Image component for displaying binding icon (can be null if using text only)")]
        [SerializeField] private Image iconImage;

        [Header("Display Options")]
        [Tooltip("Format string for text display. {0} = binding text")]
        [SerializeField] private string textFormat = "{0}";

        [Tooltip("If true, prefer showing icon over text when both are available")]
        [SerializeField] private bool preferIcon = true;

        [Tooltip("If true, hide icon when showing text and vice versa")]
        [SerializeField] private bool exclusiveDisplay = true;

        [Tooltip("Options for how the binding display string is formatted")]
        [SerializeField] private InputBinding.DisplayStringOptions displayStringOptions;

        #endregion

        #region Private Fields

        private InputAction _directAction;

        #endregion

        #region Properties

        /// <summary>
        /// The action reference being displayed.
        /// </summary>
        public InputActionReference ActionReference
        {
            get => actionReference;
            set
            {
                actionReference = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// The text format used for display.
        /// </summary>
        public string TextFormat
        {
            get => textFormat;
            set
            {
                textFormat = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Gets the resolved action (either from direct assignment or action reference).
        /// </summary>
        private InputAction ResolvedAction => _directAction ?? actionReference?.action;

        /// <summary>
        /// Gets or sets whether to prefer icon over text.
        /// </summary>
        public bool PreferIcon
        {
            get => preferIcon;
            set
            {
                preferIcon = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Gets or sets whether display is exclusive (icon OR text, not both).
        /// </summary>
        public bool ExclusiveDisplay
        {
            get => exclusiveDisplay;
            set
            {
                exclusiveDisplay = value;
                UpdateDisplay();
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            // Subscribe to binding changes
            InputSystem.onActionChange += OnActionChange;

            // Initial display
            UpdateDisplay();
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= OnActionChange;
        }

        #endregion

        #region Display Logic

        /// <summary>
        /// Refreshes the display based on current binding.
        /// </summary>
        public void UpdateDisplay()
        {
            var action = ResolvedAction;
            if (action == null)
            {
                SetDisplayEmpty();
                return;
            }

            // Find the binding index to display
            int resolvedBindingIndex = bindingIndex;
            if (resolvedBindingIndex < 0)
            {
                resolvedBindingIndex = FindBindingForControlScheme(action, controlSchemeFilter);
            }

            if (resolvedBindingIndex < 0 || resolvedBindingIndex >= action.bindings.Count)
            {
                resolvedBindingIndex = 0;
            }

            // Get display string WITH device layout and control path (Unity's pattern)
            var displayString = action.GetBindingDisplayString(
                resolvedBindingIndex,
                out var deviceLayoutName,
                out var controlPath,
                displayStringOptions
            );

            if (string.IsNullOrEmpty(displayString))
            {
                SetDisplayEmpty();
                return;
            }

            // Get icon from provider based on the binding's device layout
            Sprite icon = null;
            string fallbackText = displayString;

            if (iconProvider != null && !string.IsNullOrEmpty(controlPath))
            {
                var (mappedIcon, mappedText) = iconProvider.GetBinding(deviceLayoutName, controlPath);
                if (mappedIcon != null)
                {
                    icon = mappedIcon;
                }
                if (!string.IsNullOrEmpty(mappedText))
                {
                    fallbackText = mappedText;
                }
            }

            // Update UI based on preference
            if (preferIcon && icon != null)
            {
                SetIconDisplay(icon, fallbackText);
            }
            else
            {
                SetTextDisplay(fallbackText);
            }
        }

        private void OnActionChange(object obj, InputActionChange change)
        {
            // Only care about binding changes
            if (change != InputActionChange.BoundControlsChanged)
                return;

            // Check if it's our action
            var action = obj as InputAction;
            var actionMap = action?.actionMap ?? obj as InputActionMap;
            var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

            var resolvedAction = ResolvedAction;
            if (resolvedAction == null)
                return;

            if (resolvedAction == action ||
                resolvedAction.actionMap == actionMap ||
                resolvedAction.actionMap?.asset == actionAsset)
            {
                UpdateDisplay();
            }
        }

        private int FindBindingForControlScheme(InputAction action, string controlScheme)
        {
            if (string.IsNullOrEmpty(controlScheme))
                return 0;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                // Skip composite parts
                if (binding.isPartOfComposite)
                    continue;

                // Check if binding matches control scheme
                var groups = binding.groups;
                if (!string.IsNullOrEmpty(groups))
                {
                    var bindingGroups = groups.Split(';');
                    foreach (var group in bindingGroups)
                    {
                        if (group.Trim().Equals(controlScheme, StringComparison.OrdinalIgnoreCase))
                        {
                            return i;
                        }
                    }
                }
            }

            return 0;
        }

        private void SetIconDisplay(Sprite icon, string altText)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }

            if (promptText != null)
            {
                if (exclusiveDisplay)
                {
                    promptText.gameObject.SetActive(false);
                }
                else
                {
                    promptText.text = string.Format(textFormat, altText);
                }
            }
        }

        private void SetTextDisplay(string text)
        {
            if (promptText != null)
            {
                promptText.text = string.Format(textFormat, text);
                promptText.gameObject.SetActive(true);
            }

            if (iconImage != null && exclusiveDisplay)
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        private void SetDisplayEmpty()
        {
            if (promptText != null)
            {
                promptText.text = string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the action reference at runtime.
        /// </summary>
        public void SetAction(InputActionReference action)
        {
            actionReference = action;
            UpdateDisplay();
        }

        /// <summary>
        /// Sets an InputAction directly (for runtime-created actions).
        /// This takes precedence over the actionReference field.
        /// </summary>
        public void SetAction(InputAction action)
        {
            _directAction = action;
            UpdateDisplay();
        }

        /// <summary>
        /// Clears the directly-set InputAction, reverting to using actionReference.
        /// </summary>
        public void ClearDirectAction()
        {
            _directAction = null;
            UpdateDisplay();
        }

        /// <summary>
        /// Sets the icon provider at runtime.
        /// </summary>
        public void SetIconProvider(InputIconProvider_SO provider)
        {
            iconProvider = provider;
            UpdateDisplay();
        }

        /// <summary>
        /// Configures the UI references at runtime.
        /// </summary>
        public void ConfigureUI(TMP_Text text, Image icon)
        {
            promptText = text;
            iconImage = icon;
        }

        /// <summary>
        /// Configures display options at runtime.
        /// </summary>
        public void ConfigureDisplayOptions(string format, bool preferIconOverText, bool exclusive)
        {
            textFormat = format;
            preferIcon = preferIconOverText;
            exclusiveDisplay = exclusive;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Refresh display in editor when values change
            if (Application.isPlaying)
            {
                UpdateDisplay();
            }
        }
#endif
    }
}
