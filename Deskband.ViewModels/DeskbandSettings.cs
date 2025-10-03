using System.Text.Json.Serialization;

namespace Deskband.ViewModels
{
    public class DeskbandSettings
    {
        [JsonPropertyName("deskbands")]
        public List<DeskbandItemSettings> Deskbands { get; set; } = new();
    }

}
