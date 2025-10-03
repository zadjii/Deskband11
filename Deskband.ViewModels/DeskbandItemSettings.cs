using System.Text.Json.Serialization;

namespace Deskband.ViewModels
{
    public class DeskbandItemSettings
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("enabled")]
        public bool IsEnabled { get; set; } = true;
    }

}
