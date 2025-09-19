# JSON Deskband Loader

This module provides AOT-compatible C# classes for parsing JSON configurations into `TaskbarItemViewModel` instances for DeskBand11.

## Features

- **AOT Compatible**: Uses System.Text.Json source generators for reflection-free JSON parsing
- **Type Safe**: Strongly-typed classes with proper nullability annotations
- **Web URI Support**: Built-in support for opening web links via buttons
- **Icon Support**: Handles Unicode icons, local paths, and web URLs
- **Extensible**: Easy to extend with additional command types

## Usage

### Basic Usage

```csharp
using DeskBand11.JsonDeskband;

// Load from file
var deskbands = await JsonDeskbandLoader.LoadFromFileAsync("config.json");

// Load from JSON string
var jsonConfig = """
{
    "providerId": "com.example.provider",
    "deskbands": [
        {
            "id": "my-deskband",
            "title": "My Deskband",
            "subtitle": "Custom subtitle",
            "icon": "\uE70F",
            "buttons": [
                {
                    "id": "search-btn",
                    "name": "Search",
                    "icon": "\uE721",
                    "invokeUri": "https://www.google.com"
                }
            ]
        }
    ]
}
""";

var deskbands = JsonDeskbandLoader.LoadFromJson(jsonConfig);
```

### Example JSON Schema

```json
{
    "providerId": "com.zadjii.deskbandJson",
    "deskbands": [
        {
            "id": "test-000",
            "icon": "Assets\\StoreLogo.png",
            "title": "Test Deskband", 
            "subtitle": "I came from JSON",
            "buttons": [
                {
                    "id": "btn-001",
                    "icon": "\uE701",
                    "name": "Bing",
                    "invokeUri": "https://www.bing.com"
                },
                {
                    "id": "btn-002", 
                    "icon": "https://github.com/favicon.ico",
                    "name": "GitHub",
                    "invokeUri": "https://www.github.com"
                }
            ]
        }
    ]
}
```

## Classes

### JsonDeskbandProvider
Root configuration object containing provider ID and list of deskbands.

### JsonDeskband
Individual deskband configuration with ID, title, subtitle, icon, and buttons.

### JsonButton
Button configuration with ID, name, icon, and URI to invoke.

### JsonTaskbarItemViewModel
Extends `TaskbarItemViewModel` with JSON-based initialization.

### WebUriCommand
Command implementation that opens URIs in the default browser.

### JsonDeskbandSourceGenerationContext
AOT-compatible JSON serialization context for optimal performance.

## Icon Support

The system supports several icon formats:

- **Unicode Icons**: `"\uE701"` - Segoe MDL2 Assets font icons
- **Local Paths**: `"Assets\\icon.png"` - Local file paths
- **Web URLs**: `"https://example.com/favicon.ico"` - Web-hosted icons (shows generic globe icon)

## AOT Compatibility

This module is designed to work with .NET's Ahead-of-Time (AOT) compilation:

- Uses source generators instead of reflection
- All JSON types are pre-compiled
- No runtime code generation
- Minimal memory allocation during parsing

## Extending

To add new command types:

1. Create a class inheriting from `InvokableCommand`
2. Implement the `Invoke()` method
3. Update `JsonButton` to include new properties as needed
4. Modify `JsonTaskbarItemViewModel` to handle the new command type