using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Dandan;

public class DanmakuEntity
{
    [JsonProperty("cid")]
    public long Cid { get; set; }

    [JsonProperty("p")]
    public string Param { get; set; } = string.Empty;

    [JsonProperty("m")]
    public string Message { get; set; } = string.Empty;
}
