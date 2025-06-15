using DanmakuUpdate.Models.Dandan;
using DanmakuUpdate.Models.Net;
using GetBangumiInfo.Utils;
using Newtonsoft.Json;

namespace DanmakuUpdate;

public class DandanApiClient
{
    private readonly Dictionary<string, string> _headers;

    public DandanApiClient(string appId, string appSecret)
    {
        _headers = new Dictionary<string, string>
        {
            { "accept", "application/json" },
            { "X-AppId", appId },
            { "X-AppSecret", appSecret }
        };
    }

    public async Task<string> GetDanmakuAsync(int episodeId)
    {
        var url = $"https://api.dandanplay.net/api/v2/comment/{episodeId}";
        try
        {
            var json = await NetUtils.FetchAsync(url, _headers);
            return json;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }

        return string.Empty;
    }

    public async Task<List<ShortAnimeInfo>?> SearchAnimeBySeason(int year, int month)
    {
        if (year < 1980 || month < 1 || month > 12)
        {
            return null;
        }

        var url = $"https://api.dandanplay.net/api/v2/bangumi/season/anime/{year}/{month}?filterAdultContent=true";
        try
        {
            var json = await NetUtils.FetchAsync(url, _headers);
            var list = JsonConvert.DeserializeObject<AnimeList>(json);
            return list!.ShortInfoList;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }

        return null;
    }

    public async Task<List<ShortAnimeInfo>?> GetRecentAnime()
    {
        const string url = "https://api.dandanplay.net/api/v2/bangumi/shin?filterAdultContent=true";
        try
        {
            var json = await NetUtils.FetchAsync(url, _headers);
            var list = JsonConvert.DeserializeObject<AnimeList>(json);
            return list!.ShortInfoList;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }

        return null;
    }

    public async Task<FullAnimeInfo?> GetFullAnimeInfo(int id)
    {
        var url = $"https://api.dandanplay.net/api/v2/bangumi/{id}";
        try
        {
            var json     = await NetUtils.FetchAsync(url, _headers);
            var response = JsonConvert.DeserializeObject<ResponseAnimeInfo>(json);
            return response!.AnimeInfo;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }

        return null;
    }
}
