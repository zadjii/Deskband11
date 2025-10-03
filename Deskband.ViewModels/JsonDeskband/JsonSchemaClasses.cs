using System.Text.Json.Serialization;

namespace Deskband.ViewModels.JsonDeskband;

/// <summary>
/// Root object for the JSON deskband configuration
/// </summary>
public class JsonDeskbandProvider
{
    [JsonPropertyName("providerId")]
    public required string ProviderId { get; set; }

    [JsonPropertyName("deskbands")]
    public required List<JsonDeskband> Deskbands { get; set; }
}

/// <summary>
/// Individual deskband definition in JSON
/// </summary>
public class JsonDeskband
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("buttons")]
    public List<JsonButton>? Buttons { get; set; }
}

/// <summary>
/// Button definition in JSON
/// </summary>
public class JsonButton
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("invokeUri")]
    public string? InvokeUri { get; set; }
}