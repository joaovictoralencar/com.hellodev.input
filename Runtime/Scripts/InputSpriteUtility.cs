using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HelloDev.Input
{
    /// <summary>
    /// Utility for processing input sprite tags in TextMeshPro text.
    /// Replaces action names with device-specific sprite names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Simple Action Pattern:</b> &lt;sprite name=actionName&gt;
    /// </para>
    /// <para>
    /// Example: &lt;sprite name=Sprint&gt; becomes:
    /// - Keyboard: &lt;sprite name=LeftShift&gt; (if Sprint is bound to Left Shift)
    /// - Gamepad: &lt;sprite name=RightStickPress&gt; (if Sprint is bound to Right Stick Press)
    /// </para>
    /// <para>
    /// <b>Composite Binding Pattern:</b> &lt;sprite name=actionName/compositeName/partName&gt;
    /// </para>
    /// <para>
    /// Example: &lt;sprite name=Move/WASD/Up&gt; becomes:
    /// - Keyboard: &lt;sprite name=W&gt; (if WASD composite's Up part is bound to W key)
    /// </para>
    /// <para>
    /// The utility searches through enabled InputActionMaps, finds the action by name,
    /// and resolves the device-specific binding based on the current device.
    /// </para>
    /// </remarks>
    public static class InputSpriteUtility
    {
        private static readonly Regex SpritePattern = new Regex(@"<sprite\s+name=([^>]+)>", RegexOptions.Compiled);

        /// <summary>
        /// Processes input sprite tags and replaces them with device-specific sprite names.
        /// Searches through enabled action maps to find actions by name.
        /// Supports composite binding syntax: actionName/compositeName/partName (e.g., Move/WASD/Up)
        /// </summary>
        /// <param name="text">The text containing sprite tags with action names.</param>
        /// <param name="iconProvider">Icon provider for resolving device-specific sprites.</param>
        /// <param name="deviceLayout">The device layout to use for resolution (e.g., "Keyboard", "Gamepad").</param>
        /// <returns>Text with device-specific sprite names.</returns>
        public static string ProcessInputSprites(string text, InputIconProvider_SO iconProvider, string deviceLayout)
        {
            if (string.IsNullOrEmpty(text) || iconProvider == null || string.IsNullOrEmpty(deviceLayout))
                return text;

            return SpritePattern.Replace(text, match =>
            {
                var spriteTag = match.Groups[1].Value.Trim();

                // Parse the sprite tag for composite pattern: Action/Composite/Part
                ParseSpriteTag(spriteTag, out string actionName, out string compositeName, out string partName);

                // Find the action by name in enabled action maps
                var action = FindActionInEnabledMaps(actionName);
                if (action == null)
                {
                    // Action not found - keep original tag
                    return match.Value;
                }

                // Get the control path for the current device
                string controlPath;
                if (!string.IsNullOrEmpty(compositeName) && !string.IsNullOrEmpty(partName) && !deviceLayout.Contains("Gamepad"))
                {
                    // Composite binding pattern (e.g., Move/WASD/Up)
                    controlPath = GetCompositePartControlPath(action, compositeName, partName, deviceLayout);
                }
                else
                {
                    string layout = action.bindingMask.ToString();
                    // Simple action (e.g., Sprint)
                    if (string.IsNullOrEmpty(layout) && LastUsedDeviceTracker.Instance.CurrentDevice is Mouse or Keyboard)
                    {
                        layout = "Mouse";
                    }
                    controlPath = GetControlPathForDevice(action, layout);
                }

                if (string.IsNullOrEmpty(controlPath))
                {
                    // No binding found for this device - keep original tag
                    return deviceLayout.Contains("Gamepad") ? "Gamepad" : "Keyboard";
                }

                // Get the device-specific icon from the icon provider
                var iconMap = iconProvider.GetIconMapForLayout(deviceLayout);
                if (iconMap != null)
                {
                    var (icon, mappedText) = iconMap.GetBinding(controlPath);

                    // If we have an icon sprite, use its name for the TMP sprite tag
                    if (icon != null)
                    {
                        return $"<sprite name=\"{icon.name}\">";
                    }
                }

                // Fallback: keep original sprite tag
                return match.Value;
            });
        }

        /// <summary>
        /// Processes input sprite tags using the current device from LastUsedDeviceTracker.
        /// Automatically redirects Mouse to Keyboard for icon resolution.
        /// </summary>
        /// <param name="text">The text containing sprite tags with action names.</param>
        /// <param name="iconProvider">Icon provider for resolving device-specific sprites.</param>
        /// <returns>Text with device-specific sprite names based on current device.</returns>
        public static string ProcessInputSprites(string text, InputIconProvider_SO iconProvider)
        {
            var tracker = LastUsedDeviceTracker.Instance;
            var deviceLayout = tracker?.CurrentDeviceLayoutForIcons;

            if (string.IsNullOrEmpty(deviceLayout))
                return text;

            return ProcessInputSprites(text, iconProvider, deviceLayout);
        }

        /// <summary>
        /// Finds an action by name in all currently enabled action maps.
        /// Searches through all enabled actions in the Input System.
        /// </summary>
        /// <param name="actionName">The name of the action to find (case-insensitive).</param>
        /// <returns>The InputAction if found, otherwise null.</returns>
        private static InputAction FindActionInEnabledMaps(string actionName)
        {
            // Get all currently enabled actions from the Input System
            var allActions = InputSystem.ListEnabledActions();

            foreach (var action in allActions)
            {
                if (string.Equals(action.name, actionName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return action;
                }
            }

            return null;
        }

        /// <summary>
        /// Parses a sprite tag into action name, composite name, and part name.
        /// Supports patterns: "Action", "Action/Composite/Part"
        /// </summary>
        /// <param name="spriteTag">The sprite tag (e.g., "Move/WASD/Up").</param>
        /// <param name="actionName">Output: The action name (e.g., "Move").</param>
        /// <param name="compositeName">Output: The composite name (e.g., "WASD") or null.</param>
        /// <param name="partName">Output: The part name (e.g., "Up") or null.</param>
        private static void ParseSpriteTag(string spriteTag, out string actionName, out string compositeName, out string partName)
        {
            var parts = spriteTag.Split('/');

            if (parts.Length == 3)
            {
                // Pattern: Action/Composite/Part (e.g., Move/WASD/Up)
                actionName = parts[0].Trim();
                compositeName = parts[1].Trim();
                partName = parts[2].Trim();
            }
            else
            {
                // Pattern: Action (e.g., Sprint)
                actionName = spriteTag;
                compositeName = null;
                partName = null;
            }
        }

        /// <summary>
        /// Gets the control path for a specific part of a composite binding.
        /// Matches composites by checking if their parts' binding paths contain the device layout.
        /// </summary>
        /// <param name="action">The action containing the composite binding.</param>
        /// <param name="compositeName">The name of the composite binding (e.g., "WASD").</param>
        /// <param name="partName">The name of the composite part (e.g., "Up").</param>
        /// <param name="deviceLayout">The device layout to match.</param>
        /// <returns>The control path for the composite part, or null if not found.</returns>
        private static string GetCompositePartControlPath(InputAction action, string compositeName, string partName, string deviceLayout)
        {
            int compositeIndex = -1;

            // First, find the composite binding by name
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                // Check if this is the composite we're looking for
                if (binding.isComposite && string.Equals(binding.name, compositeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    compositeIndex = i;
                    break;
                }
            }

            if (compositeIndex == -1)
            {
                // Composite not found
                return null;
            }

            // Now find the part within this composite that matches the device
            for (int i = compositeIndex + 1; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                // Stop if we hit another composite or non-composite-part binding
                if (!binding.isPartOfComposite)
                    break;

                // Check if this is the part we're looking for AND it matches the device layout
                if (string.Equals(binding.name, partName, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Check if the binding path contains the device layout
                    if (binding.path.Contains($"<{deviceLayout}>", System.StringComparison.OrdinalIgnoreCase))
                    {
                        return ExtractControlPath(binding.path);
                    }
                }
            }

            // Part not found or doesn't match device
            return null;
        }

        /// <summary>
        /// Gets the control path for a specific device from an action's bindings.
        /// Matches bindings by checking if the path contains the device layout.
        /// </summary>
        /// <param name="action">The action to get the binding from.</param>
        /// <param name="deviceLayout">The device layout to match (e.g., "Keyboard", "Gamepad").</param>
        /// <returns>The control path for the device, or null if not found.</returns>
        private static string GetControlPathForDevice(InputAction action, string deviceLayout)
        {
            // Search through bindings for one that contains the device layout in its path
            foreach (var binding in action.bindings)
            {
                // Skip composite parts (we want the composite itself or regular bindings)
                if (binding.isPartOfComposite)
                    continue;
                
                // Check if the binding path contains the device layout
                // Format: "<DeviceLayout>/control" (e.g., "<Keyboard>/leftShift", "<Gamepad>/buttonSouth")
                if (binding.groups.Contains(deviceLayout, System.StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractControlPath(binding.path);
                }
            }

            // Fallback: return first non-composite binding
            var firstBinding = action.bindings.FirstOrDefault(b => !b.isPartOfComposite && !string.IsNullOrEmpty(b.path));
            return firstBinding.path != null ? ExtractControlPath(firstBinding.path) : null;
        }

        /// <summary>
        /// Extracts the control path from a full binding path.
        /// Converts "<Keyboard>/leftShift" to "leftShift", "<Gamepad>/rightStickPress" to "rightStickPress", etc.
        /// </summary>
        /// <param name="bindingPath">The full binding path (e.g., "<Keyboard>/leftShift").</param>
        /// <returns>The control path without the device prefix (e.g., "leftShift").</returns>
        private static string ExtractControlPath(string bindingPath)
        {
            if (string.IsNullOrEmpty(bindingPath))
                return null;

            // Remove device prefix: "<Device>/control" -> "control"
            var slashIndex = bindingPath.IndexOf('/');
            if (slashIndex >= 0 && slashIndex < bindingPath.Length - 1)
            {
                return bindingPath.Substring(slashIndex + 1);
            }

            // No slash found - return as-is (might be a composite name)
            return bindingPath;
        }
    }
}