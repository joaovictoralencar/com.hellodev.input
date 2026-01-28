using System;
using System.Collections.Generic;
using UnityEngine;

namespace HelloDev.Input
{
    /// <summary>
    /// ScriptableObject that maps input control paths to sprites and fallback text.
    /// Create one for each device type (Xbox, PlayStation, Keyboard).
    /// </summary>
    [CreateAssetMenu(fileName = "InputIconMap", menuName = "HelloDev/Input/Input Icon Map")]
    public class InputIconMap_SO : ScriptableObject
    {
        [Serializable]
        public struct IconMapping
        {
            [Tooltip("The control path from the Input System (e.g., 'buttonSouth', 'k', 'leftTrigger')")]
            public string controlPath;

            [Tooltip("The sprite to display for this control (optional - uses fallback text if null)")]
            public Sprite icon;

            [Tooltip("Fallback text to display when no icon is available (e.g., 'A', 'K', 'LT')")]
            public string fallbackText;
        }

        [Tooltip("The device layout this map applies to (e.g., 'Gamepad', 'DualShockGamepad', 'Keyboard')")]
        [SerializeField] private string deviceLayoutName = "Gamepad";

        [Tooltip("List of control path to icon/text mappings")]
        [SerializeField] private List<IconMapping> mappings = new();

        /// <summary>
        /// The device layout name this icon map is for.
        /// </summary>
        public string DeviceLayoutName => deviceLayoutName;

        /// <summary>
        /// All mappings in this icon map.
        /// </summary>
        public IReadOnlyList<IconMapping> Mappings => mappings;

        /// <summary>
        /// Gets the icon and fallback text for a given control path.
        /// </summary>
        /// <param name="controlPath">The control path (e.g., "buttonSouth", "k")</param>
        /// <returns>Tuple containing the sprite (may be null) and fallback text</returns>
        public (Sprite icon, string text) GetBinding(string controlPath)
        {
            if (string.IsNullOrEmpty(controlPath))
                return (null, string.Empty);

            // Normalize the control path (remove device prefix if present)
            var normalizedPath = NormalizeControlPath(controlPath);

            foreach (var mapping in mappings)
            {
                if (string.Equals(mapping.controlPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return (mapping.icon, mapping.fallbackText);
                }
            }

            // No mapping found - return the control path as text
            return (null, normalizedPath);
        }

        /// <summary>
        /// Checks if this icon map has a mapping for the given control path.
        /// </summary>
        public bool HasMapping(string controlPath)
        {
            if (string.IsNullOrEmpty(controlPath))
                return false;

            var normalizedPath = NormalizeControlPath(controlPath);

            foreach (var mapping in mappings)
            {
                if (string.Equals(mapping.controlPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Normalizes a control path by removing device prefixes.
        /// </summary>
        private static string NormalizeControlPath(string controlPath)
        {
            if (string.IsNullOrEmpty(controlPath))
                return controlPath;

            // Remove common device prefixes like "<Keyboard>/", "<Gamepad>/", etc.
            var lastSlash = controlPath.LastIndexOf('/');
            if (lastSlash >= 0 && lastSlash < controlPath.Length - 1)
            {
                return controlPath.Substring(lastSlash + 1);
            }

            return controlPath;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor utility to add common gamepad mappings.
        /// </summary>
        [ContextMenu("Add Common Gamepad Mappings")]
        private void AddCommonGamepadMappings()
        {
            var commonMappings = new[]
            {
                ("buttonSouth", "A"),
                ("buttonNorth", "Y"),
                ("buttonEast", "B"),
                ("buttonWest", "X"),
                ("start", "Start"),
                ("select", "Select"),
                ("leftTrigger", "LT"),
                ("rightTrigger", "RT"),
                ("leftShoulder", "LB"),
                ("rightShoulder", "RB"),
                ("leftStick", "LS"),
                ("rightStick", "RS"),
                ("leftStickPress", "L3"),
                ("rightStickPress", "R3"),
                ("dpad/up", "Up"),
                ("dpad/down", "Down"),
                ("dpad/left", "Left"),
                ("dpad/right", "Right")
            };

            foreach (var (path, text) in commonMappings)
            {
                if (!HasMapping(path))
                {
                    mappings.Add(new IconMapping
                    {
                        controlPath = path,
                        icon = null,
                        fallbackText = text
                    });
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Editor utility to add common keyboard mappings.
        /// </summary>
        [ContextMenu("Add Common Keyboard Mappings")]
        private void AddCommonKeyboardMappings()
        {
            var commonMappings = new[]
            {
                ("space", "Space"),
                ("enter", "Enter"),
                ("escape", "Esc"),
                ("backspace", "Backspace"),
                ("tab", "Tab"),
                ("leftShift", "Shift"),
                ("leftCtrl", "Ctrl"),
                ("leftAlt", "Alt")
            };

            // Add letter keys
            for (char c = 'a'; c <= 'z'; c++)
            {
                var path = c.ToString();
                if (!HasMapping(path))
                {
                    mappings.Add(new IconMapping
                    {
                        controlPath = path,
                        icon = null,
                        fallbackText = char.ToUpper(c).ToString()
                    });
                }
            }

            // Add common special keys
            foreach (var (path, text) in commonMappings)
            {
                if (!HasMapping(path))
                {
                    mappings.Add(new IconMapping
                    {
                        controlPath = path,
                        icon = null,
                        fallbackText = text
                    });
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
