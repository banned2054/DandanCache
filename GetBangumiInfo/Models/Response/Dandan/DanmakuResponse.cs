using GetBangumiInfo.Models.Dandan;
using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Response.Dandan;

public class DanmakuResponse
{
    [JsonProperty("comments")]
    public List<DanmakuEntity>? DanmakuList { get; set; }
}
