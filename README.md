# HelloDev Input

Input display, device tracking, and rebinding utilities for Unity Input System.

## Features

- **Input Icon Maps** - Map input control paths to display sprites for keyboard, gamepad, etc.
- **Input Prompt Display** - Automatically show the correct input icon based on the current device
- **Input Rebinding** - Built-in rebind manager for runtime key rebinding
- **UI Components** - Ready-to-use UI components for input prompts

## Installation

### Via Unity Package Manager (Git URL)

1. Open **Window > Package Manager**
2. Click **+** and select **Add package from git URL**
3. Enter: `https://github.com/joaovictoralencar/com.hellodev.input.git`

### Via manifest.json

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.hellodev.input": "https://github.com/joaovictoralencar/com.hellodev.input.git"
  }
}
```

## Requirements

- Unity 2022.3+
- Unity Input System 1.7.0+
- TextMeshPro 3.0.6+

## Quick Start

### 1. Create an Input Icon Map

Create an `InputIconMap_SO` asset for each device type (keyboard, Xbox, PlayStation, etc.):

1. Right-click in Project > **Create > HelloDev > Input > Input Icon Map**
2. Add control path to sprite mappings

### 2. Display Input Prompts

Add the `InputPromptDisplay` component to a UI Image:

```csharp
// The component will automatically update the icon based on the current input device
[SerializeField] private InputPromptDisplay promptDisplay;
[SerializeField] private InputActionReference action;

void Start()
{
    promptDisplay.SetAction(action);
}
```

## Components

| Component | Description |
|-----------|-------------|
| `InputIconMap_SO` | ScriptableObject mapping control paths to sprites |
| `InputIconProvider_SO` | Provides icons for multiple device types |
| `InputPromptDisplay` | UI component that shows the correct icon for an action |
| `InputRebindManager` | Handles runtime input rebinding |
| `InputActionButton` | Button that triggers an input action |
| `InputButtonWithPrompt` | Button with integrated input prompt display |

## License

MIT License - see [LICENSE.md](LICENSE.md) for details.
