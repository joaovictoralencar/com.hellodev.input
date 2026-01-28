# HelloDev Input

Input display, device tracking, and rebinding utilities for Unity Input System. Display the correct input icons automatically based on the player's current device with full rebinding support.

## Features

- **InputIconMap_SO** - Map control paths to sprites/text for each device type (Xbox, PlayStation, Keyboard)
- **InputIconProvider_SO** - Unified provider that selects the correct icon map based on device layout
- **InputPromptDisplay** - Automatically shows the correct input icon/text with binding change detection
- **InputActionButton** - Triggers UnityEvents when input actions are performed
- **InputButtonWithPrompt** - Combined button + prompt display in one component
- **InputRebindManager** - Full rebinding support with PlayerPrefs persistence
- **Per-Binding Layout Detection** - Uses Unity's recommended pattern (`GetBindingDisplayString()`)
- **Binding ID Selection** - Editor dropdowns for easy binding selection (survives reordering)
- **Custom Event System** - `UpdateBindingUIEvent` for advanced custom icon handling

## Getting Started

### 1. Install the Package

**Via Package Manager (Local):**
1. Open Unity Package Manager (Window > Package Manager)
2. Click "+" > "Add package from disk"
3. Navigate to this folder and select `package.json`

**Dependencies:**
- Unity Input System 1.7.0+
- TextMeshPro 3.0.6+
- com.hellodev.utils (for logging)

### 2. Create Icon Maps

Create icon maps for each device type you want to support:

1. Right-click in Project > **Create > HelloDev > Input > Input Icon Map**
2. Name it descriptively (e.g., `IconMap_Xbox`, `IconMap_PlayStation`, `IconMap_Keyboard`)
3. Set the **Device Layout Name** (e.g., `Gamepad`, `DualShockGamepad`, `Keyboard`)
4. Add control path mappings with icons and fallback text

**Quick Setup:** Use the context menu options:
- Right-click on the asset > **Add Common Gamepad Mappings**
- Right-click on the asset > **Add Common Keyboard Mappings**

### 3. Create an Icon Provider

The provider selects the correct icon map based on the binding's device:

1. Right-click in Project > **Create > HelloDev > Input > Input Icon Provider**
2. Add your icon maps to the **Icon Maps** list
3. Set a **Fallback Icon Map** (typically keyboard)

### 4. Display Input Prompts

Add `InputPromptDisplay` to a UI element:

```csharp
using HelloDev.Input;
using UnityEngine;
using UnityEngine.InputSystem;

public class PromptSetup : MonoBehaviour
{
    [SerializeField] private InputPromptDisplay promptDisplay;
    [SerializeField] private InputActionReference jumpAction;

    void Start()
    {
        promptDisplay.ActionReference = jumpAction;
    }
}
```

Or configure entirely in the Inspector:
1. Add `InputPromptDisplay` component to a GameObject
2. Assign an **InputActionReference**
3. Select a **Binding** from the dropdown
4. Assign your **Icon Provider**
5. Assign **Text** and/or **Icon** UI components

### 5. Wire Up Input Buttons

For buttons that respond to input AND show prompts:

```csharp
using HelloDev.Input;
using UnityEngine;

public class ConfirmButton : MonoBehaviour
{
    [SerializeField] private InputButtonWithPrompt confirmButton;

    void OnEnable()
    {
        confirmButton.OnActionPerformed.AddListener(OnConfirm);
    }

    void OnDisable()
    {
        confirmButton.OnActionPerformed.RemoveListener(OnConfirm);
    }

    void OnConfirm()
    {
        Debug.Log("Confirmed!");
    }
}
```

## Installation

### Via Package Manager (Local)
1. Open Unity Package Manager
2. Click "+" > "Add package from disk"
3. Navigate to this folder and select `package.json`

## Components

### InputIconMap_SO

Maps control paths to sprites and fallback text for a specific device.

| Field | Description |
|-------|-------------|
| `DeviceLayoutName` | Device layout this map applies to (e.g., "Gamepad", "Keyboard") |
| `Mappings` | List of control path to icon/text mappings |

```csharp
// Get binding for a control path
var (icon, text) = iconMap.GetBinding("buttonSouth");

// Check if mapping exists
bool hasMapping = iconMap.HasMapping("buttonSouth");
```

### InputIconProvider_SO

Provides the correct icon map based on device layout with inheritance matching.

| Field | Description |
|-------|-------------|
| `IconMaps` | List of icon maps for different devices |
| `FallbackIconMap` | Default map when no match found |

```csharp
// Get icon map for a device layout
var iconMap = iconProvider.GetIconMapForLayout("DualSenseGamepadHID");

// Get binding directly (convenience method)
var (icon, text) = iconProvider.GetBinding("DualSenseGamepadHID", "buttonSouth");
```

**Device Matching:** Uses Unity's `InputSystem.IsFirstLayoutBasedOnSecond()` for layout inheritance. A "DualSenseGamepadHID" will match a "DualShockGamepad" icon map.

### InputPromptDisplay

Displays the current binding for an action with automatic updates when bindings change.

| Field | Description |
|-------|-------------|
| `ActionReference` | The InputActionReference to display |
| `BindingId` | GUID of the specific binding (dropdown in editor) |
| `IconProvider` | Provider for device-specific icons |
| `BindingText` | TMP_Text for text display |
| `BindingIcon` | Image for icon display |
| `PreferIcon` | Show icon over text when available |
| `ExclusiveDisplay` | Hide text when showing icon and vice versa |

```csharp
// Configure at runtime
promptDisplay.ActionReference = myAction;
promptDisplay.SetIconProvider(myProvider);
promptDisplay.ConfigureDisplayOptions(
    format: "[{0}]",
    preferIconOverText: true,
    exclusive: true
);

// Force refresh
promptDisplay.UpdateBindingDisplay();

// Subscribe to updates for custom handling
promptDisplay.OnUpdateBindingUI.AddListener((component, displayString, deviceLayout, controlPath) =>
{
    Debug.Log($"Binding updated: {displayString} on {deviceLayout}");
});
```

### InputActionButton

Triggers a UnityEvent when an input action is performed.

| Field | Description |
|-------|-------------|
| `ActionReference` | The InputActionReference that triggers this button |
| `OnActionPerformed` | Event invoked when action is performed |
| `RespectCanvasGroup` | If true, checks CanvasGroup.interactable |

```csharp
// Get binding for a specific control scheme
int index = button.GetBindingIndexForScheme("Gamepad");

// Start rebinding
button.StartRebind();

// Reset to default
button.ResetBinding();

// Manually trigger the event
button.TriggerAction();
```

### InputButtonWithPrompt

Combines InputActionButton and InputPromptDisplay in one component.

| Field | Description |
|-------|-------------|
| `ActionReference` | The action for both input and display |
| `BindingId` | Specific binding to display |
| `IconProvider` | Provider for device-specific icons |
| `OnActionPerformed` | Event invoked when action is performed |

```csharp
// Access properties
bool interactable = button.IsInteractable;
button.ActionReference = newAction;
button.BindingId = newBindingId;

// Rebinding
button.StartRebind();
button.ResetBinding();

// Change icon provider at runtime
button.SetIconProvider(newProvider);
```

### InputRebindManager

Singleton manager for interactive rebinding with PlayerPrefs persistence.

| Field | Description |
|-------|-------------|
| `InputActions` | The InputActionAsset to manage |
| `PlayerPrefsKey` | Key for storing binding overrides |
| `LoadOnEnable` | Auto-load bindings when enabled |
| `SaveOnDisable` | Auto-save bindings when disabled |
| `CancelBindingPaths` | Keys that cancel rebinding (default: Escape) |
| `RebindTimeout` | Timeout in seconds (0 = no timeout) |

**Events:**
- `OnRebindStarted` - Fired when rebinding begins
- `OnRebindCompleted` - Fired when rebinding succeeds
- `OnRebindCanceled` - Fired when rebinding is canceled
- `OnBindingsLoaded` - Fired after loading from PlayerPrefs
- `OnBindingsSaved` - Fired after saving to PlayerPrefs

```csharp
// Access singleton
var manager = InputRebindManager.Instance;

// Save/Load bindings
manager.SaveBindings();
manager.LoadBindings();
manager.ClearSavedBindings();

// Interactive rebinding
manager.StartRebind(actionReference, bindingIndex);
manager.CancelRebind();
bool isRebinding = manager.IsRebinding;

// Reset bindings
manager.ResetBinding(actionReference);      // Reset specific action
manager.ResetAllBindings();                  // Reset all actions

// Query binding state
string display = manager.GetBindingDisplayString(actionReference);
bool hasOverride = manager.HasBindingOverride(actionReference);

// Runtime actions (temporary actions not in an asset)
var action = manager.CreateRuntimeAction("Skip", "<Keyboard>/k", "<Gamepad>/buttonSouth");
action.performed += ctx => Debug.Log("Skip pressed!");
manager.DisposeRuntimeAction("Skip");
manager.DisposeAllRuntimeActions();
```

## Usage Examples

### Basic Prompt Display

```csharp
using HelloDev.Input;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractPrompt : MonoBehaviour
{
    [SerializeField] private InputPromptDisplay promptDisplay;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputIconProvider_SO iconProvider;

    void Start()
    {
        promptDisplay.ActionReference = interactAction;
        promptDisplay.SetIconProvider(iconProvider);
    }
}
```

### Rebinding UI

```csharp
using HelloDev.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebindButton : MonoBehaviour
{
    [SerializeField] private InputButtonWithPrompt button;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button rebindUIButton;

    void OnEnable()
    {
        rebindUIButton.onClick.AddListener(StartRebind);
        InputRebindManager.Instance.OnRebindStarted.AddListener(OnRebindStarted);
        InputRebindManager.Instance.OnRebindCompleted.AddListener(OnRebindCompleted);
        InputRebindManager.Instance.OnRebindCanceled.AddListener(OnRebindCanceled);
    }

    void OnDisable()
    {
        rebindUIButton.onClick.RemoveListener(StartRebind);
        if (InputRebindManager.Instance != null)
        {
            InputRebindManager.Instance.OnRebindStarted.RemoveListener(OnRebindStarted);
            InputRebindManager.Instance.OnRebindCompleted.RemoveListener(OnRebindCompleted);
            InputRebindManager.Instance.OnRebindCanceled.RemoveListener(OnRebindCanceled);
        }
    }

    void StartRebind()
    {
        button.StartRebind();
    }

    void OnRebindStarted()
    {
        statusText.text = "Press a key...";
    }

    void OnRebindCompleted()
    {
        statusText.text = "Binding saved!";
    }

    void OnRebindCanceled()
    {
        statusText.text = "Rebind canceled";
    }
}
```

### Custom Icon Handling

```csharp
using HelloDev.Input;
using UnityEngine;

public class CustomIconHandler : MonoBehaviour
{
    [SerializeField] private InputPromptDisplay promptDisplay;
    [SerializeField] private Animator iconAnimator;

    void OnEnable()
    {
        promptDisplay.OnUpdateBindingUI.AddListener(HandleBindingUpdate);
    }

    void OnDisable()
    {
        promptDisplay.OnUpdateBindingUI.RemoveListener(HandleBindingUpdate);
    }

    void HandleBindingUpdate(InputPromptDisplay component, string displayString,
                              string deviceLayoutName, string controlPath)
    {
        // Custom logic based on device
        if (deviceLayoutName != null && deviceLayoutName.Contains("Gamepad"))
        {
            iconAnimator.SetTrigger("PulseGamepad");
        }
        else
        {
            iconAnimator.SetTrigger("PulseKeyboard");
        }
    }
}
```

## API Reference

### InputIconMap_SO

| Member | Description |
|--------|-------------|
| `DeviceLayoutName` | Device layout this map applies to |
| `Mappings` | Read-only list of all mappings |
| `GetBinding(controlPath)` | Returns (icon, text) tuple for a control path |
| `HasMapping(controlPath)` | Checks if mapping exists |

### InputIconProvider_SO

| Member | Description |
|--------|-------------|
| `IconMaps` | Read-only list of icon maps |
| `FallbackIconMap` | Default icon map |
| `GetIconMapForLayout(layoutName)` | Gets best matching icon map |
| `GetBinding(layoutName, controlPath)` | Gets (icon, text) directly |

### InputPromptDisplay

| Member | Description |
|--------|-------------|
| `ActionReference` | The action being displayed |
| `BindingId` | GUID of the selected binding |
| `BindingText` | Text component for display |
| `BindingIcon` | Image component for display |
| `PreferIcon` | Prefer icon over text |
| `ExclusiveDisplay` | Hide one when showing other |
| `OnUpdateBindingUI` | Event fired on display updates |
| `UpdateBindingDisplay()` | Force refresh the display |
| `SetIconProvider(provider)` | Change provider at runtime |
| `ConfigureDisplayOptions(...)` | Configure display settings |
| `GetBindingIndex()` | Get current binding index |

### InputActionButton

| Member | Description |
|--------|-------------|
| `ActionReference` | The action that triggers this button |
| `OnActionPerformed` | Event when action triggers |
| `IsInteractable` | Whether button responds to input |
| `TriggerAction()` | Manually trigger the event |
| `GetBindingIndexForScheme(scheme)` | Get binding for control scheme |
| `StartRebind(bindingIndex)` | Start interactive rebind |
| `ResetBinding(bindingIndex)` | Reset to default binding |

### InputButtonWithPrompt

| Member | Description |
|--------|-------------|
| `ActionReference` | Action for input and display |
| `BindingId` | Specific binding to display |
| `OnActionPerformed` | Event when action triggers |
| `IsInteractable` | Whether button responds to input |
| `TriggerAction()` | Manually trigger the event |
| `SetIconProvider(provider)` | Change provider at runtime |
| `StartRebind(bindingIndex)` | Start interactive rebind |
| `ResetBinding()` | Reset to default binding |
| `UpdateBindingDisplay()` | Force refresh display |

### InputRebindManager

| Member | Description |
|--------|-------------|
| `Instance` | Singleton instance |
| `InputActions` | The managed InputActionAsset |
| `IsRebinding` | Whether rebind is in progress |
| `CurrentRebindAction` | Action being rebound (null if not rebinding) |
| `SaveBindings()` | Save to PlayerPrefs |
| `LoadBindings()` | Load from PlayerPrefs |
| `ClearSavedBindings()` | Delete saved bindings |
| `StartRebind(action, index)` | Begin interactive rebind |
| `CancelRebind()` | Cancel current rebind |
| `ResetBinding(action, index)` | Reset specific binding |
| `ResetAllBindings()` | Reset all bindings |
| `GetBindingDisplayString(action, index)` | Get display string |
| `HasBindingOverride(action)` | Check for overrides |
| `CreateRuntimeAction(...)` | Create temporary action |
| `GetRuntimeAction(name)` | Get existing runtime action |
| `DisposeRuntimeAction(name)` | Dispose runtime action |
| `DisposeAllRuntimeActions()` | Dispose all runtime actions |

## Dependencies

**Required:**
- Unity Input System 1.7.0+
- TextMeshPro 3.0.6+
- com.hellodev.utils 1.1.0+ (for Logger)

**Optional:**
- Odin Inspector (enhanced editor experience)

## Changelog

### v2.0.0 (2026-01-27)

**Architecture:**
- Removed global device tracking in favor of per-binding layout detection
- Follows Unity's Input System sample pattern (`GetBindingDisplayString()` with out parameters)
- Uses `bindingId` (GUID) for robust binding identification

**New Features:**
- `InputIconProvider_SO` - Unified provider with device layout inheritance matching
- `InputButtonWithPrompt` - Combined input button + prompt display component
- Custom editor dropdowns for binding selection
- `UpdateBindingUIEvent` for custom icon handling
- HelloDev Logger integration (`LogSystems.Input`, `LogSystems.InputRebind`)

**Improvements:**
- Static list optimization for `InputSystem.onActionChange` subscriptions
- Layout inheritance matching via `InputSystem.IsFirstLayoutBasedOnSecond()`
- Automatic binding display updates when bindings change

**Removed:**
- `InputDeviceTracker` - No longer needed with per-binding approach

### v1.0.0

- Initial release

## License

MIT License
