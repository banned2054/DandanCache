using Newtonsoft.Json;

namespace DanmakuUpdate.Models.Dandan;

public class ShortAnimeInfo
{
    [JsonProperty("animeId")]
    public int AnimeId { get; set; }

    [JsonProperty("bangumiId")]
    public string BangumiId { get; set; } = string.Empty;

    [JsonProperty("animeTitle")]
    public string AnimeTitle { get; set; } = string.Empty;

    [JsonProperty("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonProperty("searchKeyword")]
    public string SearchKeyword { get; set; } = string.Empty;

    [JsonProperty("isOnAir")]
    public bool IsOnAir { get; set; }

    [JsonProperty("airDay")]
    public int AirDay { get; set; }

    [JsonProperty("isFavorited")]
    public bool IsFavorite { get; set; }

    [JsonProperty("isRestricted")]
    public bool IsRestricted { get; set; }

    [JsonProperty("rating")]
    public float Rating { get; set; }
}
