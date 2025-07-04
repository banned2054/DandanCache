using System.Globalization;
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
    public DateTime? AirDate => AirDateStr == null
        ? null
        : DateTime.SpecifyKind(
                               DateTime.ParseExact(AirDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                               DateTimeKind.Unspecified
                              ).AddHours(8); // 转为东八区时间（假设原始是本地零点）

    [JsonProperty("subject_id")]
    public int? SubjectId { get; set; }
}
