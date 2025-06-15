using Newtonsoft.Json;

namespace DanmakuUpdate.Models.Dandan;

public class SearchAnimeBySeason
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
