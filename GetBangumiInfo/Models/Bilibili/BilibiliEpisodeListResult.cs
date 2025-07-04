using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Bilibili;

public class BilibiliEpisodeListResult
{
    [JsonProperty("episodes")]
    public List<BilibiliEpisode>? Episodes { get; set; }
}
