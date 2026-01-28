# HelloDev Input Documentation

*Last Updated: 2026-01-27*

---

## Overview

HelloDev Input provides a complete solution for displaying input prompts and managing rebinding in Unity games. It automatically shows the correct icons (keyboard, Xbox, PlayStation, etc.) based on the player's current input device.

**Key Features:**
- Automatic device-specific icon display
- Per-binding layout detection (Unity's recommended pattern)
- Full rebinding support with PlayerPrefs persistence
- Designer-friendly ScriptableObject configuration
- Binding ID selection that survives reordering

---

## Quick Links

| Document | Description |
|----------|-------------|
| [README](../README.md) | Quick start, API reference, code examples |
| [Input Prompts Guide](input-prompts-guide.md) | Step-by-step setup for designers and developers |

---

## Getting Started

### For Designers

1. Read the [Input Prompts Guide](input-prompts-guide.md)
2. Create icon maps for each device type
3. Create an icon provider
4. Add components to UI elements

### For Programmers

1. Read the [README](../README.md) for API reference
2. Reference InputActionReference instead of InputAction
3. Use InputPromptDisplay for automatic updates
4. Subscribe to OnUpdateBindingUI for custom handling

---

## Components Overview

| Component | Purpose |
|-----------|---------|
| `InputIconMap_SO` | Maps control paths to sprites for one device |
| `InputIconProvider_SO` | Selects correct icon map based on device |
| `InputPromptDisplay` | Displays binding with automatic updates |
| `InputActionButton` | Triggers events on input action |
| `InputButtonWithPrompt` | Combined button + prompt display |
| `InputRebindManager` | Manages rebinding and persistence |

---

## Architecture

The system follows Unity's Input System sample patterns:

```
InputActionAsset
    ↓
InputActionReference (rebind-compatible)
    ↓
GetBindingDisplayString(bindingId, out deviceLayoutName, out controlPath)
    ↓
InputIconProvider_SO.GetBinding(deviceLayoutName, controlPath)
    ↓
Display (icon or text)
```

**Key Design Decisions:**

1. **Per-binding layout detection** - Device is determined per-binding, not globally
2. **Binding ID (GUID)** - Robust identification that survives reordering
3. **ScriptableObject icons** - Designer-friendly configuration
4. **Static event subscription** - Optimized for multiple instances

---

## Version History

### v2.0.0 (2026-01-27)
- Removed global device tracking
- Added per-binding layout detection
- Added InputIconProvider_SO
- Added InputButtonWithPrompt
- Added custom editor dropdowns
- Added HelloDev Logger integration

### v1.0.0
- Initial release

---

## Related Packages

| Package | Dependency |
|---------|------------|
| `com.hellodev.utils` | Required (Logger) |
| Unity Input System | Required |
| TextMeshPro | Required |
| Odin Inspector | Optional |
