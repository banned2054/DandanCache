using DanmakuUpdate.Models.Dandan;
using Newtonsoft.Json;

namespace DanmakuUpdate.Models.Net;

public class ResponseAnimeInfo
{
    [JsonProperty("errorCode")]
    public int ErrorCode { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonProperty("success")]
    public bool IsSuccess { get; set; }

    [JsonProperty("bangumi")]
    public FullAnimeInfo AnimeInfo { get; set; }
}
