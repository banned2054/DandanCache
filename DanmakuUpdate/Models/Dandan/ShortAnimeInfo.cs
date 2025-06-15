using Newtonsoft.Json;

namespace DanmakuUpdate.Models.Dandan;

public class ShortAnimeInfo
{
    [JsonProperty("animeId")]
    public int AnimeId;

    [JsonProperty("bangumiId")]
    public string BangumiId = string.Empty;

    [JsonProperty("animeTitle")]
    public string AnimeTitle = string.Empty;

    [JsonProperty("imageUrl")]
    public string ImageUrl = string.Empty;

    [JsonProperty("searchKeyword")]
    public string SearchKeyword = string.Empty;

    [JsonProperty("isOnAir")]
    public bool IsOnAir;

    [JsonProperty("airDay")]
    public int AirDay;

    [JsonProperty("isFavorited")]
    public bool IsFavorite;

    [JsonProperty("isRestricted")]
    public bool IsRestricted;

    [JsonProperty("rating")]
    public float Rating;
}
