using Newtonsoft.Json.Linq;

namespace GetBangumiInfo.Utils.Api;

public class BilibiliUtils
{
    public static async Task<int> GetSeasonIdByMediaId(string mediaId)
    {
        var url = $"https://api.bilibili.com/pgc/review/user?media_id={mediaId}";
        var header = new Dictionary<string, string>
        {
            ["User-Agent"] =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            ["Referer"]         = "https://www.bilibili.com",
            ["Accept"]          = "application/json",
            ["Accept-Language"] = "zh-CN,zh;q=0.9"
        };
        var response = await NetUtils.FetchAsync<string>(url, header);
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
}
