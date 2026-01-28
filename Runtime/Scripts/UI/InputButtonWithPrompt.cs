using HelloDev.UI.Default;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HelloDev.Input
{
    /// <summary>
    /// Orchestrator that composes InputActionButton and InputPromptDisplay.
    /// Provides a single component for buttons that respond to input and display their binding.
    /// Respects SRP by delegating to the specialized components.
    /// </summary>
    [AddComponentMenu("HelloDev/Input/Input Button With Prompt")]
    public class InputButtonWithPrompt : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Components (auto-created if null)")]
        [Tooltip("The InputActionButton component")]
        [SerializeField] private InputActionButton inputActionButton;

        [Tooltip("The InputPromptDisplay component")]
        [SerializeField] private InputPromptDisplay promptDisplay;

        [Header("Button")]
        [Tooltip("The UIButton to trigger when input is performed")]
        [SerializeField] private UIButton uiButton;

        [Header("Input Bindings")]
        [Tooltip("Unique name for this input action")]
        [SerializeField] private string actionName = "MyAction";

        [Tooltip("Keyboard binding path (e.g., <Keyboard>/k)")]
        [SerializeField] private string keyboardBinding = "<Keyboard>/space";

        [Tooltip("Gamepad binding path (e.g., <Gamepad>/buttonSouth)")]
        [SerializeField] private string gamepadBinding = "<Gamepad>/buttonSouth";

        [Header("Prompt Display")]
        [Tooltip("Text component for displaying binding text (optional)")]
        [SerializeField] private TMP_Text promptText;

        [Tooltip("Image component for displaying binding icon (optional)")]
        [SerializeField] private Image iconImage;

        [Tooltip("Format string for text display. {0} = binding text")]
        [SerializeField] private string textFormat = "[{0}]";

        [Tooltip("If true, prefer showing icon over text when both are available")]
        [SerializeField] private bool preferIcon = true;

        [Tooltip("If true, hide icon when showing text and vice versa")]
        [SerializeField] private bool exclusiveDisplay = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the InputActionButton component.
        /// </summary>
        public InputActionButton ActionButton => inputActionButton;

        /// <summary>
        /// Gets the InputPromptDisplay component.
        /// </summary>
        public InputPromptDisplay PromptDisplay => promptDisplay;

        /// <summary>
        /// Gets the action name.
        /// </summary>
        public string ActionName => actionName;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            EnsureComponents();
        }

        private void OnEnable()
        {
            ConfigureComponents();
        }

        #endregion

        #region Component Management

        private void EnsureComponents()
        {
            // Get or create InputActionButton
            if (inputActionButton == null)
            {
                inputActionButton = GetComponent<InputActionButton>();
                if (inputActionButton == null)
                {
                    inputActionButton = gameObject.AddComponent<InputActionButton>();
                    if (enableDebugLogging)
                    {
                        Debug.Log("[InputButtonWithPrompt] Created InputActionButton component", this);
                    }
                }
            }

            // Get or create InputPromptDisplay only if we have UI elements
            bool hasPromptUI = promptText != null || iconImage != null;
            if (promptDisplay == null && hasPromptUI)
            {
                promptDisplay = GetComponent<InputPromptDisplay>();
                if (promptDisplay == null)
                {
                    promptDisplay = gameObject.AddComponent<InputPromptDisplay>();
                    if (enableDebugLogging)
                    {
                        Debug.Log("[InputButtonWithPrompt] Created InputPromptDisplay component", this);
                    }
                }
            }
        }

        private void ConfigureComponents()
        {
            // Configure InputActionButton
            if (inputActionButton != null)
            {
                inputActionButton.Configure(actionName, keyboardBinding, gamepadBinding);
                inputActionButton.SetButton(uiButton);

                // Wire up prompt display
                if (promptDisplay != null)
                {
                    inputActionButton.SetPromptDisplay(promptDisplay);
                }
            }

            // Configure InputPromptDisplay
            if (promptDisplay != null)
            {
                promptDisplay.ConfigureUI(promptText, iconImage);
                promptDisplay.ConfigureDisplayOptions(textFormat, preferIcon, exclusiveDisplay);
            }
        }

        #endregion

        #region Public Methods
        

        /// <summary>
        /// Sets the UIButton at runtime.
        /// </summary>
        public void SetButton(UIButton button)
        {
            uiButton = button;
            inputActionButton?.SetButton(button);
        }

        /// <summary>
        /// Updates the input bindings at runtime.
        /// Note: Changes take effect on next enable cycle.
        /// </summary>
        public void SetBindings(string keyboard, string gamepad)
        {
            keyboardBinding = keyboard;
            gamepadBinding = gamepad;
            inputActionButton?.Configure(actionName, keyboardBinding, gamepadBinding);
        }

        #endregion

#if UNITY_EDITOR
        private void Reset()
        {
            // Auto-find UIButton on same GameObject
            if (uiButton == null)
            {
                uiButton = GetComponent<UIButton>();
            }

            // Auto-find prompt text in children
            if (promptText == null)
            {
                promptText = GetComponentInChildren<TMP_Text>();
            }

            // Auto-find existing components
            if (inputActionButton == null)
            {
                inputActionButton = GetComponent<InputActionButton>();
            }

            if (promptDisplay == null)
            {
                promptDisplay = GetComponent<InputPromptDisplay>();
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
