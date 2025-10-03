using System.Text.Json.Serialization;

namespace Deskband.ViewModels.JsonDeskband;

/// <summary>
/// JSON source generation context for AOT compatibility
/// This enables fast, reflection-free JSON serialization/deserialization
/// </summary>
[JsonSerializable(typeof(JsonDeskbandProvider))]
[JsonSerializable(typeof(JsonDeskband))]
[JsonSerializable(typeof(JsonButton))]
[JsonSerializable(typeof(DeskbandItemSettings))]
[JsonSerializable(typeof(DeskbandSettings))]
[JsonSerializable(typeof(List<JsonDeskband>))]
[JsonSerializable(typeof(List<JsonButton>))]
[JsonSerializable(typeof(List<DeskbandItemSettings>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    AllowTrailingCommas = true)]
public partial class JsonDeskbandSourceGenerationContext : JsonSerializerContext
{
}