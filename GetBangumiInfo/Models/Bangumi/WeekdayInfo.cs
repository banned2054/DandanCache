using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Bangumi;

public class WeekdayInfo
{
    [JsonProperty("en")]
    public string? En { get; set; } = string.Empty;

    [JsonProperty("cn")]
    public string? Cn { get; set; } = string.Empty;

    [JsonProperty("ja")]
    public string? Ja { get; set; } = string.Empty;

    [JsonProperty("id")]
    public int? Id { get; set; }
}
