using Newtonsoft.Json;

namespace GetBangumiInfo.Models.Dandan;

public class ShortAnimeInfo
{
    [JsonProperty("animeId")]
    public int Id { get; set; }

    [JsonProperty("animeTitle")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonProperty("searchKeyword")]
    public string SearchKeyword { get; set; } = string.Empty;

    [JsonProperty("rating")]
    public float Rating { get; set; }
}
