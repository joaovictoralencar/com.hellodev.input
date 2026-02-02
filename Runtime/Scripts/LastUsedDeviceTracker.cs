using System;
using System.Linq;
using HelloDev.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.Input
{
    /// <summary>
    /// Singleton service that detects when the player switches input devices.
    /// Uses InputSystem.onActionChange for efficient, action-based device tracking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component tracks device changes through three mechanisms (in priority order):
    /// </para>
    /// <list type="bullet">
    /// <item><b>Priority 1 (onActionChange)</b>: Tracks device via action performs (clean, semantic)</item>
    /// <item><b>Priority 2 (onEvent)</b>: Fallback to raw input if needed (optional, configurable)</item>
    /// <item><b>Priority 3 (onDeviceChange)</b>: Monitors device lifecycle (plug/unplug)</item>
    /// </list>
    /// <para>
    /// <b>Keyboard/Mouse Grouping:</b> Keyboard and Mouse are treated as the same device group.
    /// Switching between them does not trigger a device change event.
    /// </para>
    /// <para>
    /// <b>Race Condition Safety:</b> Uses <see cref="OnInstanceReady"/> event to notify
    /// components when the tracker is initialized, preventing null reference errors.
    /// </para>
    /// </remarks>
    [AddComponentMenu("HelloDev/Input/Last Used Device Tracker")]
    public class LastUsedDeviceTracker : MonoBehaviour
    {
        #region Singleton

        private static LastUsedDeviceTracker _instance;

        /// <summary>
        /// Singleton instance. Returns null if not yet initialized.
        /// </summary>
        public static LastUsedDeviceTracker Instance => _instance;

        /// <summary>
        /// Event fired when the singleton instance is ready.
        /// Use this to safely subscribe to DeviceChanged without race conditions.
        /// </summary>
        public static event Action<LastUsedDeviceTracker> OnInstanceReady;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the active input device changes.
        /// Parameters: (previousDevice, newDevice)
        /// </summary>
        public event Action<InputDevice, InputDevice> DeviceChanged;

        #endregion

        #region Properties

        /// <summary>
        /// The most recently used input device.
        /// </summary>
        public InputDevice CurrentDevice { get; private set; }

        /// <summary>
        /// Current device group (Keyboard, Mouse = "KeyboardMouse"; Gamepad = "Gamepad").
        /// </summary>
        public string CurrentDeviceGroup { get; private set; }

        /// <summary>
        /// Gets the device layout appropriate for icon mapping.
        /// Returns "Keyboard" for Mouse devices (since Mouse has no separate icons).
        /// </summary>
        public string CurrentDeviceLayoutForIcons
        {
            get
            {
                if (CurrentDevice == null)
                    return null;

                // Redirect Mouse to Keyboard for icon resolution
                if (CurrentDevice is Mouse)
                    return "Keyboard";

                return CurrentDevice.layout;
            }
        }

        #endregion

        #region Configuration

        [Header("Tracking Options")]
        [Tooltip("Track device via action performs (Priority 1 - recommended)")]
        [SerializeField] private bool trackViaActions = true;

        [Tooltip("Track device via raw input events (Priority 2 - fallback if no actions configured)")]
        [SerializeField] private bool trackViaRawInput = false;

        [Tooltip("Monitor device plug/unplug (Priority 3 - always recommended)")]
        [SerializeField] private bool monitorDeviceLifecycle = true;

        [Header("Behavior")]
        [Tooltip("Minimum time between device switches to prevent spam")]
        [SerializeField] private float debounceTime = 0.1f;

        [Tooltip("Treat keyboard and mouse as the same device group (recommended)")]
        [SerializeField] private bool groupKeyboardMouse = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging;

        #endregion

        #region Private Fields

        private float _lastDeviceChangeTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                if (enableDebugLogging)
                {
                    Logger.LogWarning(LogSystems.Input,
                        "Multiple LastUsedDeviceTracker instances detected. Destroying duplicate.", this);
                }
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize with first available device
            InitializeCurrentDevice();

            // Fire ready event for components waiting on initialization
            OnInstanceReady?.Invoke(this);

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.Input, "LastUsedDeviceTracker initialized and ready.");
            }
        }

        private void OnEnable()
        {
            // Priority 1: Track via action performs (most efficient)
            if (trackViaActions)
            {
                InputSystem.onActionChange += OnActionChange;
            }

            // Priority 2: Fallback to raw input events (optional)
            if (trackViaRawInput)
            {
                InputSystem.onEvent += OnInputEvent;
            }

            // Priority 3: Device lifecycle monitoring (plug/unplug)
            if (monitorDeviceLifecycle)
            {
                InputSystem.onDeviceChange += OnDeviceChange;
            }

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.Input,
                    $"LastUsedDeviceTracker enabled. Tracking: Actions={trackViaActions}, RawInput={trackViaRawInput}, Lifecycle={monitorDeviceLifecycle}");
            }
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= OnActionChange;
            InputSystem.onEvent -= OnInputEvent;
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        private void InitializeCurrentDevice()
        {
            // Prefer gamepad if connected, otherwise keyboard
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                SetCurrentDevice(gamepad, silent: true);
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                SetCurrentDevice(keyboard, silent: true);
            }

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.Input,
                    $"Initialized with device: {CurrentDevice?.name ?? "none"} (Group: {CurrentDeviceGroup})");
            }
        }

        #endregion

        #region Event Handlers - Priority 1 (Action Change)

        private void OnActionChange(object obj, InputActionChange change)
        {
            // Track device when any action is performed
            if (change == InputActionChange.ActionStarted)
            {
                var action = obj as InputAction;
                if (action?.activeControl?.device != null)
                {
                    var device = action.activeControl.device;
                    TrySetCurrentDevice(device);
                }
            }
        }

        #endregion

        #region Event Handlers - Priority 2 (Raw Input Events)

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            // Only process state events (actual input, not config changes)
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return;

            // Only process button presses, not analog drift
            if (!HasSignificantInput(eventPtr))
                return;

            TrySetCurrentDevice(device);
        }

        /// <summary>
        /// Checks if the event contains significant input (button press, not analog drift).
        /// </summary>
        private bool HasSignificantInput(InputEventPtr eventPtr)
        {
            foreach (var control in eventPtr.EnumerateChangedControls())
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Event Handlers - Priority 3 (Device Lifecycle)

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                    OnDeviceAdded(device);
                    break;

                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                    OnDeviceRemoved(device);
                    break;
            }
        }

        private void OnDeviceAdded(InputDevice device)
        {
            if (CurrentDevice == null)
            {
                SetCurrentDevice(device, silent: false);

                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.Input,
                        $"Device added and set as current: {device.name}");
                }
            }
        }

        private void OnDeviceRemoved(InputDevice device)
        {
            if (device == CurrentDevice)
            {
                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.Input,
                        $"Current device removed: {device.name}. Finding fallback...");
                }

                FallbackToAvailableDevice();
            }
        }

        #endregion

        #region Device Management

        /// <summary>
        /// Attempts to set the current device, with debouncing and device group filtering.
        /// </summary>
        private void TrySetCurrentDevice(InputDevice device)
        {
            if (device == null || !device.enabled)
                return;

            // Debounce to prevent spam
            if (Time.unscaledTime - _lastDeviceChangeTime < debounceTime)
                return;

            // Check if device group actually changed (keyboard/mouse are same group)
            var newGroup = GetDeviceGroup(device);
            if (groupKeyboardMouse && newGroup == CurrentDeviceGroup)
                return;

            // Device changed - update
            if (device != CurrentDevice)
            {
                SetCurrentDevice(device, silent: false);
            }
        }

        /// <summary>
        /// Sets the current device and fires change event.
        /// </summary>
        private void SetCurrentDevice(InputDevice device, bool silent)
        {
            if (device == null || !device.enabled)
            {
                if (enableDebugLogging && device != null)
                {
                    Logger.LogWarning(LogSystems.Input,
                        $"Attempted to set disabled device: {device.name}");
                }
                return;
            }

            var previousDevice = CurrentDevice;
            CurrentDevice = device;
            CurrentDeviceGroup = GetDeviceGroup(device);
            _lastDeviceChangeTime = Time.unscaledTime;

            LogDeviceChange(previousDevice, device);

            if (!silent)
            {
                DeviceChanged?.Invoke(previousDevice, device);
            }
        }

        private void FallbackToAvailableDevice()
        {
            // Priority: Gamepad > Keyboard > Others
            var fallback = Gamepad.current
                        ?? (InputDevice)Keyboard.current
                        ?? InputSystem.devices.FirstOrDefault(d => d.enabled && d != CurrentDevice);

            if (fallback != null)
            {
                SetCurrentDevice(fallback, silent: false);

                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.Input,
                        $"Fallback device selected: {fallback.name}");
                }
            }
            else
            {
                CurrentDevice = null;
                CurrentDeviceGroup = null;

                if (enableDebugLogging)
                {
                    Logger.LogWarning(LogSystems.Input,
                        "No fallback device available. CurrentDevice set to null.");
                }
            }
        }

        /// <summary>
        /// Gets the device group name for grouping (KeyboardMouse vs Gamepad).
        /// </summary>
        private string GetDeviceGroup(InputDevice device)
        {
            if (device is null)
                return "KeyboardMouse";
            if (device is Keyboard || device is Mouse)
                return "KeyboardMouse";
            if (device is Gamepad)
                return "Gamepad";
            return device.layout;
        }

        private void LogDeviceChange(InputDevice previous, InputDevice current)
        {
            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.Input,
                    $"Device switched: {previous?.name ?? "none"} ({GetDeviceGroup(previous)}) → {current.name} ({GetDeviceGroup(current)})");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually set the current device (useful for testing or manual control).
        /// </summary>
        public void ForceSetDevice(InputDevice device)
        {
            SetCurrentDevice(device, silent: false);
        }

        /// <summary>
        /// Checks if the given device is in the same group as the current device.
        /// </summary>
        public bool IsSameDeviceGroup(InputDevice device)
        {
            return GetDeviceGroup(device) == CurrentDeviceGroup;
        }

        #endregion
    }
}