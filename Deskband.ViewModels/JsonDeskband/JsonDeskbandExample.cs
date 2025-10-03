using Deskband.ViewModels.JsonDeskband;

namespace Deskband.ViewModels;

/// <summary>
/// Example usage of the JSON deskband loader
/// </summary>
public static class JsonDeskbandExample
{
    /// <summary>
    /// Loads the test deskband configuration and returns TaskbarItemViewModel instances
    /// </summary>
    /// <returns>List of TaskbarItemViewModel instances</returns>
    public static async Task<List<TaskbarItemViewModel>> LoadTestDeskbandsAsync()
    {
        // Path to the test JSON file
        string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Public", "test_registration.json");

        if (File.Exists(jsonPath))
        {
            return await JsonDeskbandLoader.LoadFromFileAsync(jsonPath, string.Empty);
        }

        // Fallback: load from inline JSON if file doesn't exist
        string testJson = """
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
                            "name": "Button 2 Tooltip",
                            "invokeUri": "https://www.github.com"
                        }
                    ]
                }
            ]
        }
        """;

        return JsonDeskbandLoader.LoadFromJson(testJson, string.Empty);
    }

    /// <summary>
    /// Creates a sample JSON deskband configuration programmatically
    /// </summary>
    /// <returns>JSON string representing deskband configuration</returns>
    public static string CreateSampleJsonConfig()
    {
        JsonDeskbandProvider provider = new()
        {
            ProviderId = "com.example.myDeskband",
            Deskbands = new List<JsonDeskband.JsonDeskband>
            {
                new() {
                    Id = "sample-001",
                    Title = "My Deskband",
                    Subtitle = "Custom created",
                    Icon = "\uE70F", // Settings icon
                    Buttons = new List<JsonButton>
                    {
                        new() {
                            Id = "web-btn-001",
                            Name = "Search",
                            Icon = "\uE721", // Search icon
                            InvokeUri = "https://www.google.com"
                        },
                        new() {
                            Id = "web-btn-002",
                            Name = "News",
                            Icon = "\uE789", // News icon
                            InvokeUri = "https://news.ycombinator.com"
                        }
                    }
                }
            }
        };

        return System.Text.Json.JsonSerializer.Serialize(provider, JsonDeskbandSourceGenerationContext.Default.JsonDeskbandProvider);
    }
}