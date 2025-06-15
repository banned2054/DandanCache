namespace GetBangumiInfo.Models.Anime;

public class SiteInfo
{
    public string  Site      { get; set; } = string.Empty;
    public string  Id        { get; set; } = string.Empty;
    public string? Begin     { get; set; }
    public string? Broadcast { get; set; }
    public string? Comment   { get; set; } // 例如“首播两集连播”这一项
}
