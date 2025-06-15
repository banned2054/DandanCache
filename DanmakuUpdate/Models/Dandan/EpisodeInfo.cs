using Newtonsoft.Json;

namespace DanmakuUpdate.Models.Dandan;

public class EpisodeInfo
{
    [JsonProperty("seasonId")]
    public string? SeasonId { get; set; }

    [JsonProperty("episodeId")]
    public int EpisodeId { get; set; }

    [JsonProperty("episodeTitle")]
    public string EpisodeTitle { get; set; } = string.Empty;

    [JsonProperty("episodeNumber")]
    public string EpisodeNumber { get; set; } = string.Empty;

    [JsonProperty("airDate")]
    public string AirDate { get; set; } = string.Empty;
}
