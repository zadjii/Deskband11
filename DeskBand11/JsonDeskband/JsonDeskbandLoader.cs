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
    /// <param name="publicFolder">Path to the public folder for resolving relative paths</param>
    /// <returns>List of TaskbarItemViewModel instances</returns>
    public static async Task<List<TaskbarItemViewModel>> LoadFromFileAsync(string jsonFilePath, string publicFolder)
    {
        string json = await File.ReadAllTextAsync(jsonFilePath);
        return LoadFromJson(json, publicFolder);
    }

    /// <summary>
    /// Loads deskbands from a JSON string (AOT-compatible)
    /// </summary>
    /// <param name="json">JSON string</param>
    /// <param name="publicFolder">Path to the public folder for resolving relative paths</param>
    /// <returns>List of TaskbarItemViewModel instances</returns>
    public static List<TaskbarItemViewModel> LoadFromJson(string json, string publicFolder)
    {
        JsonDeskbandProvider? provider = JsonSerializer.Deserialize(json, JsonDeskbandSourceGenerationContext.Default.JsonDeskbandProvider);
        return provider?.Deskbands == null ? new List<TaskbarItemViewModel>() : ConvertToTaskbarItems(provider.Deskbands, publicFolder);
    }

    ///// <summary>
    ///// Loads deskbands from a JSON string using custom JsonSerializerOptions (fallback for non-AOT scenarios)
    ///// </summary>
    ///// <param name="json">JSON string</param>
    ///// <param name="options">Custom JSON serializer options</param>
    ///// <returns>List of TaskbarItemViewModel instances</returns>
    //public static List<TaskbarItemViewModel> LoadFromJson(string json, JsonSerializerOptions options)
    //{
    //    var provider = JsonSerializer.Deserialize<JsonDeskbandProvider>(json, options);
    //    if (provider?.Deskbands == null)
    //    {
    //        return new List<TaskbarItemViewModel>();
    //    }

    //    return ConvertToTaskbarItems(provider.Deskbands, string.Empty);
    //}

    /// <summary>
    /// Converts JSON deskband objects to TaskbarItemViewModel instances
    /// </summary>
    /// <param name="jsonDeskbands">List of JSON deskband objects</param>
    /// <returns>List of TaskbarItemViewModel instances</returns>
    private static List<TaskbarItemViewModel> ConvertToTaskbarItems(List<JsonDeskband> jsonDeskbands, string publicFolder)
    {
        List<TaskbarItemViewModel> result = new();

        foreach (JsonDeskband jsonDeskband in jsonDeskbands)
        {
            JsonTaskbarItemViewModel taskbarItem = new(jsonDeskband, publicFolder);
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
    public JsonTaskbarItemViewModel(JsonDeskband jsonDeskband, string publicFolder)
    {
        Id = jsonDeskband.Id;
        Title = jsonDeskband.Title;
        Subtitle = jsonDeskband.Subtitle ?? string.Empty;

        // Set the icon if provided
        if (!string.IsNullOrEmpty(jsonDeskband.Icon))
        {
            Icon = CreateIconFromPath(jsonDeskband.Icon, publicFolder);
        }

        // Add buttons if provided
        if (jsonDeskband.Buttons != null)
        {
            foreach (JsonButton jsonButton in jsonDeskband.Buttons)
            {
                if (!string.IsNullOrEmpty(jsonButton.InvokeUri))
                {
                    WebUriCommand command = new(
                        jsonButton.Id,
                        jsonButton.Name,
                        jsonButton.Icon,
                        jsonButton.InvokeUri);

                    Buttons.Add(new CommandViewModel(command));
                }
            }
        }
    }

    private static IconInfo CreateIconFromPath(string iconPath, string publicFolder = "")
    {
        // if it's a local path, and we have a public folder, combine them
        if (Uri.TryCreate(iconPath, UriKind.RelativeOrAbsolute, out Uri? uri))
        {
            if (!uri.IsAbsoluteUri && !string.IsNullOrEmpty(publicFolder))
            {
                string combinedPath = Path.Combine(publicFolder, iconPath);
                string actualPath = Path.GetFullPath(combinedPath);
                if (File.Exists(actualPath))
                {
                    return new IconInfo(combinedPath);
                }
            }
        }

        // Otherwise, whatever, just use the normal icon parsing. 
        return new IconInfo(iconPath);
    }
}