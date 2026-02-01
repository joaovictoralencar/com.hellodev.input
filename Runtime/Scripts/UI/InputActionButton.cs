using System.Collections.Generic;
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
        [Tooltip("If true, this component will enable/disable the action. Disable if another system (e.g., PlayerInput) manages the action.")]
        [SerializeField] private bool manageActionState = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging;

        #endregion

        #region Static Reference Counting

        /// <summary>
        /// Tracks how many InputActionButton instances are actively managing each action.
        /// Prevents OnDisable from disabling an action that another instance still needs.
        /// </summary>
        private static readonly Dictionary<InputAction, int> _actionRefCounts = new();

        private static void IncrementRefCount(InputAction action)
        {
            _actionRefCounts.TryGetValue(action, out int count);
            _actionRefCounts[action] = count + 1;
        }

        private static bool DecrementRefCount(InputAction action)
        {
            if (!_actionRefCounts.TryGetValue(action, out int count)) return true;
            count--;
            if (count <= 0)
            {
                _actionRefCounts.Remove(action);
                return true; // Last reference, safe to disable
            }
            _actionRefCounts[action] = count;
            return false; // Other instances still active
        }

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
                // Unsubscribe and disable old action
                if (enabled && actionReference?.action != null)
                {
                    actionReference.action.performed -= HandleActionPerformed;
                    if (manageActionState && DecrementRefCount(actionReference.action))
                    {
                        actionReference.action.Disable();
                    }
                }

                actionReference = value;

                // Subscribe and enable new action
                if (enabled && actionReference?.action != null)
                {
                    actionReference.action.performed += HandleActionPerformed;
                    if (manageActionState)
                    {
                        IncrementRefCount(actionReference.action);
                        actionReference.action.Enable();
                    }
                }
            }
        }

        /// <summary>
        /// Event invoked when the action is performed.
        /// </summary>
        public UnityEvent OnActionPerformed => onActionPerformed;

        /// <summary>
        /// If true, this component enables/disables the action.
        /// Set to false if another system (e.g., PlayerInput) manages the action state.
        /// </summary>
        public bool ManageActionState
        {
            get => manageActionState;
            set => manageActionState = value;
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (actionReference?.action != null)
            {
                actionReference.action.performed += HandleActionPerformed;

                if (manageActionState)
                {
                    IncrementRefCount(actionReference.action);
                    actionReference.action.Enable();
                }

                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.Input, $"Subscribed to action '{actionReference.action.name}' (enabled: {actionReference.action.enabled})");
                }
            }
        }

        private void OnDisable()
        {
            if (actionReference?.action != null)
            {
                actionReference.action.performed -= HandleActionPerformed;

                if (manageActionState && DecrementRefCount(actionReference.action))
                {
                    actionReference.action.Disable();
                }

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
            if (this == null) return;

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

    }
}
