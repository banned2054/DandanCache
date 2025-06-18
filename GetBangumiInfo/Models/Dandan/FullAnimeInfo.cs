using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Dandan;

public class FullAnimeInfo
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("titles")]
    public List<TitleInfo>? TitleList { get; set; }

    [JsonProperty("episodes")]
    public List<EpisodeInfo>? EpisodeList { get; set; }

    [JsonProperty("bangumiUrl")]
    public string BangumiUrl { get; set; } = string.Empty;
}
