using GetBangumiInfo.Models.Bilibili;
using GetBangumiInfo.Models.Response.Bilibili;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GetBangumiInfo.Utils.Api;

public class BilibiliUtils
{
    public static readonly string BaseUrl = "https://api.bilibili.com";

    private static async Task<T> Fetch<T>(string                      path,
                                          Dictionary<string, string>? header = null)
    {
        header ??= new Dictionary<string, string>
        {
            ["User-Agent"] =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            ["Referer"] = "https://www.bilibili.com",
        };
        var response = await NetUtils.FetchAsync<T>($"{BaseUrl}/{path}", header);
        return response;
    }

    public static async Task<int> GetSeasonIdByMediaId(int mediaId)
    {
        var header = new Dictionary<string, string>
        {
            ["User-Agent"] =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            ["Referer"]         = "https://www.bilibili.com",
            ["Accept"]          = "application/json",
            ["Accept-Language"] = "zh-CN,zh;q=0.9"
        };
        var response = await Fetch<string>($"pgc/review/user?media_id={mediaId}", header);
        var seasonId = -1;
        try
        {
            var obj = JObject.Parse(response);

            // 判断是否包含 "code" 且其值为 0
            if (obj.TryGetValue("code", out var codeToken) &&
                codeToken.Type == JTokenType.Integer       &&
                (int)codeToken == 0)
            {
                // 判断是否包含 "season_id"
                if (obj.TryGetValue("season_id", out var seasonToken) &&
                    seasonToken.Type == JTokenType.Integer)
                {
                    seasonId = (int)seasonToken;
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return seasonId;
    }

    public static async Task<List<BilibiliEpisode>?> GetEpisodeListBySeasonIdAsync(long seasonId)
    {
        var response = await Fetch<string>($"pgc/view/web/ep/list?season_id={seasonId}");
        Console.WriteLine(response);
        var result = JsonConvert.DeserializeObject<BilibiliCidListResponse>(response);
        if (result?.Result?.Episodes == null || result.Result.Episodes.Count == 0) return null;
        return result.Result.Episodes
                     .OrderBy(e => e.PubTimeUnix)
                     .Where(e => string.IsNullOrWhiteSpace(e.NumberStr) || decimal.TryParse(e.NumberStr, out _))
                     .ToList();
    }
}
