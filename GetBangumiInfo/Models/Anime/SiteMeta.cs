namespace GetBangumiInfo.Models.Anime;

public class SiteMeta
{
    public string        Title       { get; set; } = string.Empty;
    public string        UrlTemplate { get; set; } = string.Empty;
    public string        Type        { get; set; } = string.Empty;
    public List<string>? Regions     { get; set; }
}
