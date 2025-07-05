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
            if (string.IsNullOrEmpty(dandanAppId)) Console.WriteLine("Dandan app id is null");
            if (string.IsNullOrEmpty(dandanAppSecret)) Console.WriteLine("Dandan app secret is null");
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

        //dandan id不在mapping表里
        foreach (var id in shortInfoList
                          .Select(shortInfo => shortInfo.AnimeId)
                          .Where(id => !db.MappingList
                                          .Select(e => e.DandanId)
                                          .Contains(id)
                                ))
        {
            //获取bangumi subject id
            var fullInfo = await DandanPlayUtils.GetFullAnimeInfo(id);
            if (fullInfo == null) continue;
            var match = BangumiRegex.Match(fullInfo.BangumiUrl);
            if (!match.Success) continue;
            var bangumiId = int.Parse(match.Groups["id"].Value);
            if (bangumiId < 0) continue;

            var nowItem = db.MappingList.FirstOrDefault(e => e.BangumiId == bangumiId);
            //bangumi subject id在不在表里
            if (nowItem == default)
            {
                nowItem = new Mapping() { BangumiId = bangumiId, DandanId = id, BilibiliId = -1 };
                db.MappingList.Add(nowItem);
            }
            else
            {
                nowItem.DandanId = id;
            }
        }

        foreach (var notItem in db.MappingList.Where(e => e.BilibiliId == -1))
        {
            var id = await Bangumi2BilibiliUtils.Parser(notItem.BangumiId);
            if (id == -1) continue;
            notItem.BilibiliId = id;
        }

        foreach (var mapping in db.MappingList.Where(e => e.AirDate == null || e.IsJapaneseAnime == null))
        {
            var info = BangumiUtils.GetSubjectInfo(mapping.BangumiId);
            mapping.AirDate         = info?.Date!;
            mapping.IsJapaneseAnime = info?.MetaTagList?.Contains("日本");
        }

        await db.SaveChangesAsync();
    }
}
