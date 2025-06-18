using GetBangumiInfo.Models.Dandan;
using GetBangumiInfo.Models.Response.Dandan;
using Newtonsoft.Json;

namespace GetBangumiInfo.Utils.Api;

public class DandanPlayUtils
{
    private static Dictionary<string, string> BuildHeaders()
    {
        var appId     = Environment.GetEnvironmentVariable("DandanAppId");
        var appSecret = Environment.GetEnvironmentVariable("DandanAppSecret");
        return new Dictionary<string, string>
        {
            { "accept", "application/json" },
            { "X-AppId", appId },
            { "X-AppSecret", appSecret }
        };
    }

    public static async Task<string> GetDanmakuAsync(int episodeId)
    {
        var url = $"https://api.dandanplay.net/api/v2/comment/{episodeId}";
        try
        {
            var json = await NetUtils.FetchAsync(url, BuildHeaders());
            return json;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }

        return string.Empty;
    }

    public static async Task<List<ShortAnimeInfo>?> SearchAnimeBySeason(int year, int month)
    {
        if (year < 1980 || month < 1 || month > 12)
        {
            return null;
        }

        var url = $"https://api.dandanplay.net/api/v2/bangumi/season/anime/{year}/{month}?filterAdultContent=true";
        try
        {
            var json = await NetUtils.FetchAsync(url, BuildHeaders());
            var list = JsonConvert.DeserializeObject<AnimeSeasonResponse>(json);
            return list!.ShortInfoList;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }

        return null;
    }

    public static async Task<List<ShortAnimeInfo>?> GetRecentAnime()
    {
        const string url = "https://api.dandanplay.net/api/v2/bangumi/shin?filterAdultContent=true";
        try
        {
            var json = await NetUtils.FetchAsync(url, BuildHeaders());
            var list = JsonConvert.DeserializeObject<AnimeSeasonResponse>(json);
            return list!.ShortInfoList;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }

        return null;
    }

    public static async Task<FullAnimeInfo?> GetFullAnimeInfo(int id)
    {
        var url = $"https://api.dandanplay.net/api/v2/bangumi/{id}";
        try
        {
            var json     = await NetUtils.FetchAsync(url, BuildHeaders());
            var response = JsonConvert.DeserializeObject<AnimeFullResponse>(json);
            return response!.AnimeInfo;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }

        return null;
    }

    public static async Task<List<ShortAnimeInfo>?> SearchAnimeByName(string keyword, string? type = null)
    {
        if (string.IsNullOrEmpty(keyword) || keyword.Length < 2) return null;
        var url = $"https://api.dandanplay.net/api/v2/search/anime?keyword={keyword}";
        if (!string.IsNullOrEmpty(type))
        {
            url = $"{url}&type={type}";
        }

        try
        {
            var json = await NetUtils.FetchAsync(url, BuildHeaders());
            var list = JsonConvert.DeserializeObject<AnimeSearchResponse>(json);
            return list!.ShortInfoList;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }

        return null;
    }
}
