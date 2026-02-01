using HelloDev.Logging;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace HelloDev.Input
{
    /// <summary>
    /// Coordinator that finds and syncs InputActionButton and InputPromptDisplay children.
    /// Follows the Single Responsibility Principle - this component only coordinates,
    /// it does NOT duplicate the logic of its children.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience component that coordinates child components:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="InputActionButton"/> - handles input and fires events</item>
    /// <item><see cref="InputPromptDisplay"/> - displays the binding icon/text</item>
    /// </list>
    /// <para>
    /// The coordinator syncs the ActionReference and BindingId to both children,
    /// and forwards the OnActionPerformed event from InputActionButton.
    /// </para>
    /// <para>
    /// <b>Pyramid Architecture:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>Level 1: InputPromptDisplay alone = just displays key</item>
    /// <item>Level 2: InputActionButton + InputPromptDisplay = key display + input handling</item>
    /// <item>Level 3: InputButtonWithPrompt coordinates both = unified API</item>
    /// </list>
    /// </remarks>
    [AddComponentMenu("HelloDev/Input/Input Button With Prompt")]
    public class InputButtonWithPrompt : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input Action")]
        [Tooltip("The action - will be synced to child InputActionButton and InputPromptDisplay")]
        [SerializeField] private InputActionReference actionReference;

        [Tooltip("ID of the specific binding to display (selected via dropdown in editor)")]
        [SerializeField] private string bindingId;

        [Header("Icon Provider")]
        [Tooltip("Will be synced to child InputPromptDisplay")]
        [SerializeField] private InputIconProvider_SO iconProvider;

        [Header("Child References (Auto-found if empty)")]
        [Tooltip("Child InputActionButton - auto-found in children if not set")]
        [SerializeField] private InputActionButton inputActionButton;

        [Tooltip("Child InputPromptDisplay - auto-found in children if not set")]
        [SerializeField] private InputPromptDisplay inputPromptDisplay;

        [Header("Events")]
        [Tooltip("Invoked when the action is performed (forwarded from child InputActionButton)")]
        [SerializeField] private UnityEvent onActionPerformed = new();

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging;

        #endregion

        #region Private Fields

        private bool _isSubscribed;

        #endregion

        #region Properties

        /// <summary>
        /// The action reference. Setting this syncs to all child components.
        /// </summary>
        public InputActionReference ActionReference
        {
            get => actionReference;
            set
            {
                actionReference = value;
                SyncToChildren();
            }
        }

        /// <summary>
        /// ID of the binding being displayed. Setting this syncs to child InputPromptDisplay.
        /// </summary>
        public string BindingId
        {
            get => bindingId;
            set
            {
                bindingId = value;
                SyncToChildren();
            }
        }

        /// <summary>
        /// The icon provider. Setting this syncs to child InputPromptDisplay.
        /// </summary>
        public InputIconProvider_SO IconProvider
        {
            get => iconProvider;
            set
            {
                iconProvider = value;
                SyncToChildren();
            }
        }

        /// <summary>
        /// Event invoked when the action is performed (forwarded from child InputActionButton).
        /// </summary>
        public UnityEvent OnActionPerformed => onActionPerformed;

        /// <summary>
        /// The child InputActionButton (may be null if not found).
        /// </summary>
        public InputActionButton InputActionButton => inputActionButton;

        /// <summary>
        /// The child InputPromptDisplay (may be null if not found).
        /// </summary>
        public InputPromptDisplay InputPromptDisplay => inputPromptDisplay;

        /// <summary>
        /// Whether this button is currently interactable (delegates to child InputActionButton).
        /// Returns true if no InputActionButton child exists.
        /// </summary>
        public bool IsInteractable => inputActionButton == null || inputActionButton.isActiveAndEnabled;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            FindChildComponents();
        }

        private void OnEnable()
        {
            SyncToChildren();
            SubscribeToChildEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromChildEvents();
        }

        #endregion

        #region Child Management

        /// <summary>
        /// Finds child InputActionButton and InputPromptDisplay if not already assigned.
        /// </summary>
        public void FindChildComponents()
        {
            if (inputActionButton == null)
                inputActionButton = GetComponentInChildren<InputActionButton>();

            if (inputPromptDisplay == null)
                inputPromptDisplay = GetComponentInChildren<InputPromptDisplay>();

            if (enableDebugLogging)
            {
                Logging.Logger.Log(LogSystems.Input, $"Found children - " +
                                                $"InputActionButton: {(inputActionButton != null ? "Yes" : "No")}, " +
                                                $"InputPromptDisplay: {(inputPromptDisplay != null ? "Yes" : "No")}", this);
            }
        }

        /// <summary>
        /// Syncs ActionReference, BindingId, and IconProvider to child components.
        /// </summary>
        public void SyncToChildren()
        {
            // Sync to InputActionButton
            if (inputActionButton != null)
            {
                inputActionButton.ActionReference = actionReference;

                if (enableDebugLogging)
                {
                    Logging.Logger.Log(LogSystems.Input,$"Synced ActionReference to InputActionButton: {(actionReference != null ? actionReference.action?.name : "null")}", this);
                }
            }

            // Sync to InputPromptDisplay
            if (inputPromptDisplay != null)
            {
                inputPromptDisplay.ActionReference = actionReference;
                inputPromptDisplay.BindingId = bindingId;

                if (iconProvider != null)
                {
                    inputPromptDisplay.SetIconProvider(iconProvider);
                }

                if (enableDebugLogging)
                {
                    Logging.Logger.Log(LogSystems.Input,$"Synced to InputPromptDisplay. Action={actionReference?.action?.name}, BindingId={bindingId}", this);
                }
            }
        }

        private void SubscribeToChildEvents()
        {
            if (_isSubscribed)
                return;

            if (inputActionButton != null)
            {
                inputActionButton.OnActionPerformed.SafeSubscribe(HandleChildActionPerformed);
                _isSubscribed = true;

                if (enableDebugLogging)
                {
                    Logging.Logger.Log(LogSystems.Input,"Subscribed to InputActionButton.OnActionPerformed", this);
                }
            }
        }

        private void UnsubscribeFromChildEvents()
        {
            if (!_isSubscribed)
                return;

            if (inputActionButton != null)
            {
                inputActionButton.OnActionPerformed.SafeUnsubscribe(HandleChildActionPerformed);
            }

            _isSubscribed = false;
        }

        private void HandleChildActionPerformed()
        {
            if (enableDebugLogging)
            {
                Logging.Logger.Log(LogSystems.Input,"Forwarding OnActionPerformed from child", this);
            }

            onActionPerformed?.Invoke();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually triggers the action (delegates to child InputActionButton).
        /// Does nothing if no InputActionButton child exists.
        /// </summary>
        public void TriggerAction()
        {
            if (inputActionButton != null)
            {
                inputActionButton.TriggerAction();
            }
            else
            {
                // No InputActionButton, just fire our event directly
                onActionPerformed?.Invoke();
            }
        }

        /// <summary>
        /// Updates the binding display (delegates to child InputPromptDisplay).
        /// Does nothing if no InputPromptDisplay child exists.
        /// </summary>
        public void UpdateBindingDisplay()
        {
            inputPromptDisplay?.UpdateBindingDisplay();
        }

        /// <summary>
        /// Starts an interactive rebind for this action.
        /// Requires InputRebindManager in scene and child InputActionButton.
        /// </summary>
        /// <param name="bindingIndex">The binding index to rebind (-1 for auto)</param>
        public void StartRebind(int bindingIndex = -1)
        {
            if (inputActionButton != null)
            {
                inputActionButton.StartRebind(bindingIndex);
            }
            else
            {
                Debug.LogWarning("[InputButtonWithPrompt] Cannot rebind: no InputActionButton child found", this);
            }
        }

        /// <summary>
        /// Resets this action's bindings to default.
        /// Requires InputRebindManager in scene and child InputActionButton.
        /// </summary>
        /// <param name="bindingIndex">The binding index to reset (-1 for all)</param>
        public void ResetBinding(int bindingIndex = -1)
        {
            if (inputActionButton != null)
            {
                inputActionButton.ResetBinding(bindingIndex);
            }
            else
            {
                Debug.LogWarning("[InputButtonWithPrompt] Cannot reset binding: no InputActionButton child found", this);
            }
        }

        /// <summary>
        /// Gets the binding index for the current bindingId.
        /// Returns -1 if not found or no InputPromptDisplay child exists.
        /// </summary>
        public int GetBindingIndex()
        {
            return inputPromptDisplay?.GetBindingIndex() ?? -1;
        }

        #endregion

#if UNITY_EDITOR
        private void Reset()
        {
            FindChildComponents();
        }

        private void OnValidate()
        {
            // In edit mode, try to find children if not assigned
            if (!Application.isPlaying)
            {
                if (inputActionButton == null)
                    inputActionButton = GetComponentInChildren<InputActionButton>();
                if (inputPromptDisplay == null)
                    inputPromptDisplay = GetComponentInChildren<InputPromptDisplay>();
            }
            else
            {
                SyncToChildren();
            }
        }
#endif
    }
}
