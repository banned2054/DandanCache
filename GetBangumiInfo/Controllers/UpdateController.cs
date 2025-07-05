using GetBangumiInfo.Database;
using GetBangumiInfo.Models.Database;
using GetBangumiInfo.Utils.Api;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GetBangumiInfo.Controllers;

public class UpdateController
{
    public static readonly Regex BangumiRegex = new(@"subject/(?<id>\d+)");

    public static async Task UpdateBangumi()
    {
        // ① 准备离线数据
        await BangumiUtils.DownloadDumpFile();
        await BangumiUtils.UnzipDumpFile();

        // 初始化数据库
        await using var db = new MyDbContext();

        //// ② 取本周番剧 SubjectId 列表
        //var (hotSubjectIds, coldSubjectIds) = await BangumiUtils.GetCalendar();

        //await using var tx = await db.Database.BeginTransactionAsync();

        //// ---------- 1. 读取旧表 ----------
        //var oldHotList  = await db.EpisodeList.AsNoTracking().ToListAsync();
        //var oldColdList = await db.EpisodeListCold.AsNoTracking().ToListAsync();

        //var oldHotIds  = oldHotList.Select(e => e.Id).ToHashSet();
        //var oldColdIds = oldColdList.Select(e => e.Id).ToHashSet();

        //await tx.CommitAsync();
    }

    public static async Task UpdateByDandan()
    {
        Console.WriteLine("Updating dandan...");
        Console.WriteLine("==================");
        var dandanAppId     = Environment.GetEnvironmentVariable("DandanAppId");
        var dandanAppSecret = Environment.GetEnvironmentVariable("DandanAppSecret");

        if (string.IsNullOrEmpty(dandanAppId) || string.IsNullOrEmpty(dandanAppSecret))
        {
            Console.WriteLine("Missing Dandan App ID or Secret");
            return;
        }

        await BangumiUtils.DownloadDumpFile();
        await BangumiUtils.UnzipDumpFile();
        var shortInfoList = await DandanPlayUtils.GetRecentAnime();
        if (shortInfoList == null || shortInfoList.Count == 0)
        {
            Console.WriteLine("Recent dandan data is null");
            return;
        }

        await using var db = new MyDbContext();

        // 🌟 1. 一次性加载 MappingList 数据，提高查找效率
        var allMappings        = await db.MappingList.ToListAsync();
        var existingDandanIds  = allMappings.Select(m => m.DandanId).ToHashSet();

        // 🌟 2. 添加或更新 DandanId 与 BangumiId
        foreach (var shortInfo in shortInfoList.Where(shortInfo => !existingDandanIds.Contains(shortInfo.AnimeId)))
        {
            var fullInfo = await DandanPlayUtils.GetFullAnimeInfo(shortInfo.AnimeId);
            if (fullInfo == null) continue;

            var match = BangumiRegex.Match(fullInfo.BangumiUrl);
            if (!match.Success) continue;

            var bangumiId = int.Parse(match.Groups["id"].Value);
            if (bangumiId < 0) continue;

            var nowItem = allMappings.FirstOrDefault(e => e.BangumiId == bangumiId);
            if (nowItem == null)
            {
                nowItem = new Mapping
                {
                    BangumiId  = bangumiId,
                    DandanId   = shortInfo.AnimeId,
                    BilibiliId = -1
                };
                db.MappingList.Add(nowItem);
                allMappings.Add(nowItem); // 保持本地缓存一致
            }
            else
            {
                nowItem.DandanId = shortInfo.AnimeId;
            }
        }

        // 🌟 3. 解析 BilibiliId
        foreach (var item in allMappings.Where(e => e.BilibiliId == -1))
        {
            var bilibiliId = await Bangumi2BilibiliUtils.Parser(item.BangumiId);
            if (bilibiliId == -1) continue;
            item.BilibiliId = bilibiliId;
        }

        // 🌟 4. 填补 AirDate 和 IsJapaneseAnime
        foreach (var item in allMappings.Where(e => e.AirDate == null || e.IsJapaneseAnime == null))
        {
            var info = BangumiUtils.GetSubjectInfo(item.BangumiId);
            item.AirDate         = info?.Date!;
            item.IsJapaneseAnime = info?.MetaTagList?.Contains("日本");
        }

        // ✅ 最后统一保存
        await db.SaveChangesAsync();
    }
}
