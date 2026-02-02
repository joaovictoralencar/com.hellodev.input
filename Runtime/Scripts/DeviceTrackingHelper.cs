using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HelloDev.Input
{
    /// <summary>
    /// Helper for managing device tracking subscriptions in a consistent way.
    /// Handles race conditions via OnInstanceReady pattern.
    /// </summary>
    public class DeviceTrackingHelper
    {
        private bool _isSubscribed;
        private Action<InputDevice, InputDevice> _onDeviceChanged;
        private readonly MonoBehaviour _owner;

        /// <summary>
        /// Creates a new device tracking helper.
        /// </summary>
        /// <param name="owner">The MonoBehaviour that owns this helper (for logging/debugging).</param>
        /// <param name="onDeviceChanged">Callback when device changes.</param>
        public DeviceTrackingHelper(MonoBehaviour owner, Action<InputDevice, InputDevice> onDeviceChanged)
        {
            _owner = owner;
            _onDeviceChanged = onDeviceChanged;
        }

        /// <summary>
        /// Subscribes to device change events. Safe to call in OnEnable.
        /// Handles race conditions automatically.
        /// </summary>
        public void Subscribe()
        {
            if (_isSubscribed)
                return;

            var tracker = LastUsedDeviceTracker.Instance;
            if (tracker != null)
            {
                // Tracker already initialized - subscribe immediately
                tracker.DeviceChanged += _onDeviceChanged;
                _isSubscribed = true;
            }
            else
            {
                // Tracker not ready yet - wait for initialization
                LastUsedDeviceTracker.OnInstanceReady += OnTrackerReady;
            }
        }

        /// <summary>
        /// Unsubscribes from device change events. Safe to call in OnDisable.
        /// </summary>
        public void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                // Clean up ready listener in case we never subscribed
                LastUsedDeviceTracker.OnInstanceReady -= OnTrackerReady;
                return;
            }

            var tracker = LastUsedDeviceTracker.Instance;
            if (tracker != null)
            {
                tracker.DeviceChanged -= _onDeviceChanged;
            }

            _isSubscribed = false;
        }

        /// <summary>
        /// Called when tracker becomes ready (handles race condition).
        /// </summary>
        private void OnTrackerReady(LastUsedDeviceTracker tracker)
        {
            LastUsedDeviceTracker.OnInstanceReady -= OnTrackerReady;
            
            if (_owner != null && _owner.isActiveAndEnabled)
            {
                tracker.DeviceChanged += _onDeviceChanged;
                _isSubscribed = true;
            }
        }

        /// <summary>
        /// Gets the current device layout, or null if no tracker.
        /// </summary>
        public string GetCurrentDeviceLayout()
        {
            return LastUsedDeviceTracker.Instance?.CurrentDevice?.layout;
        }

        /// <summary>
        /// Gets the device layout appropriate for icon mapping.
        /// Redirects Mouse to Keyboard automatically.
        /// </summary>
        public string GetDeviceLayoutForIcons()
        {
            return LastUsedDeviceTracker.Instance?.CurrentDeviceLayoutForIcons;
        }

        /// <summary>
        /// Gets the current device, or null if no tracker.
        /// </summary>
        public InputDevice GetCurrentDevice()
        {
            return LastUsedDeviceTracker.Instance?.CurrentDevice;
        }
    }
}