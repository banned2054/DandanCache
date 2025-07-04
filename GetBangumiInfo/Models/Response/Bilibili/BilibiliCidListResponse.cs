using GetBangumiInfo.Models.Bilibili;
using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Response.Bilibili;

public class BilibiliCidListResponse
{
    [JsonProperty("code")]
    public int? StatusCode { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("result")]
    public BilibiliEpisodeListResult? Result { get; set; }
}
