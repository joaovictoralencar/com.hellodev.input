using HelloDev.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.Input
{
    /// <summary>
    /// Triggers a UnityEvent when an input action is performed.
    /// Uses InputActionReference for rebinding compatibility.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component listens to an <see cref="InputActionReference"/> and invokes
    /// <see cref="OnActionPerformed"/> when the action is triggered. It works with
    /// the rebinding system - if bindings change, the component automatically responds
    /// to the new bindings.
    /// </para>
    /// </remarks>
    [AddComponentMenu("HelloDev/Input/Input Action Button")]
    public class InputActionButton : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input Action")]
        [Tooltip("Reference to the action that triggers this button")]
        [SerializeField] private InputActionReference actionReference;

        [Header("Events")]
        [Tooltip("Invoked when the action is performed")]
        [SerializeField] private UnityEvent onActionPerformed = new();

        [Header("Options")]
        [Tooltip("If true, only trigger when the GameObject is interactable (checks CanvasGroup)")]
        [SerializeField] private bool respectCanvasGroup = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging;

        #endregion

        #region Private Fields

        private CanvasGroup _canvasGroup;

        #endregion

        #region Properties

        /// <summary>
        /// The action reference this button responds to.
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
            if (actionReference?.action != null)
            {
                actionReference.action.performed += HandleActionPerformed;

                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.Input, $"Subscribed to action '{actionReference.action.name}'");
                }
            }
        }

        private void OnDisable()
        {
            if (actionReference?.action != null)
            {
                actionReference.action.performed -= HandleActionPerformed;

                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.Input, $"Unsubscribed from action '{actionReference.action.name}'");
                }
            }
        }

        #endregion

        #region Event Handling

        private void HandleActionPerformed(InputAction.CallbackContext context)
        {
            if (!IsInteractable)
            {
                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.Input, $" Action '{actionReference.action.name}' ignored (not interactable)", this);
                }
                return;
            }

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.Input, $" Action '{actionReference.action.name}' performed", this);
            }

            onActionPerformed?.Invoke();
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
        /// Gets the binding index for a specific control scheme.
        /// </summary>
        /// <param name="controlScheme">The control scheme name (e.g., "Gamepad", "Keyboard&Mouse")</param>
        /// <returns>The binding index, or -1 if not found</returns>
        public int GetBindingIndexForScheme(string controlScheme)
        {
            var action = actionReference?.action;
            if (action == null || string.IsNullOrEmpty(controlScheme))
                return -1;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.isPartOfComposite)
                    continue;

                if (!string.IsNullOrEmpty(binding.groups) &&
                    binding.groups.Contains(controlScheme))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Starts an interactive rebind for this action.
        /// Requires InputRebindManager in scene.
        /// </summary>
        /// <param name="bindingIndex">The binding index to rebind (-1 for first non-composite)</param>
        public void StartRebind(int bindingIndex = -1)
        {
            var manager = InputRebindManager.Instance;
            if (manager == null)
            {
                Logger.LogWarning(LogSystems.Input, "Cannot rebind: InputRebindManager not found");
                return;
            }

            manager.StartRebind(actionReference, bindingIndex);
        }

        /// <summary>
        /// Resets this action's bindings to default.
        /// Requires InputRebindManager in scene.
        /// </summary>
        /// <param name="bindingIndex">The binding index to reset (-1 for all bindings)</param>
        public void ResetBinding(int bindingIndex = -1)
        {
            var manager = InputRebindManager.Instance;
            if (manager == null)
            {
                Logger.LogWarning(LogSystems.Input, "Cannot reset: InputRebindManager not found");
                return;
            }

            manager.ResetBinding(actionReference, bindingIndex);
        }

        #endregion

#if UNITY_EDITOR
        private void Reset()
        {
            // Auto-find CanvasGroup
            _canvasGroup = GetComponentInParent<CanvasGroup>();
        }
#endif
    }
}
