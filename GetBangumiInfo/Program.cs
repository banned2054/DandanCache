using GetBangumiInfo.Database;
using GetBangumiInfo.Models.Database;
using GetBangumiInfo.Utils;
using Microsoft.EntityFrameworkCore;

namespace GetBangumiInfo;

internal class Program
{
    private static async Task Main()
    {
        var (hotList, coldList) = await BangumiUtils.GetCalendar();
        await using var db          = new MyDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        // 高效清空 EpisodeList
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"episodeList\";");

        // 添加 Hot Episodes
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

        // 删除冷表中已存在的 hot ids
        var hotIds       = hotEpisodes.Select(e => e.Id).ToList();
        var toDeleteCold = db.EpisodeListCold.Where(e => hotIds.Contains(e.Id));
        db.EpisodeListCold.RemoveRange(toDeleteCold);

        var coldEpisodes = coldList
                          .SelectMany(BangumiUtils.GetSubjectEpisodeList)
                          .Select(item => new EpisodeCold
                           {
                               Id         = item.GetProperty("id").GetInt32(),
                               EpisodeNum = item.GetProperty("sort").GetSingle(),
                               SubjectId  = item.GetProperty("subject_id").GetInt32()
                           })
                          .ToList();

        db.EpisodeListCold.AddRange(coldEpisodes);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
