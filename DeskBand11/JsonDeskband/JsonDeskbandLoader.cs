using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Text.Json;

namespace DeskBand11.JsonDeskband;

/// <summary>
/// Loads TaskbarItemViewModel instances from JSON deskband configurations
/// </summary>
public static class JsonDeskbandLoader
{
    /// <summary>
    /// Loads deskbands from a JSON file
    /// </summary>
    /// <param name="jsonFilePath">Path to the JSON file</param>
    /// <returns>List of TaskbarItemViewModel instances</returns>
    public static async Task<List<TaskbarItemViewModel>> LoadFromFileAsync(string jsonFilePath)
    {
        var json = await File.ReadAllTextAsync(jsonFilePath);
        return LoadFromJson(json);
    }

    /// <summary>
    /// Loads deskbands from a JSON string (AOT-compatible)
    /// </summary>
    /// <param name="json">JSON string</param>
    /// <returns>List of TaskbarItemViewModel instances</returns>
    public static List<TaskbarItemViewModel> LoadFromJson(string json)
    {
        var provider = JsonSerializer.Deserialize(json, JsonDeskbandSourceGenerationContext.Default.JsonDeskbandProvider);
        if (provider?.Deskbands == null)
        {
            return new List<TaskbarItemViewModel>();
        }

        return ConvertToTaskbarItems(provider.Deskbands);
    }

    /// <summary>
    /// Loads deskbands from a JSON string using custom JsonSerializerOptions (fallback for non-AOT scenarios)
    /// </summary>
    /// <param name="json">JSON string</param>
    /// <param name="options">Custom JSON serializer options</param>
    /// <returns>List of TaskbarItemViewModel instances</returns>
    public static List<TaskbarItemViewModel> LoadFromJson(string json, JsonSerializerOptions options)
    {
        var provider = JsonSerializer.Deserialize<JsonDeskbandProvider>(json, options);
        if (provider?.Deskbands == null)
        {
            return new List<TaskbarItemViewModel>();
        }

        return ConvertToTaskbarItems(provider.Deskbands);
    }

    /// <summary>
    /// Converts JSON deskband objects to TaskbarItemViewModel instances
    /// </summary>
    /// <param name="jsonDeskbands">List of JSON deskband objects</param>
    /// <returns>List of TaskbarItemViewModel instances</returns>
    private static List<TaskbarItemViewModel> ConvertToTaskbarItems(List<JsonDeskband> jsonDeskbands)
    {
        var result = new List<TaskbarItemViewModel>();

        foreach (var jsonDeskband in jsonDeskbands)
        {
            var taskbarItem = new JsonTaskbarItemViewModel(jsonDeskband);
            result.Add(taskbarItem);
        }

        return result;
    }
}

/// <summary>
/// A TaskbarItemViewModel that is created from JSON configuration
/// </summary>
public partial class JsonTaskbarItemViewModel : TaskbarItemViewModel
{
    public JsonTaskbarItemViewModel(JsonDeskband jsonDeskband)
    {
        Id = jsonDeskband.Id;
        Title = jsonDeskband.Title;
        Subtitle = jsonDeskband.Subtitle ?? string.Empty;
        
        // Set the icon if provided
        if (!string.IsNullOrEmpty(jsonDeskband.Icon))
        {
            Icon = CreateIconFromPath(jsonDeskband.Icon);
        }

        // Add buttons if provided
        if (jsonDeskband.Buttons != null)
        {
            foreach (var jsonButton in jsonDeskband.Buttons)
            {
                if (!string.IsNullOrEmpty(jsonButton.InvokeUri))
                {
                    var command = new WebUriCommand(
                        jsonButton.Id,
                        jsonButton.Name,
                        jsonButton.Icon,
                        jsonButton.InvokeUri);
                    
                    Buttons.Add(new CommandViewModel(command));
                }
            }
        }
    }

    private static IconInfo CreateIconFromPath(string iconPath)
    {
        // Handle different types of icon paths
        if (iconPath.StartsWith("http://") || iconPath.StartsWith("https://"))
        {
            // For web icons, use a generic web icon for now
            // In a full implementation, you might want to download and cache these
            return new IconInfo("\uE774"); // Globe icon
        }
        else if (iconPath.StartsWith("\\u") && iconPath.Length == 6)
        {
            // Handle Unicode escape sequences like "\uE701"
            if (int.TryParse(iconPath.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int unicodeValue))
            {
                return new IconInfo(((char)unicodeValue).ToString());
            }
            else
            {
                return new IconInfo(iconPath);
            }
        }
        else
        {
            // Local file path or direct Unicode character
            return new IconInfo(iconPath);
        }
    }
}