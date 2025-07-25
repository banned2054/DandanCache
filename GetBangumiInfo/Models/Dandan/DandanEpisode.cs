using GetBangumiInfo.Utils;
using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Dandan;

public class DandanEpisode
{
    [JsonProperty("episodeId")]
    public int EpisodeId { get; set; }

    [JsonProperty("episodeTitle")]
    public string? Title { get; set; }

    [JsonProperty("episodeNumber")]
    public string? EpisodeNumber { get; set; }

    [JsonProperty("airDate")]
    public string? AirDateStr { get; set; }

    [JsonIgnore]
    public DateTimeOffset? AirDate => string.IsNullOrWhiteSpace(AirDateStr)
        ? null
        : TimeUtils.ParseString(AirDateStr, "yyyy-MM-dd'T'HH:mm:ss");
}
