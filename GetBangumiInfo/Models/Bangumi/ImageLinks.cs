using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Bangumi;

public class ImageLinks
{
    [JsonProperty("large")]
    public string Large { get; set; } = string.Empty;

    [JsonProperty("common")]
    public string Common { get; set; } = string.Empty;

    [JsonProperty("medium")]
    public string Medium { get; set; } = string.Empty;

    [JsonProperty("small")]
    public string Small { get; set; } = string.Empty;

    [JsonProperty("grid")]
    public string Grid { get; set; } = string.Empty;
}