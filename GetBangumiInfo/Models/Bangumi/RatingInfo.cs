using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Bangumi;

public class RatingInfo
{
    [JsonProperty("total")]
    public int Total { get; set; }

    [JsonProperty("score")]
    public float Score { get; set; }

    [JsonProperty("count")]
    public Dictionary<string, int> Count { get; set; }
}