using GetBangumiInfo.Utils;
using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Bangumi;

public class BangumiItem
{
    [JsonProperty("id")]
    public int? Id { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("type")]
    public int? Type { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("name_cn")]
    public string? NameCn { get; set; }

    [JsonProperty("air_date")]
    public string? AirDateStr { get; set; }

    [JsonIgnore]
    public DateTimeOffset? AirDate => string.IsNullOrWhiteSpace(AirDateStr)
        ? null
        : TimeUtils.ParseString(AirDateStr, "yyyy-MM-dd");

    [JsonProperty("date")]
    public string? DateStr { get; set; }

    [JsonIgnore]
    public DateTimeOffset? Date => string.IsNullOrWhiteSpace(DateStr)
        ? null
        : TimeUtils.ParseString(DateStr, "yyyy-MM-dd");

    [JsonProperty("air_weekday")]
    public int? AirWeekday { get; set; }

    [JsonProperty("eps")]
    public int? Eps { get; set; }

    [JsonProperty("eps_count")]
    public int? EpsCount { get; set; }

    [JsonProperty("images")]
    public ImageLinks? Images { get; set; }

    [JsonProperty("rating")]
    public RatingInfo? Rating { get; set; }

    [JsonProperty("rank")]
    public int? Rank { get; set; }

    [JsonProperty("collection")]
    public CollectionInfo? Collection { get; set; }

    [JsonProperty("meta_tags")]
    public List<string>? MetaTagList { get; set; }
}
