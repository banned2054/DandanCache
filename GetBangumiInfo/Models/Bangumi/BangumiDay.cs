using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Bangumi;

public class BangumiDay
{
    [JsonProperty("weekday")]
    public WeekdayInfo Weekday { get; set; }

    [JsonProperty("items")]
    public List<BangumiItem> Items { get; set; }
}