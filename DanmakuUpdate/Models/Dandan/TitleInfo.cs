using Newtonsoft.Json;

namespace DanmakuUpdate.Models.Dandan;

public class TitleInfo
{
    [JsonProperty("language")]
    public string Language { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
}
