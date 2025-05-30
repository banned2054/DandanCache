using GetBangumiInfo.Database;
using GetBangumiInfo.Models.Database;
using GetBangumiInfo.Utils;
using Microsoft.EntityFrameworkCore;

namespace GetBangumiInfo;

internal class Program
{
    private static async Task Main()
    {
        await BangumiUtils.DownloadDumpFile();
        await BangumiUtils.UnzipDumpFile();

        var (hotList, coldList) = await BangumiUtils.GetCalendar();
        await using var db          = new MyDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        // Step 1: 读取旧数据
        var oldHotList  = await db.EpisodeList.AsNoTracking().ToListAsync();
        var oldColdList = await db.EpisodeListCold.AsNoTracking().ToListAsync();

        var oldHotIds  = oldHotList.Select(e => e.Id).ToHashSet();
        var oldColdIds = oldColdList.Select(e => e.Id).ToHashSet();

        // Step 2: 清空热表与冷表
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"episodeList\";");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"episodeListCold\";");

        // Step 3: 构造新热表数据
        var hotEpisodes = hotList
                         .SelectMany(BangumiUtils.GetSubjectEpisodeList)
                         .Select(item => new Episode
                          {
                              Id         = item.GetProperty("id").GetInt32(),
                              EpisodeNum = item.GetProperty("sort").GetSingle(),
                              SubjectId  = item.GetProperty("subject_id").GetInt32()
                          })
                         .ToList();

        db.EpisodeList.AddRange(hotEpisodes);
        var hotIds = hotEpisodes.Select(e => e.Id).ToHashSet();

        // Step 4: 构造新冷表数据，排除热数据
        var coldEpisodes = coldList
                          .SelectMany(BangumiUtils.GetSubjectEpisodeList)
                          .Select(item => new EpisodeCold
                           {
                               Id         = item.GetProperty("id").GetInt32(),
                               EpisodeNum = item.GetProperty("sort").GetSingle(),
                               SubjectId  = item.GetProperty("subject_id").GetInt32()
                           })
                          .Where(e => !hotIds.Contains(e.Id))
                          .ToList();

        db.EpisodeListCold.AddRange(coldEpisodes);
        var coldIds = coldEpisodes.Select(e => e.Id).ToHashSet();

        // Step 5: 从旧热/冷表中筛选未出现在新数据中的，标记为VeryCold
        var newHotColdIds = hotIds.Union(coldIds);

        var veryColdCandidates = oldHotList
                                .Where(e => !newHotColdIds.Contains(e.Id))
                                .Select(e => new EpisodeVeryCold
                                 {
                                     Id         = e.Id,
                                     SubjectId  = e.SubjectId,
                                     EpisodeNum = e.EpisodeNum ?? 0,
                                     AddInDate  = DateTime.UtcNow
                                 })
                                .Concat(
                                        oldColdList
                                           .Where(e => !newHotColdIds.Contains(e.Id))
                                           .Select(e => new EpisodeVeryCold
                                            {
                                                Id         = e.Id,
                                                SubjectId  = e.SubjectId,
                                                EpisodeNum = e.EpisodeNum ?? 0,
                                                AddInDate  = DateTime.UtcNow
                                            })
                                       )
                                .ToList();

        db.EpisodeListVeryCold.AddRange(veryColdCandidates);

        // Step 6: 清除VeryCold中已“回温”的数据
        var allNewIds            = newHotColdIds.Select(id => (long)id).ToList();
        var toDeleteFromVeryCold = db.EpisodeListVeryCold.Where(e => allNewIds.Contains(e.Id));
        db.EpisodeListVeryCold.RemoveRange(toDeleteFromVeryCold);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
