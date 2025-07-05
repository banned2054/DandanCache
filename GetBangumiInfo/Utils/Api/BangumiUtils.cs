using GetBangumiInfo.Models.Bangumi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GetBangumiInfo.Utils.Api;

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
        var response         = await NetUtils.FetchAsync<string>(url, headers);
        var todayWeekday     = TimeUtils.GetNowWeekday() + 1;
        var yesterdayWeekday = (todayWeekday + 6) % 7;
        var days             = JsonConvert.DeserializeObject<List<BangumiDay>>(response);
        foreach (var day in days!)
        {
            if (day.Weekday!.Id == todayWeekday || day.Weekday.Id == yesterdayWeekday)
            {
                hotIdList.AddRange(from item in day.Items!
                                   where !(item.Rating!.Score! <= 5)
                                   select item.Id!.Value);
            }
            else
            {
                coldIdList.AddRange(from item in day.Items!
                                    where !(item.Rating!.Score! <= 5)
                                    select item.Id!.Value);
            }
        }

        return (hotIdList, coldIdList);
    }

    public static async Task DownloadDumpFile()
    {
        var latestJson =
            await
                NetUtils.FetchAsync<string>("https://raw.githubusercontent.com/bangumi/Archive/master/aux/latest.json",
                                            null, true);
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

        await NetUtils.DownloadAsync(dumpUrl, "dump.zip", enableProxy : true);
    }

    public static Task UnzipDumpFile()
    {
        FileUtils.UnzipFile("dump.zip", AppContext.BaseDirectory);
        return Task.CompletedTask;
    }

    public static BangumiItem? GetSubjectInfo(int subjectId)
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
                return JsonConvert.DeserializeObject<BangumiItem>(line);
            }
            catch
            {
                // skip invalid lines
            }
        }

        return null;
    }


    public static List<BangumiEpisode> GetSubjectEpisodeList(int subjectId)
    {
        const int    limit    = 20;
        const string fileName = "episode.jsonlines";

        var results = new List<BangumiEpisode>();
        var path    = Path.Combine(AppContext.BaseDirectory, fileName);

        using var reader = new StreamReader(path);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line == null) continue;

            if (!StringUtils.QuickFilter(line, subjectId)) continue;

            try
            {
                var data = JsonConvert.DeserializeObject<BangumiEpisode>(line);
                results.Add(data!);
            }
            catch
            {
                // skip invalid lines
            }
        }

        return results
              .OrderBy(e => e.Sort)
              .Take(limit)
              .ToList();
    }
}
