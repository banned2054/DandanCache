namespace GetBangumiInfo.Models.Anime;

public class AnimeItem
{
    public string?                           Title          { get; set; }
    public Dictionary<string, List<string>>? TitleTranslate { get; set; }
    public string?                           Type           { get; set; }
    public string?                           Lang           { get; set; }
    public string?                           OfficialSite   { get; set; }
    public string?                           Begin          { get; set; }
    public string?                           Broadcast      { get; set; }
    public string?                           End            { get; set; }
    public string?                           Comment        { get; set; }
    public List<SiteInfo>?                   Sites          { get; set; }
}
