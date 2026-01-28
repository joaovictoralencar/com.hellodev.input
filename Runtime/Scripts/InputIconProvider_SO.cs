using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HelloDev.Input
{
    /// <summary>
    /// ScriptableObject that provides icon maps for different device layouts.
    /// Reference this in UI components that need to display input prompts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This follows Unity's Input System sample pattern where the device layout
    /// is determined per-binding via <c>GetBindingDisplayString()</c>, not from
    /// a global "current device" tracker.
    /// </para>
    /// <para>
    /// Each <see cref="InputIconMap_SO"/> in the list defines its own <c>DeviceLayoutName</c>.
    /// The provider finds the best match using Unity's layout inheritance system.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get icon for a specific binding
    /// var displayString = action.GetBindingDisplayString(bindingIndex,
    ///     out var deviceLayoutName, out var controlPath);
    ///
    /// var iconMap = iconProvider.GetIconMapForLayout(deviceLayoutName);
    /// var (icon, text) = iconMap.GetBinding(controlPath);
    /// </code>
    /// </example>
    [CreateAssetMenu(fileName = "InputIconProvider", menuName = "HelloDev/Input/Input Icon Provider")]
    public class InputIconProvider_SO : ScriptableObject
    {
        [Header("Icon Maps")]
        [Tooltip("Icon maps for different devices. Each defines its own DeviceLayoutName. Order matters - first match wins.")]
        [SerializeField] private List<InputIconMap_SO> iconMaps = new();

        [Tooltip("Fallback icon map when no layout matches (typically keyboard)")]
        [SerializeField] private InputIconMap_SO fallbackIconMap;

        /// <summary>
        /// All registered icon maps.
        /// </summary>
        public IReadOnlyList<InputIconMap_SO> IconMaps => iconMaps;

        /// <summary>
        /// The fallback icon map used when no match is found.
        /// </summary>
        public InputIconMap_SO FallbackIconMap => fallbackIconMap;

        /// <summary>
        /// Gets the icon map that best matches the given device layout.
        /// Uses Unity's layout inheritance for matching (e.g., "DualSenseGamepadHID" matches "DualShockGamepad").
        /// </summary>
        /// <param name="deviceLayoutName">The device layout name from GetBindingDisplayString()</param>
        /// <returns>The matching icon map, or fallback if no match found.</returns>
        public InputIconMap_SO GetIconMapForLayout(string deviceLayoutName)
        {
            if (string.IsNullOrEmpty(deviceLayoutName))
                return fallbackIconMap;

#if ENABLE_INPUT_SYSTEM
            // First pass: exact match
            foreach (var iconMap in iconMaps)
            {
                if (iconMap == null) continue;

                if (string.Equals(iconMap.DeviceLayoutName, deviceLayoutName, StringComparison.OrdinalIgnoreCase))
                {
                    return iconMap;
                }
            }

            // Second pass: layout inheritance (e.g., DualSenseGamepadHID inherits from DualShockGamepad)
            foreach (var iconMap in iconMaps)
            {
                if (iconMap == null) continue;

                if (InputSystem.IsFirstLayoutBasedOnSecond(deviceLayoutName, iconMap.DeviceLayoutName))
                {
                    return iconMap;
                }
            }
#else
            // Without Input System, just do string contains matching
            foreach (var iconMap in iconMaps)
            {
                if (iconMap == null) continue;

                if (deviceLayoutName.Contains(iconMap.DeviceLayoutName, StringComparison.OrdinalIgnoreCase) ||
                    iconMap.DeviceLayoutName.Contains(deviceLayoutName, StringComparison.OrdinalIgnoreCase))
                {
                    return iconMap;
                }
            }
#endif

            return fallbackIconMap;
        }

        /// <summary>
        /// Convenience method to get icon and text for a binding in one call.
        /// </summary>
        /// <param name="deviceLayoutName">The device layout name from GetBindingDisplayString()</param>
        /// <param name="controlPath">The control path from GetBindingDisplayString()</param>
        /// <returns>Tuple of (sprite, fallback text). Sprite may be null.</returns>
        public (Sprite icon, string text) GetBinding(string deviceLayoutName, string controlPath)
        {
            var iconMap = GetIconMapForLayout(deviceLayoutName);
            if (iconMap != null)
            {
                return iconMap.GetBinding(controlPath);
            }
            return (null, controlPath);
        }
    }
}
