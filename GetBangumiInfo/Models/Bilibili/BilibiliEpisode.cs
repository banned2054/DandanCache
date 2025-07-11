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
    public DateTimeOffset PubDate => TimeUtils.ParseUnix(PubTimeUnix);

    [JsonProperty("link")]
    public string? Link { get; set; }

    [JsonProperty("show_title")]
    public string? Title { get; set; }

    [JsonProperty("title")]
    public string? NumberStr { get; set; }

    [JsonIgnore]
    public int? Number => string.IsNullOrWhiteSpace(NumberStr) || !int.TryParse(NumberStr, out _)
        ? null
        : int.Parse(NumberStr);
}
