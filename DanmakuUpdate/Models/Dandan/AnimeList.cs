using Newtonsoft.Json;

namespace DanmakuUpdate.Models.Dandan;

public class AnimeList
{
    [JsonProperty("errorCode")]
    public int ErrorCode;

    [JsonProperty("errorMessage")]
    public string ErrorMessage = string.Empty;

    [JsonProperty("success")]
    public bool IsSuccess;

    [JsonProperty("hasMore")]
    public bool IsNotEnd;

    [JsonProperty("bangumiList")]
    public List<ShortAnimeInfo>? ShortInfoList;
}
