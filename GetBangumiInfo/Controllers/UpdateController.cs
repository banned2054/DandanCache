using GetBangumiInfo.Database;
using GetBangumiInfo.Models.Dandan;
using GetBangumiInfo.Models.Database;
using GetBangumiInfo.Utils;
using GetBangumiInfo.Utils.Api;
using Microsoft.EntityFrameworkCore;

namespace GetBangumiInfo.Controllers;

public class UpdateController
{
    /// <summary>
    /// 获取 Bilibili 热门剧集列表（最近3天内发布的剧集编号）
    /// </summary>
    private static async Task<List<int>?> GetBilibiliHotListAsync(int bilibiliId)
    {
        if (bilibiliId == -1) return null;

        Console.WriteLine("📡 Fetching bilibili media ID...");
        var mediaId = await BilibiliUtils.GetSeasonIdByMediaId(bilibiliId);
        if (mediaId == -1)
        {
            Console.WriteLine("⚠️ No bilibili media found.");
            return null;
        }

        Console.WriteLine("🎯 Got bilibili season ID, fetching episodes...");
        var bilibiliEpisodeList = await BilibiliUtils.GetEpisodeListBySeasonIdAsync(mediaId);
        var bilibiliHotList = bilibiliEpisodeList!
                             .Where(e => TimeUtils.IsWithinThreeDays(e.PubDate))
                             .Select(e => e.Number!.Value)
                             .ToList();
        Console.WriteLine($"🔥 Found {bilibiliHotList.Count} recent bilibili episodes.");
        return bilibiliHotList;
    }

    /// <summary>
    /// 处理番剧剧集，分类到热/冷列表
    /// </summary>
    private static void ProcessEpisodes(
        List<DandanEpisode> episodeList,
        int                 bangumiId,
        List<int>?          bilibiliHotList,
        List<Episode>       tempHotList,
        List<EpisodeCold>   tempColdList)
    {
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

    /// <summary>
    /// 同步热表数据（移除不存在的数据，添加新数据）
    /// </summary>
    private static async Task SyncHotListAsync(
        MyDbContext   db,
        List<Episode> tempHotList,
        bool          removeNotInTemp)
    {
        // 使用 AsNoTracking() 避免追踪查询的实体，因为我们只关心 ID 列表
        var dbHotIds = await db.EpisodeList
                               .AsNoTracking()
                               .Select(e => e.Id)
                               .ToListAsync();
        var dbHotIdSet  = dbHotIds.ToHashSet();
        var tempHotDict = tempHotList.ToDictionary(e => e.Id);

        if (removeNotInTemp)
        {
            Console.WriteLine("🧹 Cleaning up hot episodes...");
            var removedHot = 0;

            // 找出需要删除的 ID
            var idsToRemove = dbHotIdSet.Where(id => !tempHotDict.ContainsKey(id)).ToList();

            foreach (var id in idsToRemove)
            {
                // 使用 Attach + Remove 方式删除，这样可以和 Add 操作在同一个 SaveChanges 事务中
                var entityToRemove = new Episode { Id = id };
                db.EpisodeList.Attach(entityToRemove);
                db.EpisodeList.Remove(entityToRemove);
                removedHot++;
            }

            Console.WriteLine($"🗑 Marked {removedHot} hot episodes for removal.");
        }

        var addedHot = 0;
        Console.WriteLine("➕ Adding new hot episodes...");

        // 重新查询数据库中的 ID（因为可能有其他进程插入，或者刚刚删除了一些）
        var currentDbHotIds = await db.EpisodeList
                                      .AsNoTracking()
                                      .Select(e => e.Id)
                                      .ToListAsync();
        var currentDbHotIdSet = currentDbHotIds.ToHashSet();

        // 获取 ChangeTracker 中已添加的 ID（避免重复添加）
        var trackedAddedIds = db.ChangeTracker.Entries<Episode>()
                                .Where(e => e.State == EntityState.Added)
                                .Select(e => e.Entity.Id)
                                .ToHashSet();

        foreach (var tempItem in tempHotList.Where(tempItem =>
                                                       !currentDbHotIdSet.Contains(tempItem.Id) &&
                                                       !trackedAddedIds.Contains(tempItem.Id)))
        {
            await db.EpisodeList.AddAsync(tempItem);
            addedHot++;
        }

        Console.WriteLine($"✅ Marked {addedHot} new hot episodes for addition.");

        // 统一保存所有变更
        if (addedHot > 0 || (removeNotInTemp && dbHotIdSet.Count > tempHotDict.Count))
        {
            await SaveChangesWithRetryAsync(db);
        }
    }

    /// <summary>
    /// 同步冷表数据（移除不存在的数据，添加新数据）
    /// </summary>
    private static async Task SyncColdListAsync(
        MyDbContext       db,
        List<EpisodeCold> tempColdList,
        bool              removeNotInTemp)
    {
        // 使用 AsNoTracking() 避免追踪查询的实体，因为我们只关心 ID 列表
        var dbColdIds = await db.EpisodeListCold
                                .AsNoTracking()
                                .Select(e => e.Id)
                                .ToListAsync();
        var dbColdIdSet  = dbColdIds.ToHashSet();
        var tempColdDict = tempColdList.ToDictionary(e => e.Id);

        if (removeNotInTemp)
        {
            Console.WriteLine("🧹 Cleaning up cold episodes...");
            var removedCold = 0;

            // 找出需要删除的 ID
            var idsToRemove = dbColdIdSet.Where(id => !tempColdDict.ContainsKey(id)).ToList();

            foreach (var id in idsToRemove)
            {
                // 使用 Attach + Remove 方式删除，这样可以和 Add 操作在同一个 SaveChanges 事务中
                var entityToRemove = new EpisodeCold { Id = id };
                db.EpisodeListCold.Attach(entityToRemove);
                db.EpisodeListCold.Remove(entityToRemove);
                removedCold++;
            }

            Console.WriteLine($"🗑 Marked {removedCold} cold episodes for removal.");
        }

        var addedCold = 0;
        Console.WriteLine("➕ Adding new cold episodes...");

        // 重新查询数据库中的 ID（因为可能有其他进程插入，或者刚刚删除了一些）
        var currentDbColdIds = await db.EpisodeListCold
                                       .AsNoTracking()
                                       .Select(e => e.Id)
                                       .ToListAsync();
        var currentDbColdIdSet = currentDbColdIds.ToHashSet();

        // 获取 ChangeTracker 中已添加的 ID（避免重复添加）
        var trackedAddedColdIds = db.ChangeTracker.Entries<EpisodeCold>()
                                    .Where(e => e.State == EntityState.Added)
                                    .Select(e => e.Entity.Id)
                                    .ToHashSet();

        foreach (var tempItem in tempColdList.Where(tempItem =>
                                                        !currentDbColdIdSet.Contains(tempItem.Id) &&
                                                        !trackedAddedColdIds.Contains(tempItem.Id)))
        {
            await db.EpisodeListCold.AddAsync(tempItem);
            addedCold++;
        }

        Console.WriteLine($"✅ Marked {addedCold} new cold episodes for addition.");

        // 统一保存所有变更
        if (addedCold > 0 || (removeNotInTemp && dbColdIdSet.Count > tempColdDict.Count))
        {
            await SaveChangesWithRetryAsync(db);
        }
    }

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

            var bilibiliHotList = await GetBilibiliHotListAsync(mapping.BilibiliId);

            Console.WriteLine("📥 Fetching DandanPlay info...");
            var info = await DandanPlayUtils.GetFullAnimeInfo(mapping.DandanId);
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

            ProcessEpisodes(episodeList, bangumiId, bilibiliHotList, tempHotList, tempColdList);
        }

        Console.WriteLine("\n🧊 Loading existing hot/cold lists from DB...");
        await SyncHotListAsync(db, tempHotList, removeNotInTemp : true);
        await SyncColdListAsync(db, tempColdList, removeNotInTemp : true);

        // 清理 ChangeTracker，防止长时间运行导致内存膨胀和脏数据
        db.ChangeTracker.Clear();
        Console.WriteLine("🧹 ChangeTracker cleared.");

        Console.WriteLine("🎉 UpdateBangumi completed successfully!");
    }

    public static async Task UpdateDandan(MyDbContext db)
    {
        var tempHotList  = new List<Episode>();
        var tempColdList = new List<EpisodeCold>();

        Console.WriteLine("📅 Fetching dandan recent...");
        var bangumiList = await DandanPlayUtils.GetRecentAnime();
        if (bangumiList == null) return;
        Console.WriteLine($"📅 Got {bangumiList.Count} items from calendar.");

        Console.WriteLine("📊 Loading mapping list from DB...");
        var mappingList = db.MappingList.ToList();
        var idList = db.EpisodeList
                       .Select(e => e.SubjectId)
                       .Concat(db.EpisodeListCold.Select(e => e.SubjectId))
                       .Distinct()
                       .ToList();
        // 去除已有的id
        bangumiList = bangumiList
                     .Where(e =>
                      {
                          var mapping = mappingList.FirstOrDefault(m => m.DandanId == e.Id);
                          if (mapping == null)
                          {
                              Console.WriteLine($"Mapping not contains, id:{e.Id}");
                          }

                          return mapping != null && !idList.Contains(mapping.BangumiId);
                      })
                     .ToList();
        Console.WriteLine($"📊 Loaded {mappingList.Count} mapping entries.");

        foreach (var (bangumiId, name) in bangumiList
                    .Select(e => (mappingList.First(m => m.DandanId == e.Id).BangumiId, e.Title)))
        {
            Console.WriteLine($"\n🎬 Processing {name} (ID: {bangumiId})");

            var mapping = mappingList.FirstOrDefault(e => e.BangumiId == bangumiId);
            if (mapping == null)
            {
                if (!idList.Contains(bangumiId))
                    Console.WriteLine("⚠️ Mapping not found, skipping.");
                continue;
            }

            if (!mapping.IsJapaneseAnime!.Value)
            {
                Console.WriteLine("🔕 Not Japanese anime, skipping.");
                continue;
            }

            var bilibiliHotList = await GetBilibiliHotListAsync(mapping.BilibiliId);

            Console.WriteLine("📥 Fetching DandanPlay info...");
            var info = await DandanPlayUtils.GetFullAnimeInfo(mapping.DandanId);
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

            ProcessEpisodes(episodeList, bangumiId, bilibiliHotList, tempHotList, tempColdList);
        }

        Console.WriteLine("\n🧊 Loading existing hot/cold lists from DB...");
        await SyncHotListAsync(db, tempHotList, removeNotInTemp : false);
        await SyncColdListAsync(db, tempColdList, removeNotInTemp : false);

        // 清理 ChangeTracker，防止长时间运行导致内存膨胀和脏数据
        db.ChangeTracker.Clear();
        Console.WriteLine("🧹 ChangeTracker cleared.");

        Console.WriteLine("🎉 UpdateDandan completed successfully!");
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
        var existingDandanIds = allMappings.Select(m => m.DandanId).Distinct();

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
        }

        await SaveChangesWithRetryAsync(db);

        // 🌟 3. 解析 BilibiliId
        foreach (var item in allMappings.Where(e => e.BilibiliId == -1))
        {
            var bilibiliId = await Bangumi2BilibiliUtils.Parser(item.BangumiId);
            if (bilibiliId == -1) continue;
            item.BilibiliId = bilibiliId;
        }

        await SaveChangesWithRetryAsync(db);

        // 🌟 4. 填补 AirDate 和 IsJapaneseAnime
        foreach (var item in allMappings.Where(e => e.AirDate == null || e.IsJapaneseAnime == null))
        {
            var info = await BangumiUtils.GetSubjectInfo(item.BangumiId);
            item.AirDate         = info?.Date!;
            item.IsJapaneseAnime = info?.MetaTagList?.Contains("日本");
        }

        // ✅ 最后统一保存
        await SaveChangesWithRetryAsync(db);
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
            catch (DbUpdateConcurrencyException concurrencyEx)
            {
                // 并发冲突：实体在数据库中已被删除或修改
                Console.WriteLine($"⚠️ [Attempt {attempt}] Concurrency conflict. Detaching failed entries...");

                foreach (var entry in concurrencyEx.Entries)
                {
                    entry.State = EntityState.Detached;
                }

                // 并发冲突分离后可以立即重试，让 EF Core 重新评估剩余实体
                if (attempt == maxRetries)
                {
                    Console.WriteLine("Reached max retries. Rethrowing.");
                    throw;
                }
            }
            catch (DbUpdateException dbEx) when (IsDuplicateKeyException(dbEx))
            {
                // 主键/唯一约束冲突 (23505)：数据已存在，直接清空 Tracker 跳过
                Console.WriteLine($"⚠️ [Attempt {attempt}] Duplicate key (23505). Clearing entire batch tracker to be safe.");

                db.ChangeTracker.Clear();
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

                var waitTime = delayMs * attempt;
                Console.WriteLine($"⏳ Waiting {waitTime}ms before retry {attempt + 1}/{maxRetries}...");
                await Task.Delay(waitTime);
            }
        }
    }

    private static bool IsTransient(Exception ex)
    {
        // 处理 PostgreSQL 超时和读取异常
        if (ex is DbUpdateException { InnerException: Npgsql.NpgsqlException exception } &&
            (exception.Message.Contains("Timeout") || exception.Message.Contains("Exception while reading")))
        {
            return true;
        }

        // 处理乐观并发冲突异常 - 可以重试
        if (ex is DbUpdateConcurrencyException)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查是否为主键/唯一约束冲突异常 (PostgreSQL 23505)
    /// </summary>
    private static bool IsDuplicateKeyException(Exception ex)
    {
        return ex is DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23505" } };
    }
}
