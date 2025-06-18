using GetBangumiInfo.Database;
using GetBangumiInfo.Models.Anime;
using GetBangumiInfo.Utils;
using GetBangumiInfo.Utils.Api;
using Newtonsoft.Json;

namespace Test;

internal class Program
{
    private const           string   CachePath     = "bangumi-data-cache.json";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(1);

    private static async Task Main()
    {
        DotNetEnv.Env.Load();
        await TestBangumi2Bilibili();
    }

    private static async Task TestDanmakuUpdate()
    {
        var dandanAppId     = Environment.GetEnvironmentVariable("DandanAppId");
        var dandanAppSecret = Environment.GetEnvironmentVariable("DandanAppSecret");
        if (string.IsNullOrEmpty(dandanAppId) || string.IsNullOrEmpty(dandanAppSecret))
        {
            return;
        }

        var a = await DandanPlayUtils.SearchAnimeByName("海贼王");
        if (a == null) return;
        foreach (var id in a.Select(shortInfo => shortInfo.AnimeId))
        {
            var info = await DandanPlayUtils.GetFullAnimeInfo(id);
            if (!info!.BangumiUrl.EndsWith("/975")) continue;
            foreach (var episode in info.EpisodeList!)
            {
                Console.WriteLine($"{episode.EpisodeNumber} : {episode.EpisodeTitle}, {episode.AirDate}");
            }

            break;
        }
    }

    private static async Task TestBangumi2Bilibili()
    {
        var db = new MyDbContext();
        foreach (var notItem in db.MappingList.Where(e => e.BilibiliId == -1))
        {
            var id = await Bangumi2Bilibili(notItem.BangumiId);
        }
    }

    private static async Task<int> Bangumi2Bilibili(int subjectId)
    {
        var json         = await GetBangumiDataJson();
        var responseJson = JsonConvert.DeserializeObject<BangumiDataResponse>(json);
        var animeList    = responseJson!.Items;
        foreach (var bilibiliInfo in from anime in animeList!
                                     let bangumiInfo =
                                         anime.Sites!.FirstOrDefault(s => s.Site == "bangumi" &&
                                                                          s.Id   == subjectId.ToString())
                                     where bangumiInfo != null
                                     select anime.Sites!.FirstOrDefault(s => s.Site == "bilibili")
                                     into bilibiliInfo
                                     where bilibiliInfo != null
                                     select bilibiliInfo)
        {
            var flag = int.TryParse(bilibiliInfo.Id, out var result);
            return flag ? result : -1;
        }

        return -1;
    }


    public static List<int> GetTodaySubjectId()
    {
        var db            = new MyDbContext();
        var subjectIdList = db.EpisodeList.Select(e => e.SubjectId).Distinct();
        var result        = subjectIdList.ToList();
        foreach (var subjectId in subjectIdList)
        {
            var element = BangumiUtils.GetSubjectInfo(subjectId);
            if (element == null) continue;
            if (!element.Value.TryGetProperty("date", out var dateProp)) continue;
            var dateStr = dateProp.GetString();
            if (!DateTime.TryParse(dateStr, out var date)) continue;
            var weekday = ((int)date.DayOfWeek + 6) % 7 + 1; // 周一=1，周日=7
            Console.WriteLine($"Weekday: {weekday}");
        }

        return result;
    }

    private static async Task<string> GetBangumiDataJson()
    {
        if (!File.Exists(CachePath)) return await DownloadBangumiDataJson();
        var fileInfo = new FileInfo(CachePath);
        if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc >= CacheDuration) return await DownloadBangumiDataJson();
        try
        {
            var jsonStr      = await File.ReadAllTextAsync(CachePath);
            var responseJson = JsonConvert.DeserializeObject<BangumiDataResponse>(jsonStr);
            return jsonStr;
        }
        catch
        {
            return await DownloadBangumiDataJson();
        }
    }

    private static async Task<string> DownloadBangumiDataJson()
    {
        // 缓存不存在或过期，重新下载
        var response = await NetUtils.FetchAsync("https://unpkg.com/bangumi-data@0.3/dist/data.json",
                                                 null,
                                                 true);
        await File.WriteAllTextAsync(CachePath, response);
        return response;
    }
}
