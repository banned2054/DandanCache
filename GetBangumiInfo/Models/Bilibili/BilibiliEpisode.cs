using GetBangumiInfo.Utils;
using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Bilibili;

public class BilibiliEpisode
{
    [JsonProperty("cid")]
    public long Cid { get; set; }

    [JsonProperty("pub_time")]
    public long PubTimeUnix { get; set; }

    [JsonIgnore]
    public DateTimeOffset PubDate => DateUtils.FromUnixTimeToBeijing(PubTimeUnix);

    [JsonProperty("link")]
    public string? Link { get; set; }
}
