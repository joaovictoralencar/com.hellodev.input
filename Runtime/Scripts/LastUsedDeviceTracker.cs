using System;
using System.Linq;
using HelloDev.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.Input
{
    /// <summary>
    /// Singleton service that detects when the player switches input devices.
    /// Fires events to trigger UI updates - does not resolve bindings itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component tracks device changes through two mechanisms:
    /// </para>
    /// <list type="bullet">
    /// </list>
    /// <para>
    /// The tracker fires a <see cref="DeviceChanged"/> event when the active device switches.
    /// UI components like <see cref="InputPromptDisplay"/> can subscribe to this event and
    /// refresh their displays. Unity's Input System automatically resolves which binding
    /// to show based on the current device.
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add this component to a persistent GameObject in your scene.
    /// It will persist across scene loads via DontDestroyOnLoad.
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

        #endregion

        #region Configuration

        [Header("Options")]
        [Tooltip("Minimum time between device switches to prevent spam")]
        [SerializeField] private float debounceTime = 0.1f;

        [Tooltip("Track last-used device via input events")]
        [SerializeField] private bool trackLastUsedDevice = true;

        [Tooltip("Monitor device plug/unplug")]
        [SerializeField] private bool monitorDeviceLifecycle = true;

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

            // Start with first available device
            InitializeCurrentDevice();
        }

        private void OnEnable()
        {
            if (trackLastUsedDevice)
            {
                InputSystem.onEvent += OnInputEvent;
            }

            if (monitorDeviceLifecycle)
            {
                InputSystem.onDeviceChange += OnDeviceChange;
            }

            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.Input, 
                    $"LastUsedDeviceTracker enabled. Options: trackLastUsed={trackLastUsedDevice}, monitorLifecycle={monitorDeviceLifecycle}");
            }
        }

        private void OnDisable()
        {
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
                    $"Initialized with device: {CurrentDevice?.name ?? "none"}");
            }
        }

        #endregion

        #region Event Handlers

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            // Only process state events (actual input, not config changes)
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return;
            
            // Only process button presses, not analog drift
            // This prevents mouse movement or stick drift from triggering device switches
            if (!HasSignificantInput(eventPtr))
                return;
            
            // Debounce to prevent spam during rapid device switching
            if (Time.unscaledTime - _lastDeviceChangeTime < debounceTime)
                return;

            // Check if device actually changed
            if (device != CurrentDevice)
            {
                if (device.name.Contains("Mouse") && CurrentDevice.name.Contains("Keyboard"))
                    return;
                SetCurrentDevice(device, silent: false);
            }
        }

        /// <summary>
        /// Checks if the event contains significant input (button press, not analog drift).
        /// This prevents mouse movement or stick drift from triggering device switches.
        /// </summary>
        private bool HasSignificantInput(InputEventPtr eventPtr)
        {
            // Check if ANY button was pressed (not just moved)
            foreach (var control in eventPtr.EnumerateChangedControls())
            {
                if (control is ButtonControl button && button.isPressed)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Event Handlers (Device Lifecycle)

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
            // If we don't have a current device, set this as current
            if (CurrentDevice == null)
            {
                SetCurrentDevice(device, silent: false);

                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.Input, 
                        $"Device added and set as current: {device.name}");
                }
            }
            else if (enableDebugLogging)
            {
                Logger.Log(LogSystems.Input, 
                    $"Device added: {device.name} (current device unchanged)");
            }
        }

        private void OnDeviceRemoved(InputDevice device)
        {
            // If current device was removed, fallback to another
            if (device == CurrentDevice)
            {
                if (enableDebugLogging)
                {
                    Logger.Log(LogSystems.Input, 
                        $"Current device removed: {device.name}. Finding fallback...");
                }

                FallbackToAvailableDevice();
            }
            else if (enableDebugLogging)
            {
                Logger.Log(LogSystems.Input, 
                    $"Device removed: {device.name} (not current device)");
            }
        }

        #endregion

        #region Device Management

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
            _lastDeviceChangeTime = Time.unscaledTime;

            LogDeviceChange(previousDevice, device);

            if (!silent)
            {
                DeviceChanged?.Invoke(previousDevice, device);
            }
        }

        private void FallbackToAvailableDevice()
        {
            // Try to find any available device
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
                // No devices available
                CurrentDevice = null;

                if (enableDebugLogging)
                {
                    Logger.LogWarning(LogSystems.Input, 
                        "No fallback device available. CurrentDevice set to null.");
                }
            }
        }

        private void LogDeviceChange(InputDevice previous, InputDevice current)
        {
            if (enableDebugLogging)
            {
                Logger.Log(LogSystems.Input,
                    $"Device switched: {previous?.name ?? "none"} → {current.name} ({current.layout})");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually set the current device (useful for testing or manual control).
        /// </summary>
        /// <param name="device">The device to set as current</param>
        public void ForceSetDevice(InputDevice device)
        {
            SetCurrentDevice(device, silent: false);
        }

        #endregion
    }
}