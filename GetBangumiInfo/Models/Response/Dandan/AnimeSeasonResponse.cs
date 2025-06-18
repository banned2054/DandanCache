using GetBangumiInfo.Models.Dandan;
using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Response.Dandan;

public class AnimeSeasonResponse
{
    [JsonProperty("errorCode")]
    public int ErrorCode { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonProperty("success")]
    public bool? IsSuccess { get; set; }

    [JsonProperty("hasMore")]
    public bool IsNotEnd { get; set; }

    [JsonProperty("bangumiList")]
    public List<ShortAnimeInfo>? ShortInfoList { get; set; }
}
