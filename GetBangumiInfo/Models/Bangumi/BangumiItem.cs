using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Bangumi;

public class BangumiItem
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("type")]
    public int Type { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("name_cn")]
    public string NameCn { get; set; } = string.Empty;

    [JsonProperty("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonProperty("air_date")]
    public string AirDate { get; set; } = string.Empty;

    [JsonProperty("air_weekday")]
    public int AirWeekday { get; set; }

    [JsonProperty("eps")]
    public int Eps { get; set; }

    [JsonProperty("eps_count")]
    public int EpsCount { get; set; }

    [JsonProperty("images")]
    public ImageLinks Images { get; set; }

    [JsonProperty("rating")]
    public RatingInfo? Rating { get; set; }

    [JsonProperty("rank")]
    public int Rank { get; set; }

    [JsonProperty("collection")]
    public CollectionInfo Collection { get; set; }
}