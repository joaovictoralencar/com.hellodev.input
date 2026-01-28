# Input Prompts Guide

*Last Updated: 2026-01-27*

---

## What You'll Build

By the end of this guide, you'll have:

- Input prompts that automatically show the correct icons for keyboard/gamepad
- Buttons that respond to input actions and display their bindings
- A rebinding system that lets players customize their controls
- Icons that update automatically when bindings change

**Final Result Preview:**

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│     Press [A] to interact      ← Gamepad connected         │
│                                                             │
│     Press [E] to interact      ← Keyboard/Mouse            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Prerequisites

Before starting, ensure you have:

### Required Packages

| Package | Purpose | How to Verify |
|---------|---------|---------------|
| **Input System** | Unity's new input system | Check Package Manager |
| **TextMeshPro** | UI text rendering | Window > TextMeshPro > Import TMP Essential Resources |
| **HelloDev Utils** | Logging system | Check `Assets/HelloDev/com.hellodev.utils` exists |

### Unity Version

- Unity 2022.3 or newer recommended

### Project Setup

- An `InputActionAsset` with your game's input actions
- A UI Canvas for displaying prompts

---

## Glossary

Understanding the naming conventions will help you navigate the system:

| Term | Meaning |
|------|---------|
| `_SO` suffix | **ScriptableObject** - A data asset that lives in your Project folder. |
| `InputActionReference` | A reference to an action in an InputActionAsset - supports rebinding. |
| `Binding` | A specific input (e.g., "Keyboard/E" or "Gamepad/buttonSouth"). One action can have multiple bindings. |
| `BindingId` | A GUID that uniquely identifies a binding. Survives binding reordering. |
| `Control Path` | The internal path to a control (e.g., "buttonSouth", "leftTrigger"). |
| `Device Layout` | The type of device (e.g., "Gamepad", "Keyboard", "DualShockGamepad"). |

**How they connect:**

```
InputActionAsset (your actions)
    ↓ contains
InputAction (e.g., "Jump")
    ↓ has multiple
Bindings (e.g., "Keyboard/Space", "Gamepad/buttonSouth")
    ↓ each identified by
BindingId (GUID)
    ↓ displayed via
InputPromptDisplay (shows icon/text)
```

---

## Quick Start (5 Minutes)

Want to test the system quickly? Here's the minimum setup:

### Step 1: Create Icon Maps (2 minutes)

1. **Project** > Right-click > **Create > HelloDev > Input > Input Icon Map**
2. Name it: `IconMap_Keyboard`
3. Set **Device Layout Name**: `Keyboard`
4. Right-click on the asset > **Add Common Keyboard Mappings**

Repeat for gamepad:
1. **Create > HelloDev > Input > Input Icon Map**
2. Name it: `IconMap_Gamepad`
3. Set **Device Layout Name**: `Gamepad`
4. Right-click on the asset > **Add Common Gamepad Mappings**

### Step 2: Create Icon Provider (1 minute)

1. **Create > HelloDev > Input > Input Icon Provider**
2. Name it: `InputIconProvider`
3. Add `IconMap_Gamepad` and `IconMap_Keyboard` to **Icon Maps**
4. Set `IconMap_Keyboard` as **Fallback Icon Map**

### Step 3: Add Prompt Display (2 minutes)

1. In your scene, create **UI > Text - TextMeshPro**
2. Rename to: `InteractPrompt`
3. **Add Component** > `InputPromptDisplay`
4. Configure:
   - **Action Reference**: Your interact action
   - **Binding**: Select from dropdown
   - **Icon Provider**: `InputIconProvider`
   - **Binding Text**: The TMP_Text component

### Step 4: Test It

1. Enter **Play Mode**
2. The text shows "E" (or your keyboard binding)
3. Connect a gamepad - it shows "A" (or your gamepad binding)

**It works!** Continue reading for the full setup.

---

## Full Setup Guide

### Part 1: Create Icon Maps

Icon maps define how control paths map to sprites and text for each device.

#### Step 1.1: Create a Folder for Organization

1. In **Project** window, navigate to your assets folder
2. Right-click > **Create > Folder**
3. Name it: `InputIcons`

#### Step 1.2: Create Keyboard Icon Map

1. Right-click in folder > **Create > HelloDev > Input > Input Icon Map**
2. Name it: `IconMap_Keyboard`
3. Configure:

| Field | Value | Why |
|-------|-------|-----|
| **Device Layout Name** | `Keyboard` | Matches keyboard device layouts |

4. Right-click on the asset > **Add Common Keyboard Mappings**
5. (Optional) Add custom sprites for each key

#### Step 1.3: Create Gamepad Icon Maps

For basic gamepad support:

1. **Create > HelloDev > Input > Input Icon Map**
2. Name it: `IconMap_Gamepad`
3. Set **Device Layout Name**: `Gamepad`
4. Right-click > **Add Common Gamepad Mappings**

**For platform-specific icons (Xbox, PlayStation):**

Create separate icon maps:

| Asset Name | Device Layout Name | Use For |
|------------|-------------------|---------|
| `IconMap_Xbox` | `XInputController` | Xbox controllers |
| `IconMap_PlayStation` | `DualShockGamepad` | PlayStation controllers |
| `IconMap_Switch` | `SwitchProControllerHID` | Nintendo Switch Pro |

**Important:** The system uses Unity's layout inheritance. A "DualSenseGamepadHID" controller will match "DualShockGamepad" because DualSense inherits from DualShock.

#### Step 1.4: Add Sprites to Mappings

For each mapping in your icon map:

| Field | Description |
|-------|-------------|
| **Control Path** | The path without device prefix (e.g., `buttonSouth`, `space`) |
| **Icon** | Sprite to display (drag from Project) |
| **Fallback Text** | Text to show if no icon (e.g., "A", "Space") |

**Control Path Examples:**

| Device | Control | Path |
|--------|---------|------|
| Gamepad | A button | `buttonSouth` |
| Gamepad | B button | `buttonEast` |
| Gamepad | X button | `buttonWest` |
| Gamepad | Y button | `buttonNorth` |
| Gamepad | Left trigger | `leftTrigger` |
| Gamepad | Right bumper | `rightShoulder` |
| Gamepad | D-pad up | `dpad/up` |
| Keyboard | E key | `e` |
| Keyboard | Space | `space` |
| Keyboard | Left Shift | `leftShift` |

---

### Part 2: Create Icon Provider

The provider selects the correct icon map based on the binding's device.

#### Step 2.1: Create the Provider Asset

1. **Create > HelloDev > Input > Input Icon Provider**
2. Name it: `InputIconProvider`

#### Step 2.2: Configure the Provider

| Field | What to Assign | Why |
|-------|----------------|-----|
| **Icon Maps** | Add all your icon maps | Provider searches these in order |
| **Fallback Icon Map** | `IconMap_Keyboard` | Used when no match found |

**Order matters!** More specific maps should come first:
1. `IconMap_PlayStation` (specific)
2. `IconMap_Xbox` (specific)
3. `IconMap_Gamepad` (generic fallback for gamepads)
4. `IconMap_Keyboard` (keyboard)

#### Checkpoint: Verify Assets

Your InputIcons folder should contain:
```
InputIcons/
├── IconMap_Keyboard
├── IconMap_Gamepad
├── IconMap_PlayStation (optional)
├── IconMap_Xbox (optional)
└── InputIconProvider
```

---

### Part 3: Display Input Prompts

Now we'll show prompts in the UI that update based on bindings.

#### Step 3.1: Create the UI Text

1. In **Hierarchy**, right-click > **UI > Text - TextMeshPro**
2. If prompted, click **Import TMP Essentials**
3. Rename to: `InteractPromptText`
4. Position and style as desired

#### Step 3.2: Add InputPromptDisplay Component

1. Select `InteractPromptText` GameObject
2. **Add Component** > search `InputPromptDisplay` > add it

#### Step 3.3: Configure the Component

| Field | What to Assign | Why |
|-------|----------------|-----|
| **Action Reference** | Your InputActionReference | The action to display |
| **Binding** | Select from dropdown | Which binding to show |
| **Icon Provider** | `InputIconProvider` | For device-specific icons |
| **Binding Text** | The TMP_Text component | Where to show text |
| **Text Format** | `Press [{0}] to interact` | Format string ({0} = binding) |
| **Prefer Icon** | ✓ checked | Show icon when available |
| **Exclusive Display** | ✓ checked | Hide text when showing icon |

#### Step 3.4: (Optional) Add Icon Display

If you want to show icons instead of text:

1. Add **UI > Image** as sibling to the text
2. Rename to: `InteractPromptIcon`
3. Select your `InputPromptDisplay` component
4. Assign the Image to **Binding Icon** field

**With ExclusiveDisplay enabled:**
- Icon shows when available, text hidden
- Text shows when no icon, icon hidden

**With ExclusiveDisplay disabled:**
- Both show simultaneously

#### Checkpoint: Test Prompt Display

1. Enter **Play Mode**
2. Verify the prompt shows your keyboard binding
3. (Optional) Connect a gamepad and verify it shows gamepad binding
4. (Optional) Rebind the action and verify prompt updates

---

### Part 4: Input Buttons

For buttons that respond to input AND show their binding:

#### Step 4.1: Create Button with Prompt

1. Create **UI > Button - TextMeshPro**
2. Rename to: `ConfirmButton`
3. **Add Component** > `InputButtonWithPrompt`

#### Step 4.2: Configure the Button

| Field | Value | Why |
|-------|-------|-----|
| **Action Reference** | Your confirm action | Triggers and displays this action |
| **Binding** | Select from dropdown | Which binding to display |
| **Icon Provider** | `InputIconProvider` | For icons |
| **Binding Text** | Button's text child | Shows binding text |
| **Text Format** | `[{0}] Confirm` | Format for display |
| **Respect Canvas Group** | ✓ checked | Ignores input when not interactable |

#### Step 4.3: Connect Events

Option A: Inspector wiring
1. Find **On Action Performed** event
2. Click **+** to add listener
3. Drag target object and select method

Option B: Code wiring
```csharp
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
```

---

### Part 5: Rebinding System

Allow players to customize their controls.

#### Step 5.1: Create Rebind Manager

1. In **Hierarchy**, right-click > **Create Empty**
2. Name it: `InputRebindManager`
3. **Add Component** > `InputRebindManager`

#### Step 5.2: Configure the Manager

| Field | Value | Why |
|-------|-------|-----|
| **Input Actions** | Your InputActionAsset | The asset to manage rebindings for |
| **Player Prefs Key** | `InputBindings` | Key for saving bindings |
| **Load On Enable** | ✓ checked | Auto-load saved bindings |
| **Save On Disable** | ✓ checked | Auto-save when exiting |
| **Cancel Binding Paths** | `<Keyboard>/escape` | Keys that cancel rebinding |
| **Rebind Timeout** | `5` | Seconds before timeout (0 = no timeout) |

#### Step 5.3: Create Rebind UI

For each rebindable action, create:

1. **UI > Button** - "Rebind" button
2. **TMP_Text** - Shows current binding
3. Script to connect them:

```csharp
using HelloDev.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebindRow : MonoBehaviour
{
    [SerializeField] private InputButtonWithPrompt actionButton;
    [SerializeField] private Button rebindButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject waitingOverlay;

    void OnEnable()
    {
        rebindButton.onClick.AddListener(StartRebind);
        resetButton.onClick.AddListener(ResetBinding);

        var manager = InputRebindManager.Instance;
        manager.OnRebindStarted.AddListener(OnRebindStarted);
        manager.OnRebindCompleted.AddListener(OnRebindCompleted);
        manager.OnRebindCanceled.AddListener(OnRebindCanceled);
    }

    void OnDisable()
    {
        rebindButton.onClick.RemoveListener(StartRebind);
        resetButton.onClick.RemoveListener(ResetBinding);

        var manager = InputRebindManager.Instance;
        if (manager != null)
        {
            manager.OnRebindStarted.RemoveListener(OnRebindStarted);
            manager.OnRebindCompleted.RemoveListener(OnRebindCompleted);
            manager.OnRebindCanceled.RemoveListener(OnRebindCanceled);
        }
    }

    void StartRebind()
    {
        actionButton.StartRebind();
    }

    void ResetBinding()
    {
        actionButton.ResetBinding();
        statusText.text = "Reset to default";
    }

    void OnRebindStarted()
    {
        waitingOverlay.SetActive(true);
        statusText.text = "Press a key...";
    }

    void OnRebindCompleted()
    {
        waitingOverlay.SetActive(false);
        statusText.text = "Saved!";
    }

    void OnRebindCanceled()
    {
        waitingOverlay.SetActive(false);
        statusText.text = "Canceled";
    }
}
```

#### Step 5.4: Add Reset All Button

```csharp
[SerializeField] private Button resetAllButton;

void OnEnable()
{
    resetAllButton.onClick.AddListener(ResetAll);
}

void ResetAll()
{
    InputRebindManager.Instance.ResetAllBindings();
}
```

---

## Advanced Topics

### Custom Icon Handling

For effects beyond simple icon/text swapping:

```csharp
using HelloDev.Input;
using UnityEngine;

public class AnimatedPrompt : MonoBehaviour
{
    [SerializeField] private InputPromptDisplay promptDisplay;
    [SerializeField] private Animator iconAnimator;
    [SerializeField] private ParticleSystem highlightParticles;

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
        // Different animation based on device
        bool isGamepad = deviceLayoutName != null &&
                         (deviceLayoutName.Contains("Gamepad") ||
                          deviceLayoutName.Contains("Controller"));

        iconAnimator.SetBool("IsGamepad", isGamepad);

        // Pulse effect when binding changes
        highlightParticles.Play();
    }
}
```

### Runtime Icon Provider Switching

Switch between icon sets at runtime:

```csharp
public class IconThemeSwitcher : MonoBehaviour
{
    [SerializeField] private InputPromptDisplay[] allPrompts;
    [SerializeField] private InputIconProvider_SO standardIcons;
    [SerializeField] private InputIconProvider_SO colorblindIcons;

    public void UseColorblindMode(bool enabled)
    {
        var provider = enabled ? colorblindIcons : standardIcons;

        foreach (var prompt in allPrompts)
        {
            prompt.SetIconProvider(provider);
        }
    }
}
```

### Multiple Bindings Per Action

Display different bindings for the same action:

```csharp
public class DualBindingDisplay : MonoBehaviour
{
    [SerializeField] private InputPromptDisplay keyboardPrompt;
    [SerializeField] private InputPromptDisplay gamepadPrompt;
    [SerializeField] private InputActionReference action;

    void Start()
    {
        // Both reference the same action, different bindings
        // The binding dropdown in the editor lets you select which one
        keyboardPrompt.ActionReference = action;
        gamepadPrompt.ActionReference = action;

        // Bindings are set via BindingId in the inspector
    }
}
```

---

## Device Layout Reference

Common device layout names for your icon maps:

| Layout Name | Matches |
|-------------|---------|
| `Keyboard` | All keyboards |
| `Mouse` | All mice |
| `Gamepad` | Any gamepad (generic fallback) |
| `XInputController` | Xbox controllers (Windows) |
| `DualShockGamepad` | PlayStation 4 controllers |
| `DualSenseGamepadHID` | PlayStation 5 controllers (matches DualShockGamepad) |
| `SwitchProControllerHID` | Nintendo Switch Pro Controller |

**Inheritance:** More specific layouts inherit from generic ones. A DualSense controller will match `DualShockGamepad` if no `DualSenseGamepadHID` map exists.

---

## Control Path Reference

### Gamepad Controls

| Control | Path |
|---------|------|
| Face Button South (A/Cross) | `buttonSouth` |
| Face Button East (B/Circle) | `buttonEast` |
| Face Button West (X/Square) | `buttonWest` |
| Face Button North (Y/Triangle) | `buttonNorth` |
| Left Stick Button | `leftStickPress` |
| Right Stick Button | `rightStickPress` |
| Left Shoulder | `leftShoulder` |
| Right Shoulder | `rightShoulder` |
| Left Trigger | `leftTrigger` |
| Right Trigger | `rightTrigger` |
| D-Pad Up | `dpad/up` |
| D-Pad Down | `dpad/down` |
| D-Pad Left | `dpad/left` |
| D-Pad Right | `dpad/right` |
| Start | `start` |
| Select | `select` |

### Keyboard Controls

| Control | Path |
|---------|------|
| Letters | `a`, `b`, `c`, ... `z` |
| Numbers | `1`, `2`, ... `0` |
| Space | `space` |
| Enter | `enter` |
| Escape | `escape` |
| Tab | `tab` |
| Backspace | `backspace` |
| Left Shift | `leftShift` |
| Right Shift | `rightShift` |
| Left Ctrl | `leftCtrl` |
| Left Alt | `leftAlt` |
| Arrow Keys | `upArrow`, `downArrow`, `leftArrow`, `rightArrow` |
| F Keys | `f1`, `f2`, ... `f12` |

### Mouse Controls

| Control | Path |
|---------|------|
| Left Button | `leftButton` |
| Right Button | `rightButton` |
| Middle Button | `middleButton` |
| Scroll | `scroll` |

---

## Troubleshooting

### Prompt Shows Wrong Binding

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Always shows keyboard | Wrong binding selected | Check Binding dropdown in inspector |
| Shows raw control path | No icon map match | Verify Device Layout Name matches |
| Icon missing | Sprite not assigned | Check Icon field in mapping |

### Rebinding Issues

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Rebind doesn't save | SaveOnDisable unchecked | Enable or call SaveBindings() manually |
| Bindings reset on restart | Wrong PlayerPrefs key | Use consistent key across scenes |
| Can't rebind | Action disabled | Enable action before rebinding |

### Icon Provider Not Matching

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Generic gamepad icons always | Specific map missing | Add map for your controller type |
| Fallback text showing | No mapping for control | Add mapping in icon map |
| Wrong device detected | Binding index wrong | Verify correct binding selected |

### Debugging Tips

1. **Enable logging**: Set `enableDebugLogging` true on components
2. **Check Console**: Look for `[Input]` and `[Rebind]` prefixed messages
3. **Verify references**: Select component and confirm all fields assigned
4. **Test bindings**: Use Unity's Input Debugger (Window > Analysis > Input Debugger)

---

## Architecture Reference

### Class Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                        SCRIPTABLEOBJECTS                        │
│                    (Assets in Project folder)                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   InputIconProvider_SO ────────┐                                │
│   - iconMaps[]                 │ references                     │
│   - fallbackIconMap            │                                │
│                                ▼                                │
│   InputIconMap_SO              InputIconMap_SO                  │
│   - deviceLayoutName           - deviceLayoutName               │
│   - mappings[]                 - mappings[]                     │
│     (controlPath → sprite)       (controlPath → sprite)         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ used by
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     SCENE COMPONENTS                            │
│                  (MonoBehaviours in scene)                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   InputPromptDisplay                                            │
│   - actionReference                                             │
│   - bindingId (GUID)                                            │
│   - iconProvider (reference)                                    │
│   - Calls GetBindingDisplayString() to get:                     │
│     • displayString                                             │
│     • deviceLayoutName                                          │
│     • controlPath                                               │
│                                                                 │
│   InputActionButton                InputButtonWithPrompt        │
│   - actionReference                - (combines both)            │
│   - OnActionPerformed              - actionReference            │
│                                    - bindingId                  │
│                                    - iconProvider               │
│                                    - OnActionPerformed          │
│                                                                 │
│   InputRebindManager (Singleton)                                │
│   - inputActions (asset)                                        │
│   - StartRebind() / SaveBindings() / LoadBindings()             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Event Flow

```
Binding Changes (rebind, device switch)
            │
            ▼
InputSystem.onActionChange (BoundControlsChanged)
            │
            ▼
InputPromptDisplay.OnActionChange (static)
            │
            ▼
Each instance calls UpdateBindingDisplay()
            │
            ├──► action.GetBindingDisplayString(bindingId)
            │    Returns: displayString, deviceLayoutName, controlPath
            │
            ├──► iconProvider.GetBinding(deviceLayoutName, controlPath)
            │    Returns: (icon, fallbackText)
            │
            ├──► Update UI (text and/or icon)
            │
            └──► Fire updateBindingUIEvent (for custom handlers)
```

---

## Best Practices

1. **Create icon maps per platform** - Xbox, PlayStation, Nintendo, Keyboard
2. **Use layout inheritance** - Generic "Gamepad" map catches unknown controllers
3. **Always set fallback** - Keyboard is a good default
4. **Test with multiple devices** - Connect different controllers
5. **Use ExclusiveDisplay** - Cleaner UI with icon OR text
6. **Enable rebinding early** - Players expect control customization
7. **Save bindings automatically** - Use LoadOnEnable/SaveOnDisable
8. **Add reset option** - Let players restore defaults

---

## Related Documentation

- [README.md](../README.md) - Quick reference and API
- Unity Input System documentation
- Unity Input System samples (in Package Manager)
