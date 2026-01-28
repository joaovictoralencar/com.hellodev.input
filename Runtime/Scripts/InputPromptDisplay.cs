using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace HelloDev.Input
{
    /// <summary>
    /// Displays the current binding for an input action with automatic icon/text switching.
    /// Follows Unity's Input System sample pattern with added designer-friendly features.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <c>bindingId</c> (GUID) to identify specific bindings, which survives binding reordering.
    /// The editor provides a dropdown to select bindings easily.
    /// </para>
    /// <para>
    /// Supports both built-in icon display (via <see cref="InputIconProvider_SO"/>) and
    /// custom display via <see cref="updateBindingUIEvent"/> for maximum flexibility.
    /// </para>
    /// </remarks>
    public class InputPromptDisplay : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input Action")]
        [Tooltip("Reference to the action to display binding for")]
        [SerializeField] private InputActionReference actionReference;

        [Tooltip("ID of the specific binding to display (selected via dropdown in editor)")]
        [SerializeField] private string bindingId;

        [Tooltip("Options for how the binding display string is formatted")]
        [SerializeField] private InputBinding.DisplayStringOptions displayStringOptions;

        [Header("Icon Provider")]
        [Tooltip("ScriptableObject that provides icon maps for different device layouts (optional)")]
        [SerializeField] private InputIconProvider_SO iconProvider;

        [Header("UI References")]
        [Tooltip("Text component for displaying binding text")]
        [SerializeField] private TMP_Text bindingText;

        [Tooltip("Image component for displaying binding icon")]
        [SerializeField] private Image bindingIcon;

        [Header("Display Options")]
        [Tooltip("Format string for text display. {0} = binding text")]
        [SerializeField] private string textFormat = "{0}";

        [Tooltip("If true, prefer showing icon over text when available")]
        [SerializeField] private bool preferIcon = true;

        [Tooltip("If true, hide icon when showing text and vice versa")]
        [SerializeField] private bool exclusiveDisplay = true;

        [Header("Events")]
        [Tooltip("Event fired when binding display updates. Use for custom icon handling.")]
        [SerializeField] private UpdateBindingUIEvent updateBindingUIEvent;

        #endregion

        #region Static Fields

        // Static list optimization (Unity's pattern) - single subscription for all instances
        private static List<InputPromptDisplay> s_Instances;

        #endregion

        #region Properties

        /// <summary>
        /// Reference to the action being displayed.
        /// </summary>
        public InputActionReference ActionReference
        {
            get => actionReference;
            set
            {
                actionReference = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// ID of the binding being displayed.
        /// </summary>
        public string BindingId
        {
            get => bindingId;
            set
            {
                bindingId = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// The text component showing the binding.
        /// </summary>
        public TMP_Text BindingText
        {
            get => bindingText;
            set
            {
                bindingText = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// The image component showing the binding icon.
        /// </summary>
        public Image BindingIcon
        {
            get => bindingIcon;
            set
            {
                bindingIcon = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Display string options.
        /// </summary>
        public InputBinding.DisplayStringOptions DisplayStringOptions
        {
            get => displayStringOptions;
            set
            {
                displayStringOptions = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Event fired when binding display updates.
        /// Parameters: (component, displayString, deviceLayoutName, controlPath)
        /// </summary>
        public UpdateBindingUIEvent OnUpdateBindingUI
        {
            get
            {
                if (updateBindingUIEvent == null)
                    updateBindingUIEvent = new UpdateBindingUIEvent();
                return updateBindingUIEvent;
            }
        }

        /// <summary>
        /// Whether to prefer icon over text.
        /// </summary>
        public bool PreferIcon
        {
            get => preferIcon;
            set
            {
                preferIcon = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Whether display is exclusive (icon OR text).
        /// </summary>
        public bool ExclusiveDisplay
        {
            get => exclusiveDisplay;
            set
            {
                exclusiveDisplay = value;
                UpdateBindingDisplay();
            }
        }

        #endregion

        #region Unity Lifecycle

        protected void OnEnable()
        {
            // Static list optimization - single subscription shared by all instances
            if (s_Instances == null)
                s_Instances = new List<InputPromptDisplay>();

            s_Instances.Add(this);

            if (s_Instances.Count == 1)
                InputSystem.onActionChange += OnActionChange;

            UpdateBindingDisplay();
        }

        protected void OnDisable()
        {
            s_Instances.Remove(this);

            if (s_Instances.Count == 0)
            {
                s_Instances = null;
                InputSystem.onActionChange -= OnActionChange;
            }
        }

        #endregion

        #region Display Logic

        /// <summary>
        /// Refreshes the binding display.
        /// </summary>
        public void UpdateBindingDisplay()
        {
            var displayString = string.Empty;
            var deviceLayoutName = default(string);
            var controlPath = default(string);

            // Get display string from action
            var action = actionReference?.action;
            if (action != null)
            {
                var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == bindingId);
                if (bindingIndex != -1)
                {
                    displayString = action.GetBindingDisplayString(
                        bindingIndex,
                        out deviceLayoutName,
                        out controlPath,
                        displayStringOptions
                    );
                }
                else if (action.bindings.Count > 0)
                {
                    // Fallback to first binding if bindingId not found
                    displayString = action.GetBindingDisplayString(
                        0,
                        out deviceLayoutName,
                        out controlPath,
                        displayStringOptions
                    );
                }
            }

            // Update built-in display
            UpdateBuiltInDisplay(displayString, deviceLayoutName, controlPath);

            // Fire event for custom handling
            updateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
        }

        private void UpdateBuiltInDisplay(string displayString, string deviceLayoutName, string controlPath)
        {
            if (string.IsNullOrEmpty(displayString))
            {
                SetDisplayEmpty();
                return;
            }

            // Try to get icon from provider
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

        private void SetIconDisplay(Sprite icon, string altText)
        {
            if (bindingIcon != null)
            {
                bindingIcon.sprite = icon;
                bindingIcon.gameObject.SetActive(true);
            }

            if (bindingText != null)
            {
                if (exclusiveDisplay)
                {
                    bindingText.gameObject.SetActive(false);
                }
                else
                {
                    bindingText.text = string.Format(textFormat, altText);
                    bindingText.gameObject.SetActive(true);
                }
            }
        }

        private void SetTextDisplay(string text)
        {
            if (bindingText != null)
            {
                bindingText.text = string.Format(textFormat, text);
                bindingText.gameObject.SetActive(true);
            }

            if (bindingIcon != null && exclusiveDisplay)
            {
                bindingIcon.gameObject.SetActive(false);
            }
        }

        private void SetDisplayEmpty()
        {
            if (bindingText != null)
            {
                bindingText.text = string.Empty;
            }

            if (bindingIcon != null)
            {
                bindingIcon.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Static Event Handler

        // Static handler - updates all instances when bindings change (Unity's pattern)
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
                return;

            var action = obj as InputAction;
            var actionMap = action?.actionMap ?? obj as InputActionMap;
            var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

            for (var i = 0; i < s_Instances.Count; ++i)
            {
                var component = s_Instances[i];
                var referencedAction = component.actionReference?.action;
                if (referencedAction == null)
                    continue;

                if (referencedAction == action ||
                    referencedAction.actionMap == actionMap ||
                    referencedAction.actionMap?.asset == actionAsset)
                {
                    component.UpdateBindingDisplay();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the icon provider at runtime.
        /// </summary>
        public void SetIconProvider(InputIconProvider_SO provider)
        {
            iconProvider = provider;
            UpdateBindingDisplay();
        }

        /// <summary>
        /// Configures display options at runtime.
        /// </summary>
        public void ConfigureDisplayOptions(string format, bool preferIconOverText, bool exclusive)
        {
            textFormat = format;
            preferIcon = preferIconOverText;
            exclusiveDisplay = exclusive;
            UpdateBindingDisplay();
        }

        /// <summary>
        /// Gets the binding index for the current bindingId.
        /// Returns -1 if not found.
        /// </summary>
        public int GetBindingIndex()
        {
            var action = actionReference?.action;
            if (action == null || string.IsNullOrEmpty(bindingId))
                return -1;

            return action.bindings.IndexOf(x => x.id.ToString() == bindingId);
        }

        #endregion

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateBindingDisplay();
            }
        }
#endif
    }

    /// <summary>
    /// Event fired when binding UI should be updated.
    /// Parameters: (component, displayString, deviceLayoutName, controlPath)
    /// </summary>
    [Serializable]
    public class UpdateBindingUIEvent : UnityEvent<InputPromptDisplay, string, string, string>
    {
    }
}
