using GetBangumiInfo.Database;
using GetBangumiInfo.Models.Database;
using GetBangumiInfo.Utils;
using GetBangumiInfo.Utils.Api;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GetBangumiInfo.Controllers;

public class UpdateController
{
    public static readonly Regex BangumiRegex         = new(@"subject/(?<id>\d+)");
    private const          int   MaxDataBaseBatchSize = 5;

    private static int _counter;

    public static async Task UpdateBangumi()
    {
        Console.WriteLine("Updating danmaku...");
        Console.WriteLine("==================");
        // 初始化数据库
        await using var db = new MyDbContext();

        var tempHotList  = new List<Episode>();
        var tempColdList = new List<EpisodeCold>();
        Console.WriteLine("Get bangumi calender...");
        Console.WriteLine("==================");
        var bangumiList = await BangumiUtils.GetCalendar();
        var mappingList = db.MappingList.ToList();
        _counter = 0;
        foreach (var (bangumiId, name)in bangumiList
                    .Select(e => (e.Id!.Value, e.NameCn == null ? e.NameCn : e.Name)))
        {
            Console.WriteLine($"\nUpdate {name}");
            var mapping = mappingList.FirstOrDefault(e => e.BangumiId == bangumiId);
            if (mapping == default) continue;
            if (!mapping.IsJapaneseAnime!.Value) continue;

            var        bilibiliId      = mapping.BilibiliId;
            List<int>? bilibiliHotList = null;
            if (bilibiliId != -1)
            {
                var mediaId = await BilibiliUtils.GetSeasonIdByMediaId(bilibiliId);
                if (mediaId != -1)
                {
                    Console.WriteLine("Bilibili data find");
                    var bilibiliEpisodeList = await BilibiliUtils.GetEpisodeListBySeasonIdAsync(mediaId);
                    bilibiliHotList = bilibiliEpisodeList!
                                     .Where(e => TimeUtils.IsWithinThreeDays(e.PubDate))
                                     .Select(e => e.Number!.Value).ToList();
                }
            }

            Console.WriteLine("Get dandan full info");
            var dandanId = mapping.DandanId;
            var info     = await DandanPlayUtils.GetFullAnimeInfo(dandanId);
            if (info == null) continue;

            var episodeList = info.EpisodeList!
                                  .Where(e => int.TryParse(e.EpisodeNumber, out _))
                                  .Where(e => e.AirDate != null)
                                  .OrderBy(e => e.AirDate)
                                  .ToList();

            Console.WriteLine($"Download danmaku, episode count: {episodeList.Count}");
            for (var i = 0; i < episodeList.Count; i++)
            {
                var episode = episodeList[i];

                // bilibili中是最新，或者弹弹play中是最新
                if ((bilibiliHotList != null && bilibiliHotList!.Contains(i + 1)) ||
                    TimeUtils.IsWithinThreeDays(episode.AirDate!.Value))
                {
                    if (tempHotList.Any(e => e.Id == episode.EpisodeId)) continue;
                    tempHotList.Add(new Episode
                    {
                        Id         = episode.EpisodeId,
                        EpisodeNum = i + 1,
                        SubjectId  = bangumiId
                    });
                    continue;
                }

                if (tempColdList.Any(e => e.Id == episode.EpisodeId)) continue;
                if (!TimeUtils.IsWithinThreeMonths(episode.AirDate!.Value)) continue;
                tempColdList.Add(new EpisodeCold
                {
                    Id         = episode.EpisodeId,
                    EpisodeNum = i + 1,
                    SubjectId  = bangumiId
                });
            }
        }

        var dbHotList  = db.EpisodeList.ToList();
        var dbColdList = db.EpisodeListCold.ToList();

        var dbHotDict    = dbHotList.ToDictionary(e => e.Id);
        var dbColdDict   = dbColdList.ToDictionary(e => e.Id);
        var tempHotDict  = tempHotList.ToDictionary(e => e.Id);
        var tempColdDict = tempColdList.ToDictionary(e => e.Id);

        // --- 热表处理 ---

        // 删除 db 有但 temp 没有的
        Console.WriteLine("Remove not hot episode...");
        foreach (var dbItem in dbHotList.Where(dbItem => !tempHotDict.ContainsKey(dbItem.Id)))
        {
            db.EpisodeList.Remove(dbItem);
            await AddBatch(db);
        }

        // 添加 temp 有但 db 没有的
        Console.WriteLine("Add new hot episode...");
        foreach (var tempItem in tempHotList.Where(tempItem => !dbHotDict.ContainsKey(tempItem.Id)))
        {
            await db.EpisodeList.AddAsync(tempItem);
            await AddBatch(db);
        }
        
        Console.WriteLine("Remove not cold episode...");
        foreach (var dbItem in dbColdList.Where(dbItem => !tempColdDict.ContainsKey(dbItem.Id)))
        {
            db.EpisodeListCold.Remove(dbItem);
            await AddBatch(db);
        }
        
        Console.WriteLine("Add new cold episode...");
        foreach (var tempItem in tempColdList.Where(tempItem => !dbColdDict.ContainsKey(tempItem.Id)))
        {
            await db.EpisodeListCold.AddAsync(tempItem);
            await AddBatch(db);
        }

        // 最后一次保存
        if (_counter > 0)
        {
            await SaveChangesWithRetryAsync(db);
            _counter = 0;
        }

        await db.DisposeAsync();
    }

    public static async Task UpdateByDandan()
    {
        var shortInfoList = await DandanPlayUtils.GetRecentAnime();
        if (shortInfoList == null || shortInfoList.Count == 0)
        {
            Console.WriteLine("Recent dandan data is null");
            return;
        }

        await using var db = new MyDbContext();

        // 🌟 1. 一次性加载 MappingList 数据，提高查找效率
        var allMappings       = await db.MappingList.ToListAsync();
        var existingDandanIds = allMappings.Select(m => m.DandanId).ToHashSet();

        // 🌟 2. 添加或更新 DandanId 与 BangumiId
        foreach (var shortInfo in shortInfoList.Where(shortInfo => !existingDandanIds.Contains(shortInfo.Id)))
        {
            var fullInfo = await DandanPlayUtils.GetFullAnimeInfo(shortInfo.Id);
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
                    DandanId   = shortInfo.Id,
                    BilibiliId = -1
                };
                db.MappingList.Add(nowItem);
                allMappings.Add(nowItem); // 保持本地缓存一致
                await AddBatch(db);
            }
            else
            {
                nowItem.DandanId = shortInfo.Id;
                await AddBatch(db);
            }
        }

        await SaveChangesWithRetryAsync(db);
        _counter = 0;

        // 🌟 3. 解析 BilibiliId
        foreach (var item in allMappings.Where(e => e.BilibiliId == -1))
        {
            var bilibiliId = await Bangumi2BilibiliUtils.Parser(item.BangumiId);
            if (bilibiliId == -1) continue;
            item.BilibiliId = bilibiliId;
            await AddBatch(db);
        }

        await SaveChangesWithRetryAsync(db);
        _counter = 0;

        // 🌟 4. 填补 AirDate 和 IsJapaneseAnime
        foreach (var item in allMappings.Where(e => e.AirDate == null || e.IsJapaneseAnime == null))
        {
            var info = await BangumiUtils.GetSubjectInfo(item.BangumiId);
            item.AirDate         = info?.Date!;
            item.IsJapaneseAnime = info?.MetaTagList?.Contains("日本");
            await AddBatch(db);
        }

        // ✅ 最后统一保存
        await SaveChangesWithRetryAsync(db);

        await db.DisposeAsync();
    }

    private static async Task AddBatch(DbContext db)
    {
        _counter++;
        if (_counter == MaxDataBaseBatchSize)
        {
            Console.WriteLine("Batch is max, save.");
            await SaveChangesWithRetryAsync(db);
            _counter = 0;
        }
    }

    private static async Task SaveChangesWithRetryAsync(DbContext db, int maxRetries = 3, int delayMs = 1000)
    {
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await db.SaveChangesAsync();
                return;
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                Console.WriteLine($"[Attempt {attempt}] SaveChangesAsync failed: {ex.Message}");

                if (attempt == maxRetries)
                {
                    Console.WriteLine("Reached max retries. Rethrowing.");
                    throw;
                }

                await Task.Delay(delayMs);
            }
        }
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is DbUpdateException { InnerException: Npgsql.NpgsqlException exception } &&
               (exception.Message.Contains("Timeout") || exception.Message.Contains("Exception while reading"));
    }
}
