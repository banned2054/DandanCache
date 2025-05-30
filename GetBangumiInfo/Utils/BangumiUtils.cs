using GetBangumiInfo.Models.Bangumi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace GetBangumiInfo.Utils;

public class BangumiUtils
{
    public static async Task<(List<int>, List<int>)> GetCalendar()
    {
        const string url = "https://api.bgm.tv/calendar";

        var hotIdList  = new List<int>();
        var coldIdList = new List<int>();
        var headers = new Dictionary<string, string>()
        {
            ["accept"] = "application/json"
        };
        var response         = await NetUtils.FetchAsync(url, headers);
        var todayWeekday     = DateUtils.GetBeijingWeekday() + 1;
        var yesterdayWeekday = (todayWeekday + 6) % 7;
        var days             = JsonConvert.DeserializeObject<List<BangumiDay>>(response);
        foreach (var day in days!)
        {
            if (day.Weekday!.Id == todayWeekday || day.Weekday.Id == yesterdayWeekday)
            {
                hotIdList.AddRange(from item in day.Items
                                   where item.Rating != null
                                   where !(item.Rating.Score <= 5)
                                   select item.Id);
            }
            else
            {
                coldIdList.AddRange(from item in day.Items
                                    where item.Rating != null
                                    where !(item.Rating.Score <= 5)
                                    select item.Id);
            }
        }

        return (hotIdList, coldIdList);
    }

    public static async Task DownloadDumpFile()
    {
        var latestJson =
            await NetUtils.FetchAsync("https://raw.githubusercontent.com/bangumi/Archive/master/aux/latest.json");
        var dict    = JObject.Parse(latestJson);
        var dumpUrl = dict["browser_download_url"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(dumpUrl))
        {
            return;
        }

        if (!NetUtils.IsValidUrl(dumpUrl))
        {
            return;
        }

        await NetUtils.DownloadAsync(dumpUrl, "dump.zip");
    }

    public static Task UnzipDumpFile()
    {
        FileUtils.UnzipFile("dump.zip", AppContext.BaseDirectory);
        return Task.CompletedTask;
    }

    public static JsonElement? GetSubjectInfo(int subjectId)
    {
        const string fileName = "subject.jsonlines";
        var          path     = Path.Combine(AppContext.BaseDirectory, fileName);

        using var reader = new StreamReader(path);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line == null) continue;

            if (!line.Contains($"\"id\":{subjectId},")) continue;

            try
            {
                using var doc  = JsonDocument.Parse(line);
                var       root = doc.RootElement;

                if (root.TryGetProperty("id", out var idProp) && idProp.GetInt32() == subjectId)
                {
                    // 返回深拷贝，避免文档 Dispose 后失效
                    return JsonDocument.Parse(root.GetRawText()).RootElement;
                }
            }
            catch
            {
                // skip invalid lines
            }
        }

        return null;
    }


    public static IEnumerable<JsonElement> GetSubjectEpisodeList(int subjectId)
    {
        const int    limit    = 20;
        const string fileName = "episode.jsonlines";

        var results = new List<JsonElement>();
        var path    = Path.Combine(AppContext.BaseDirectory, fileName);

        using var reader = new StreamReader(path);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line == null) continue;

            if (!StringUtils.QuickFilter(line, subjectId)) continue;

            try
            {
                using var doc  = JsonDocument.Parse(line);
                var       root = doc.RootElement;

                if (StringUtils.IsValid(root, subjectId))
                {
                    // 深拷贝避免 JsonDocument Dispose 后失效
                    results.Add(JsonDocument.Parse(root.GetRawText()).RootElement);
                }
            }
            catch
            {
                // skip invalid lines
            }
        }

        return results
              .Where(e => e.TryGetProperty("sort", out var sortProp) && sortProp.TryGetSingle(out _))
              .OrderBy(e => e.GetProperty("sort").GetSingle())
              .Take(limit)
              .ToList();
    }
}
