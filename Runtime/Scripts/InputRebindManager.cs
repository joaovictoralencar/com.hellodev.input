using System;
using System.Collections.Generic;
using HelloDev.Logging;
using UnityEngine;
using UnityEngine.Events;
using Logger = HelloDev.Logging.Logger;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HelloDev.Input
{
    /// <summary>
    /// Manages input rebinding and persistence via PlayerPrefs.
    /// Based on Unity's Input System RebindSaveLoad sample.
    /// </summary>
    public class InputRebindManager : MonoBehaviour
    {
        #region Singleton

        private static InputRebindManager _instance;

        public static InputRebindManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<InputRebindManager>();
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Input Asset")]
        [Tooltip("The InputActionAsset containing actions to rebind")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Save Settings")]
        [Tooltip("PlayerPrefs key for storing binding overrides")]
        [SerializeField] private string playerPrefsKey = "InputBindings";

        [Tooltip("If true, automatically load bindings when enabled")]
        [SerializeField] private bool loadOnEnable = true;

        [Tooltip("If true, automatically save bindings when disabled")]
        [SerializeField] private bool saveOnDisable = true;

        [Header("Rebind Settings")]
        [Tooltip("Keys/buttons that cancel an interactive rebind")]
        [SerializeField] private string[] cancelBindingPaths = { "<Keyboard>/escape" };

        [Tooltip("Timeout in seconds for interactive rebind (0 = no timeout)")]
        [SerializeField] private float rebindTimeout = 5f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging;

        #endregion

        #region Events

        [Header("Events")]
        public UnityEvent OnRebindStarted = new();
        public UnityEvent OnRebindCompleted = new();
        public UnityEvent OnRebindCanceled = new();
        public UnityEvent OnBindingsLoaded = new();
        public UnityEvent OnBindingsSaved = new();

        #endregion

        #region Private Fields

#if ENABLE_INPUT_SYSTEM
        private InputActionRebindingExtensions.RebindingOperation _currentRebindOperation;
        private readonly Dictionary<string, InputAction> _runtimeActions = new();
#endif
        private InputActionReference _currentRebindAction;
        private int _currentRebindBindingIndex;

        #endregion

        #region Properties

        /// <summary>
        /// The InputActionAsset being managed.
        /// </summary>
        public InputActionAsset InputActions
        {
            get => inputActions;
            set => inputActions = value;
        }

        /// <summary>
        /// Whether a rebind operation is currently in progress.
        /// </summary>
        public bool IsRebinding => _currentRebindOperation != null;

        /// <summary>
        /// The action currently being rebound (null if not rebinding).
        /// </summary>
        public InputActionReference CurrentRebindAction => _currentRebindAction;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnEnable()
        {
            if (loadOnEnable)
            {
                LoadBindings();
            }
        }

        private void OnDisable()
        {
            CancelRebind();
            DisposeAllRuntimeActions();

            if (saveOnDisable)
            {
                SaveBindings();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            CancelRebind();
            DisposeAllRuntimeActions();
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Saves current binding overrides to PlayerPrefs.
        /// </summary>
        public void SaveBindings()
        {
            if (inputActions == null)
            {
                if (enableDebugLogging)
                {
                    Logger.LogWarning(LogSystems.InputRebind, "Cannot save: no InputActionAsset assigned");
                }
                return;
            }

#if ENABLE_INPUT_SYSTEM
            var rebinds = inputActions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(playerPrefsKey, rebinds);
            PlayerPrefs.Save();

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.InputRebind, $"Bindings saved to PlayerPrefs key: {playerPrefsKey}");
            }

            OnBindingsSaved?.Invoke();
#endif
        }

        /// <summary>
        /// Loads binding overrides from PlayerPrefs.
        /// </summary>
        public void LoadBindings()
        {
            if (inputActions == null)
            {
                if (enableDebugLogging)
                {
                    Logger.LogWarning(LogSystems.InputRebind, "Cannot load: no InputActionAsset assigned");
                }
                return;
            }

#if ENABLE_INPUT_SYSTEM
            var rebinds = PlayerPrefs.GetString(playerPrefsKey, string.Empty);

            if (string.IsNullOrEmpty(rebinds))
            {
                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.InputRebind, "No saved bindings found");
                }
                return;
            }

            inputActions.LoadBindingOverridesFromJson(rebinds);

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.InputRebind, $"Bindings loaded from PlayerPrefs key: {playerPrefsKey}");
            }

            OnBindingsLoaded?.Invoke();
#endif
        }

        /// <summary>
        /// Clears all saved binding overrides.
        /// </summary>
        public void ClearSavedBindings()
        {
            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.InputRebind, "Saved bindings cleared");
            }
        }

        #endregion

        #region Rebinding

        /// <summary>
        /// Starts an interactive rebind for the specified action.
        /// </summary>
        /// <param name="actionReference">The action to rebind</param>
        /// <param name="bindingIndex">The binding index to rebind (-1 for first non-composite binding)</param>
        public void StartRebind(InputActionReference actionReference, int bindingIndex = -1)
        {
#if ENABLE_INPUT_SYSTEM
            if (actionReference == null || actionReference.action == null)
            {
                Logger.LogWarning(LogSystems.InputRebind,"Cannot rebind: action reference is null");
                return;
            }

            var action = actionReference.action;

            // Find binding index if not specified
            if (bindingIndex < 0)
            {
                bindingIndex = FindFirstNonCompositeBinding(action);
            }

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                Logger.LogWarning(LogSystems.InputRebind, $"Invalid binding index {bindingIndex} for action {action.name}");
                return;
            }

            // Handle composite bindings
            if (action.bindings[bindingIndex].isComposite)
            {
                // Start rebinding the first part of the composite
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                {
                    PerformInteractiveRebind(actionReference, firstPartIndex, allCompositeParts: true);
                }
            }
            else
            {
                PerformInteractiveRebind(actionReference, bindingIndex, allCompositeParts: false);
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void PerformInteractiveRebind(InputActionReference actionReference, int bindingIndex, bool allCompositeParts)
        {
            var action = actionReference.action;
            var actionMap = action.actionMap;

            // Cancel any existing rebind
            _currentRebindOperation?.Cancel();

            // Store current rebind info
            _currentRebindAction = actionReference;
            _currentRebindBindingIndex = bindingIndex;

            // Disable the action map during rebind (not just the action)
            // This prevents other actions from interfering during rebind
            var wasEnabled = action.enabled;
            if (actionMap != null)
            {
                actionMap.Disable();
            }
            else
            {
                action.Disable();
            }

            // Create rebind operation
            _currentRebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .WithActionEventNotificationsBeingSuppressed()
                .OnCancel(operation =>
                {
                    CleanupRebind(action, actionMap, wasEnabled);
                    OnRebindCanceled?.Invoke();

                    if (enableDebugLogging)
                    {
                        Logger.Log(LogSystems.InputRebind, $"Rebind canceled for {action.name}");
                    }
                })
                .OnComplete(operation =>
                {
                    CleanupRebind(action, actionMap, wasEnabled);
                    OnRebindCompleted?.Invoke();

                    if (enableDebugLogging)
                    {
                        var binding = action.bindings[bindingIndex];
                        Logger.Log(LogSystems.InputRebind, $"Rebind completed for {action.name}: {binding.effectivePath}");
                    }

                    // If rebinding composite, continue to next part
                    if (allCompositeParts)
                    {
                        var nextBindingIndex = bindingIndex + 1;
                        if (nextBindingIndex < action.bindings.Count &&
                            action.bindings[nextBindingIndex].isPartOfComposite)
                        {
                            PerformInteractiveRebind(actionReference, nextBindingIndex, true);
                        }
                    }
                });

            // Add cancel bindings
            foreach (var cancelPath in cancelBindingPaths)
            {
                _currentRebindOperation.WithCancelingThrough(cancelPath);
            }

            // Set timeout if configured
            if (rebindTimeout > 0)
            {
                _currentRebindOperation.WithTimeout(rebindTimeout);
            }

            // Start the rebind
            _currentRebindOperation.Start();

            OnRebindStarted?.Invoke();

            if (enableDebugLogging)
            {
                var binding = action.bindings[bindingIndex];
                var partName = binding.isPartOfComposite ? $" (part: {binding.name})" : "";
                Logger.Log(LogSystems.InputRebind, $"Rebind started for {action.name}{partName}");
            }
        }

        private void CleanupRebind(InputAction action, InputActionMap actionMap, bool wasEnabled)
        {
            _currentRebindOperation?.Dispose();
            _currentRebindOperation = null;
            _currentRebindAction = null;
            _currentRebindBindingIndex = -1;

            // Re-enable the action map (or action if no map)
            if (wasEnabled)
            {
                if (actionMap != null)
                {
                    actionMap.Enable();
                }
                else
                {
                    action.Enable();
                }
            }
        }

        private int FindFirstNonCompositeBinding(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!action.bindings[i].isPartOfComposite)
                {
                    return i;
                }
            }
            return 0;
        }
#endif

        /// <summary>
        /// Cancels the current rebind operation.
        /// </summary>
        public void CancelRebind()
        {
#if ENABLE_INPUT_SYSTEM
            _currentRebindOperation?.Cancel();
#endif
        }

        /// <summary>
        /// Resets a specific action's bindings to default.
        /// </summary>
        public void ResetBinding(InputActionReference actionReference, int bindingIndex = -1)
        {
#if ENABLE_INPUT_SYSTEM
            if (actionReference == null || actionReference.action == null)
                return;

            var action = actionReference.action;

            if (bindingIndex < 0)
            {
                // Reset all bindings for this action
                action.RemoveAllBindingOverrides();
            }
            else if (action.bindings[bindingIndex].isComposite)
            {
                // Reset all parts of the composite
                for (var i = bindingIndex + 1;
                     i < action.bindings.Count && action.bindings[i].isPartOfComposite;
                     ++i)
                {
                    action.RemoveBindingOverride(i);
                }
            }
            else
            {
                action.RemoveBindingOverride(bindingIndex);
            }

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.InputRebind, $"Reset bindings for {action.name}");
            }
#endif
        }

        /// <summary>
        /// Resets all bindings to default.
        /// </summary>
        public void ResetAllBindings()
        {
#if ENABLE_INPUT_SYSTEM
            if (inputActions == null)
                return;

            inputActions.RemoveAllBindingOverrides();
            ClearSavedBindings();

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.InputRebind, "All bindings reset to default");
            }
#endif
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the display string for an action's current binding.
        /// </summary>
        public string GetBindingDisplayString(InputActionReference actionReference, int bindingIndex = -1)
        {
#if ENABLE_INPUT_SYSTEM
            if (actionReference == null || actionReference.action == null)
                return string.Empty;

            var action = actionReference.action;

            if (bindingIndex < 0)
            {
                return action.GetBindingDisplayString();
            }

            return action.GetBindingDisplayString(bindingIndex);
#else
            return string.Empty;
#endif
        }

        /// <summary>
        /// Checks if an action has any binding overrides.
        /// </summary>
        public bool HasBindingOverride(InputActionReference actionReference)
        {
#if ENABLE_INPUT_SYSTEM
            if (actionReference == null || actionReference.action == null)
                return false;

            var action = actionReference.action;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!string.IsNullOrEmpty(action.bindings[i].overridePath))
                {
                    return true;
                }
            }
#endif
            return false;
        }

        #endregion

        #region Runtime Action Management

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Creates a temporary InputAction with keyboard and gamepad bindings.
        /// The action is enabled immediately and tracked for disposal.
        /// </summary>
        /// <param name="actionName">Unique name for the action</param>
        /// <param name="keyboardBinding">Keyboard binding path (e.g., "&lt;Keyboard&gt;/k")</param>
        /// <param name="gamepadBinding">Gamepad binding path (e.g., "&lt;Gamepad&gt;/buttonSouth")</param>
        /// <returns>The created InputAction, or existing action if name already exists</returns>
        public InputAction CreateRuntimeAction(string actionName, string keyboardBinding, string gamepadBinding)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                Logger.LogWarning(LogSystems.InputRebind, "Cannot create runtime action: actionName is null or empty");
                return null;
            }

            // Return existing action if it already exists (idempotent)
            if (_runtimeActions.TryGetValue(actionName, out var existingAction))
            {
                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.InputRebind, $"Runtime action '{actionName}' already exists, returning existing action");
                }
                return existingAction;
            }

            // Create new action
            var action = new InputAction(
                name: actionName,
                type: InputActionType.Button
            );

            // Add keyboard binding
            if (!string.IsNullOrEmpty(keyboardBinding))
            {
                action.AddBinding(keyboardBinding)
                    .WithGroup("Keyboard and Mouse");
            }

            // Add gamepad binding
            if (!string.IsNullOrEmpty(gamepadBinding))
            {
                action.AddBinding(gamepadBinding)
                    .WithGroup("Gamepad");
            }

            // Enable the action
            action.Enable();

            // Track for disposal
            _runtimeActions[actionName] = action;

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.InputRebind, $"Created runtime action '{actionName}' with bindings: keyboard='{keyboardBinding}', gamepad='{gamepadBinding}'");
            }

            return action;
        }

        /// <summary>
        /// Creates a temporary InputAction with a single binding.
        /// The action is enabled immediately and tracked for disposal.
        /// </summary>
        /// <param name="actionName">Unique name for the action</param>
        /// <param name="binding">Binding path (e.g., "&lt;Keyboard&gt;/k")</param>
        /// <returns>The created InputAction, or existing action if name already exists</returns>
        public InputAction CreateRuntimeAction(string actionName, string binding)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                Logger.LogWarning(LogSystems.InputRebind, "Cannot create runtime action: actionName is null or empty");
                return null;
            }

            // Return existing action if it already exists (idempotent)
            if (_runtimeActions.TryGetValue(actionName, out var existingAction))
            {
                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.InputRebind, $"Runtime action '{actionName}' already exists, returning existing action");
                }
                return existingAction;
            }

            // Create new action
            var action = new InputAction(
                name: actionName,
                type: InputActionType.Button
            );

            // Add binding
            if (!string.IsNullOrEmpty(binding))
            {
                action.AddBinding(binding);
            }

            // Enable the action
            action.Enable();

            // Track for disposal
            _runtimeActions[actionName] = action;

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.InputRebind, $"Created runtime action '{actionName}' with binding: '{binding}'");
            }

            return action;
        }

        /// <summary>
        /// Gets an existing runtime action by name.
        /// </summary>
        /// <param name="actionName">The name of the action</param>
        /// <returns>The action, or null if not found</returns>
        public InputAction GetRuntimeAction(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return null;

            _runtimeActions.TryGetValue(actionName, out var action);
            return action;
        }

        /// <summary>
        /// Checks if a runtime action with the given name exists.
        /// </summary>
        /// <param name="actionName">The name of the action</param>
        /// <returns>True if the action exists</returns>
        public bool HasRuntimeAction(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return false;

            return _runtimeActions.ContainsKey(actionName);
        }

        /// <summary>
        /// Disposes and removes a runtime action by name.
        /// </summary>
        /// <param name="actionName">The name of the action to dispose</param>
        /// <returns>True if action was found and disposed</returns>
        public bool DisposeRuntimeAction(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return false;

            if (!_runtimeActions.TryGetValue(actionName, out var action))
                return false;

            // Disable and dispose
            action.Disable();
            action.Dispose();

            // Remove from dictionary
            _runtimeActions.Remove(actionName);

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.InputRebind, $"Disposed runtime action '{actionName}'");
            }

            return true;
        }

        /// <summary>
        /// Disposes all runtime actions.
        /// Called automatically on disable and destroy.
        /// </summary>
        public void DisposeAllRuntimeActions()
        {
            if (_runtimeActions.Count == 0)
                return;

            foreach (var kvp in _runtimeActions)
            {
                kvp.Value.Disable();
                kvp.Value.Dispose();
            }

            var count = _runtimeActions.Count;
            _runtimeActions.Clear();

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.InputRebind, $"Disposed {count} runtime action(s)");
            }
        }
#else
        public object CreateRuntimeAction(string actionName, string keyboardBinding, string gamepadBinding) => null;
        public object CreateRuntimeAction(string actionName, string binding) => null;
        public object GetRuntimeAction(string actionName) => null;
        public bool HasRuntimeAction(string actionName) => false;
        public bool DisposeRuntimeAction(string actionName) => false;
        public void DisposeAllRuntimeActions() { }
#endif

        #endregion
    }
}
