using System.Text.Json.Serialization;

namespace PowerDock;

/// <summary>
/// JSON source generation context for PowerDock settings serialization
/// This enables fast, reflection-free JSON serialization/deserialization
/// </summary>
[JsonSerializable(typeof(Settings))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    AllowTrailingCommas = true)]
public partial class PowerDockSourceGenerationContext : JsonSerializerContext
{
}