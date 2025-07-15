using GetBangumiInfo.Database;
using GetBangumiInfo.Models.Database;
using GetBangumiInfo.Utils;
using GetBangumiInfo.Utils.Api;
using Microsoft.EntityFrameworkCore;

namespace GetBangumiInfo.Controllers;

public class UpdateController
{
    private const int MaxDataBaseBatchSize = 5;

    private static int _counter;

    public static async Task UpdateBangumi(MyDbContext db)
    {
        Console.WriteLine("🚀 Start Updating danmaku...");
        Console.WriteLine("============================");

        var tempHotList  = new List<Episode>();
        var tempColdList = new List<EpisodeCold>();

        Console.WriteLine("📅 Fetching bangumi calendar...");
        var bangumiList = await BangumiUtils.GetCalendar();
        Console.WriteLine($"📅 Got {bangumiList.Count} items from calendar.");

        Console.WriteLine("📊 Loading mapping list from DB...");
        var mappingList = db.MappingList.ToList();
        Console.WriteLine($"📊 Loaded {mappingList.Count} mapping entries.");

        _counter = 0;

        foreach (var (bangumiId, name) in bangumiList
                    .Select(e => (e.Id!.Value, string.IsNullOrEmpty(e.NameCn) ? e.Name : e.NameCn)))
        {
            Console.WriteLine($"\n🎬 Processing {name} (ID: {bangumiId})");

            var mapping = mappingList.FirstOrDefault(e => e.BangumiId == bangumiId);
            if (mapping == null)
            {
                Console.WriteLine("⚠️ Mapping not found, skipping.");
                continue;
            }

            if (!mapping.IsJapaneseAnime!.Value)
            {
                Console.WriteLine("🔕 Not Japanese anime, skipping.");
                continue;
            }

            var        bilibiliId      = mapping.BilibiliId;
            List<int>? bilibiliHotList = null;
            if (bilibiliId != -1)
            {
                Console.WriteLine("📡 Fetching bilibili media ID...");
                var mediaId = await BilibiliUtils.GetSeasonIdByMediaId(bilibiliId);
                if (mediaId != -1)
                {
                    Console.WriteLine("🎯 Got bilibili season ID, fetching episodes...");
                    var bilibiliEpisodeList = await BilibiliUtils.GetEpisodeListBySeasonIdAsync(mediaId);
                    bilibiliHotList = bilibiliEpisodeList!
                                     .Where(e => TimeUtils.IsWithinThreeDays(e.PubDate))
                                     .Select(e => e.Number!.Value)
                                     .ToList();
                    Console.WriteLine($"🔥 Found {bilibiliHotList.Count} recent bilibili episodes.");
                }
                else
                {
                    Console.WriteLine("⚠️ No bilibili media found.");
                }
            }

            Console.WriteLine("📥 Fetching DandanPlay info...");
            var dandanId = mapping.DandanId;
            var info     = await DandanPlayUtils.GetFullAnimeInfo(dandanId);
            if (info == null)
            {
                Console.WriteLine("⚠️ DandanPlay info not found, skipping.");
                continue;
            }

            var episodeList = info.EpisodeList!
                                  .Where(e => int.TryParse(e.EpisodeNumber, out _))
                                  .Where(e => e.AirDate != null)
                                  .OrderBy(e => e.AirDate)
                                  .ToList();

            Console.WriteLine($"🎞 Total episodes to check: {episodeList.Count}");

            for (var i = 0; i < episodeList.Count; i++)
            {
                var episode = episodeList[i];

                var isHot = (bilibiliHotList != null && bilibiliHotList.Contains(i + 1)) ||
                            TimeUtils.IsWithinThreeDays(episode.AirDate!.Value);

                if (isHot)
                {
                    if (tempHotList.All(e => e.Id != episode.EpisodeId))
                    {
                        tempHotList.Add(new Episode
                        {
                            Id         = episode.EpisodeId,
                            EpisodeNum = i + 1,
                            SubjectId  = bangumiId
                        });
                    }
                }
                else if (TimeUtils.IsWithinThreeMonths(episode.AirDate!.Value))
                {
                    if (tempColdList.All(e => e.Id != episode.EpisodeId))
                    {
                        tempColdList.Add(new EpisodeCold
                        {
                            Id         = episode.EpisodeId,
                            EpisodeNum = i + 1,
                            SubjectId  = bangumiId
                        });
                    }
                }
            }
        }

        Console.WriteLine("\n🧊 Loading existing hot/cold lists from DB...");
        var dbHotList  = db.EpisodeList.ToList();
        var dbColdList = db.EpisodeListCold.ToList();

        var dbHotDict    = dbHotList.ToDictionary(e => e.Id);
        var dbColdDict   = dbColdList.ToDictionary(e => e.Id);
        var tempHotDict  = tempHotList.ToDictionary(e => e.Id);
        var tempColdDict = tempColdList.ToDictionary(e => e.Id);

        // --- 热表处理 ---
        Console.WriteLine("🧹 Cleaning up hot episodes...");
        var removedHot = 0;
        foreach (var dbItem in dbHotList.Where(dbItem => !tempHotDict.ContainsKey(dbItem.Id)))
        {
            db.EpisodeList.Remove(dbItem);
            removedHot++;
            await AddBatch(db);
        }

        Console.WriteLine($"🗑 Removed {removedHot} hot episodes not in temp list.");

        var addedHot = 0;
        Console.WriteLine("➕ Adding new hot episodes...");
        foreach (var tempItem in tempHotList.Where(tempItem => !dbHotDict.ContainsKey(tempItem.Id)))
        {
            await db.EpisodeList.AddAsync(tempItem);
            addedHot++;
            await AddBatch(db);
        }

        Console.WriteLine($"✅ Added {addedHot} new hot episodes.");

        Console.WriteLine("🧹 Cleaning up cold episodes...");
        var removedCold = 0;
        foreach (var dbItem in dbColdList.Where(dbItem => !tempColdDict.ContainsKey(dbItem.Id)))
        {
            db.EpisodeListCold.Remove(dbItem);
            removedCold++;
            await AddBatch(db);
        }

        Console.WriteLine($"🗑 Removed {removedCold} cold episodes not in temp list.");

        var addedCold = 0;
        Console.WriteLine("➕ Adding new cold episodes...");
        foreach (var tempItem in tempColdList.Where(tempItem => !dbColdDict.ContainsKey(tempItem.Id)))
        {
            await db.EpisodeListCold.AddAsync(tempItem);
            addedCold++;
            await AddBatch(db);
        }

        Console.WriteLine($"✅ Added {addedCold} new cold episodes.");

        // 最后一次保存
        if (_counter > 0)
        {
            Console.WriteLine("💾 Saving remaining batched changes...");
            await SaveChangesWithRetryAsync(db);
            _counter = 0;
        }

        Console.WriteLine("🎉 UpdateBangumi completed successfully!");
    }

    public static async Task UpdateMapping(MyDbContext db)
    {
        var shortInfoList = await DandanPlayUtils.GetRecentAnime();
        if (shortInfoList == null || shortInfoList.Count == 0)
        {
            Console.WriteLine("Recent dandan data is null");
            return;
        }

        // 🌟 1. 一次性加载 MappingList 数据，提高查找效率
        var allMappings       = await db.MappingList.ToListAsync();
        var existingDandanIds = allMappings.Select(m => m.DandanId).ToHashSet();

        // 🌟 2. 添加或更新 DandanId 与 BangumiId
        foreach (var shortInfo in shortInfoList.Where(shortInfo => !existingDandanIds.Contains(shortInfo.Id)))
        {
            var fullInfo = await DandanPlayUtils.GetFullAnimeInfo(shortInfo.Id);
            if (fullInfo == null) continue;

            var bangumiId = fullInfo.BangumiId;
            if (bangumiId is null or -1) continue;

            var nowItem = allMappings.FirstOrDefault(e => e.BangumiId == bangumiId);
            if (nowItem == null)
            {
                nowItem = new Mapping
                {
                    BangumiId  = bangumiId.Value,
                    DandanId   = shortInfo.Id,
                    BilibiliId = -1
                };
                db.MappingList.Add(nowItem);
                allMappings.Add(nowItem); // 保持本地缓存一致
            }
            else
            {
                nowItem.DandanId = shortInfo.Id;
            }

            await AddBatch(db);
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
