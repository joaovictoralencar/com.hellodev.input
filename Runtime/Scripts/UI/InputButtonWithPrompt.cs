using HelloDev.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.Input
{
    /// <summary>
    /// Combines InputActionButton functionality with InputPromptDisplay.
    /// A single component for buttons that respond to input and display their binding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience component that wires up an <see cref="InputActionButton"/>
    /// with an <see cref="InputPromptDisplay"/>. Use it when you want a button that:
    /// </para>
    /// <list type="bullet">
    /// <item>Responds to an input action (keyboard/gamepad)</item>
    /// <item>Displays the current binding with icons</item>
    /// <item>Supports rebinding</item>
    /// </list>
    /// <para>
    /// For more control, use <see cref="InputActionButton"/> and <see cref="InputPromptDisplay"/>
    /// separately.
    /// </para>
    /// </remarks>
    [AddComponentMenu("HelloDev/Input/Input Button With Prompt")]
    public class InputButtonWithPrompt : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input Action")]
        [Tooltip("The action that triggers this button and is displayed")]
        [SerializeField] private InputActionReference actionReference;

        [Tooltip("ID of the specific binding to display (selected via dropdown in editor)")]
        [SerializeField] private string bindingId;

        [Header("Icon Provider")]
        [Tooltip("ScriptableObject that provides icon maps for different device layouts")]
        [SerializeField] private InputIconProvider_SO iconProvider;

        [Header("UI References")]
        [Tooltip("Text component for displaying binding text")]
        [SerializeField] private TMP_Text bindingText;

        [Tooltip("Image component for displaying binding icon")]
        [SerializeField] private Image bindingIcon;

        [Header("Display Options")]
        [Tooltip("Format string for text display. {0} = binding text")]
        [SerializeField] private string textFormat = "[{0}]";

        [Tooltip("If true, prefer showing icon over text when available")]
        [SerializeField] private bool preferIcon = true;

        [Tooltip("If true, hide icon when showing text and vice versa")]
        [SerializeField] private bool exclusiveDisplay = true;

        [Header("Button Options")]
        [Tooltip("If true, only trigger when the GameObject is interactable (checks CanvasGroup)")]
        [SerializeField] private bool respectCanvasGroup = true;

        [Header("Events")]
        [Tooltip("Invoked when the action is performed")]
        [SerializeField] private UnityEvent onActionPerformed = new();

        [Tooltip("Event fired when binding display updates. Use for custom icon handling.")]
        [SerializeField] private UpdateBindingUIEvent updateBindingUIEvent;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging;

        #endregion

        #region Private Fields

        private CanvasGroup _canvasGroup;
        private static System.Collections.Generic.List<InputButtonWithPrompt> s_Instances;

        #endregion

        #region Properties

        /// <summary>
        /// The action reference.
        /// </summary>
        public InputActionReference ActionReference
        {
            get => actionReference;
            set
            {
                // Unsubscribe from old action
                if (enabled && actionReference?.action != null)
                {
                    actionReference.action.performed -= HandleActionPerformed;
                }

                actionReference = value;

                // Subscribe to new action
                if (enabled && actionReference?.action != null)
                {
                    actionReference.action.performed += HandleActionPerformed;
                }

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
        /// Event invoked when the action is performed.
        /// </summary>
        public UnityEvent OnActionPerformed => onActionPerformed;

        /// <summary>
        /// Whether this button is currently interactable.
        /// </summary>
        public bool IsInteractable
        {
            get
            {
                if (!respectCanvasGroup)
                    return true;

                if (_canvasGroup == null)
                    _canvasGroup = GetComponentInParent<CanvasGroup>();

                return _canvasGroup == null || _canvasGroup.interactable;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (respectCanvasGroup)
            {
                _canvasGroup = GetComponentInParent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            // Subscribe to action
            if (actionReference?.action != null)
            {
                actionReference.action.performed += HandleActionPerformed;
            }

            // Static list for binding change notifications
            if (s_Instances == null)
                s_Instances = new System.Collections.Generic.List<InputButtonWithPrompt>();

            s_Instances.Add(this);

            if (s_Instances.Count == 1)
                InputSystem.onActionChange += OnActionChange;

            UpdateBindingDisplay();
        }

        private void OnDisable()
        {
            // Unsubscribe from action
            if (actionReference?.action != null)
            {
                actionReference.action.performed -= HandleActionPerformed;
            }

            // Remove from static list
            s_Instances.Remove(this);

            if (s_Instances.Count == 0)
            {
                s_Instances = null;
                InputSystem.onActionChange -= OnActionChange;
            }
        }

        #endregion

        #region Action Handling

        private void HandleActionPerformed(InputAction.CallbackContext context)
        {
            if (!IsInteractable)
            {
                if (enableDebugLogging)
                {
                    Logger.LogVerbose(LogSystems.Input, "Action ignored (not interactable)");
                }
                return;
            }

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.Input, "Action performed");
            }

            onActionPerformed?.Invoke();
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
                        InputBinding.DisplayStringOptions.DontUseShortDisplayNames
                    );
                }
                else if (action.bindings.Count > 0)
                {
                    displayString = action.GetBindingDisplayString(
                        0,
                        out deviceLayoutName,
                        out controlPath,
                        InputBinding.DisplayStringOptions.DontUseShortDisplayNames
                    );
                }
            }

            UpdateBuiltInDisplay(displayString, deviceLayoutName, controlPath);
            updateBindingUIEvent?.Invoke(null, displayString, deviceLayoutName, controlPath);
        }

        private void UpdateBuiltInDisplay(string displayString, string deviceLayoutName, string controlPath)
        {
            if (string.IsNullOrEmpty(displayString))
            {
                SetDisplayEmpty();
                return;
            }

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
        /// Manually triggers the action performed event.
        /// </summary>
        public void TriggerAction()
        {
            if (!IsInteractable)
                return;

            onActionPerformed?.Invoke();
        }

        /// <summary>
        /// Sets the icon provider at runtime.
        /// </summary>
        public void SetIconProvider(InputIconProvider_SO provider)
        {
            iconProvider = provider;
            UpdateBindingDisplay();
        }

        /// <summary>
        /// Starts an interactive rebind for this action.
        /// </summary>
        /// <param name="bindingIndex">The binding index to rebind (-1 for auto)</param>
        public void StartRebind(int bindingIndex = -1)
        {
            var manager = InputRebindManager.Instance;
            if (manager == null)
            {
                Logger.LogWarning(LogSystems.Input, "Cannot rebind: InputRebindManager not found");
                return;
            }

            if (bindingIndex < 0 && !string.IsNullOrEmpty(bindingId))
            {
                var action = actionReference?.action;
                if (action != null)
                {
                    bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == bindingId);
                }
            }

            manager.StartRebind(actionReference, bindingIndex);
        }

        /// <summary>
        /// Resets this action's bindings to default.
        /// </summary>
        public void ResetBinding()
        {
            var manager = InputRebindManager.Instance;
            if (manager == null)
            {
                Logger.LogWarning(LogSystems.Input, "Cannot reset: InputRebindManager not found");
                return;
            }

            var bindingIndex = -1;
            if (!string.IsNullOrEmpty(bindingId))
            {
                var action = actionReference?.action;
                if (action != null)
                {
                    bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == bindingId);
                }
            }

            manager.ResetBinding(actionReference, bindingIndex);
        }

        #endregion

#if UNITY_EDITOR
        private void Reset()
        {
            _canvasGroup = GetComponentInParent<CanvasGroup>();
            bindingText = GetComponentInChildren<TMP_Text>();
            bindingIcon = GetComponentInChildren<Image>();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateBindingDisplay();
            }
        }
#endif
    }
}
