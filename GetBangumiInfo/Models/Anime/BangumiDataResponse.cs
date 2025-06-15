namespace GetBangumiInfo.Models.Anime;

public class BangumiDataResponse
{
    public Dictionary<string, SiteMeta>? SiteMeta { get; set; }
    public List<AnimeItem>?              Items    { get; set; }
}
