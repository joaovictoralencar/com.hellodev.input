using HelloDev.UI.Default;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HelloDev.Input
{
    /// <summary>
    /// Adds input action support to a button.
    /// When the configured input is pressed, triggers the button click.
    /// Creates runtime action on enable, disposes on disable.
    /// </summary>
    public class InputActionButton : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Button Reference")]
        [Tooltip("HelloDev UIButton to trigger (uses UIButton)")]
        [SerializeField] private UIButton uiButton;

        [Header("Input Bindings")]
        [Tooltip("Unique name for this input action")]
        [SerializeField] private string actionName = "MyAction";

        [Tooltip("Keyboard binding path (e.g., <Keyboard>/space)")]
        [SerializeField] private string keyboardBinding = "<Keyboard>/space";

        [Tooltip("Gamepad binding path (e.g., <Gamepad>/buttonSouth)")]
        [SerializeField] private string gamepadBinding = "<Gamepad>/buttonSouth";

        [Header("Prompt Display (Optional)")]
        [Tooltip("Optional InputPromptDisplay to show binding icon/text")]
        [SerializeField] private InputPromptDisplay promptDisplay;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging;

        #endregion

        #region Private Fields

        private InputAction _action;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the unique action name for this button.
        /// </summary>
        public string ActionName => actionName;

        /// <summary>
        /// Gets the runtime InputAction (null when disabled).
        /// </summary>
        public InputAction Action => _action;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            InitializeAction();
        }

        private void OnDisable()
        {
            DisposeAction();
        }

        #endregion

        #region Input Action Management

        private void InitializeAction()
        {
            var manager = InputRebindManager.Instance;
            if (manager == null)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[InputActionButton] InputRebindManager not found, input action '{actionName}' will not work", this);
                }
                return;
            }

            // Create runtime action with both bindings
            _action = manager.CreateRuntimeAction(actionName, keyboardBinding, gamepadBinding);

            if (_action != null)
            {
                _action.performed += OnActionPerformed;

                // Update prompt display if assigned
                if (promptDisplay != null)
                {
                    promptDisplay.SetAction(_action);
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"[InputActionButton] Action '{actionName}' initialized", this);
                }
            }
        }

        private void DisposeAction()
        {
            // Unsubscribe from action
            if (_action != null)
            {
                _action.performed -= OnActionPerformed;
                _action = null;
            }

            // Clear prompt display
            if (promptDisplay != null)
            {
                promptDisplay.ClearDirectAction();
            }

            // Dispose action from manager
            var manager = InputRebindManager.Instance;
            if (manager != null)
            {
                manager.DisposeRuntimeAction(actionName);

                if (enableDebugLogging)
                {
                    Debug.Log($"[InputActionButton] Action '{actionName}' disposed", this);
                }
            }
        }

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            // Trigger UIButton
            if (uiButton != null && uiButton.IsInteractable)
            {
                if (enableDebugLogging)
                {
                    Debug.Log($"[InputActionButton] Action '{actionName}' triggered UIButton click", this);
                }

                uiButton.OnClick?.Invoke();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Configures the input bindings at runtime.
        /// Must be called before OnEnable or while disabled.
        /// </summary>
        public void Configure(string name, string keyboard, string gamepad)
        {
            actionName = name;
            keyboardBinding = keyboard;
            gamepadBinding = gamepad;
        }

        /// <summary>
        /// Sets the UIButton reference at runtime.
        /// </summary>
        public void SetButton(UIButton button)
        {
            uiButton = button;
        }

        /// <summary>
        /// Sets the prompt display reference at runtime.
        /// </summary>
        public void SetPromptDisplay(InputPromptDisplay display)
        {
            promptDisplay = display;

            // Update display with current action if we have one
            if (_action != null && promptDisplay != null)
            {
                promptDisplay.SetAction(_action);
            }
        }

        /// <summary>
        /// Gets the keyboard binding path.
        /// </summary>
        public string KeyboardBinding => keyboardBinding;

        /// <summary>
        /// Gets the gamepad binding path.
        /// </summary>
        public string GamepadBinding => gamepadBinding;

        #endregion

#if UNITY_EDITOR
        private void Reset()
        {
            // Auto-find UIButton on same GameObject
            if (uiButton == null)
            {
                uiButton = GetComponent<UIButton>();
            }
        }

        private void OnValidate()
        {
            // Generate unique action name if using default
            if (actionName == "MyAction" && !string.IsNullOrEmpty(gameObject.name))
            {
                actionName = $"{gameObject.name}_Action";
            }
        }
#endif
    }
}
