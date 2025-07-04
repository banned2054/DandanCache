using GetBangumiInfo.Utils;
using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Bangumi;

public class BangumiEpisode
{
    [JsonProperty("id")]
    public int? Id { get; set; }

    [JsonProperty("type")]
    public int? Type { get; set; }

    [JsonProperty("sort")]
    public int? Sort { get; set; }

    [JsonProperty("ep")]
    public int? Episode { get; set; }

    [JsonProperty("airdate")]
    public string? AirDateStr { get; set; }

    [JsonIgnore]
    public DateTimeOffset? AirDate => AirDateStr == null
        ? null
        : DateUtils.ParseBeijingTime(AirDateStr, "yyyy-MM-dd");

    [JsonProperty("subject_id")]
    public int? SubjectId { get; set; }
}
