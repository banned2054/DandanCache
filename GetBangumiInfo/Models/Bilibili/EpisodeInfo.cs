using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Bilibili;

public class EpisodeInfo
{
    [JsonProperty("cid")]
    public long Cid { get; set; }

    [JsonProperty("pub_time")]
    public long PubTimeUnix { get; set; }

    [JsonIgnore]
    public DateTime PubDate =>
        DateTimeOffset.FromUnixTimeSeconds(PubTimeUnix)
                      .ToOffset(TimeSpan.FromHours(8)) // 北京时间
                      .DateTime;

    [JsonProperty("link")]
    public string? Link { get; set; }
}
