using DanmakuUpdate;
using GetBangumiInfo.Database;
using GetBangumiInfo.Models.Database;
using GetBangumiInfo.Utils.Api;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GetBangumiInfo.Controllers;

public class UpdateController
{
    public static async Task UpdateBangumi()
    {
        // ① 准备离线数据
        await BangumiUtils.DownloadDumpFile();
        await BangumiUtils.UnzipDumpFile();

        // ② 取本周番剧 SubjectId 列表
        var (hotSubjectIds, coldSubjectIds) = await BangumiUtils.GetCalendar();

        await using var db = new MyDbContext();
        await using var tx = await db.Database.BeginTransactionAsync();

        // ---------- 1. 读取旧表 ----------
        var oldHotList  = await db.EpisodeList.AsNoTracking().ToListAsync();
        var oldColdList = await db.EpisodeListCold.AsNoTracking().ToListAsync();

        var oldHotIds  = oldHotList.Select(e => e.Id).ToHashSet();
        var oldColdIds = oldColdList.Select(e => e.Id).ToHashSet();

        // ---------- 2. 生成新热表 ----------
        var hotEpisodes = hotSubjectIds
                         .SelectMany(BangumiUtils.GetSubjectEpisodeList)
                         .Select(j => new Episode
                          {
                              Id         = j.GetProperty("id").GetInt32(),
                              SubjectId  = j.GetProperty("subject_id").GetInt32(),
                              EpisodeNum = j.TryGetProperty("sort", out var s) ? s.GetSingle() : null
                          })
                         .ToList();

        var newHotIds = hotEpisodes.Select(e => e.Id).ToHashSet();

        // ---------- 3. 生成新冷表（排除热表里的） ----------
        var coldEpisodes = coldSubjectIds
                          .SelectMany(BangumiUtils.GetSubjectEpisodeList)
                          .Select(j => new EpisodeCold
                           {
                               Id         = j.GetProperty("id").GetInt32(),
                               SubjectId  = j.GetProperty("subject_id").GetInt32(),
                               EpisodeNum = j.TryGetProperty("sort", out var s) ? s.GetSingle() : null
                           })
                          .Where(e => !newHotIds.Contains(e.Id))
                          .ToList();

        var newColdIds = coldEpisodes.Select(e => e.Id).ToHashSet();

        // ---------- 4. 差集运算 ----------
        var hotToInsert     = hotEpisodes.Where(e => !oldHotIds.Contains(e.Id)).ToList();
        var coldToInsert    = coldEpisodes.Where(e => !oldColdIds.Contains(e.Id)).ToList();
        var hotToDeleteIds  = oldHotIds.Except(newHotIds).ToList();
        var coldToDeleteIds = oldColdIds.Except(newColdIds).ToList();

        // ---------- 5. 删掉过时行 ----------
        if (hotToDeleteIds.Count > 0)
            await db.EpisodeList
                    .Where(e => hotToDeleteIds.Contains(e.Id))
                    .ExecuteDeleteAsync();

        if (coldToDeleteIds.Count > 0)
            await db.EpisodeListCold
                    .Where(e => coldToDeleteIds.Contains(e.Id))
                    .ExecuteDeleteAsync();

        // ---------- 6. 插入新增行 ----------
        if (hotToInsert.Count  > 0) await db.EpisodeList.AddRangeAsync(hotToInsert);
        if (coldToInsert.Count > 0) await db.EpisodeListCold.AddRangeAsync(coldToInsert);

        // ---------- 7. VeryCold：新增 ----------
        var idsMovedToVeryCold = hotToDeleteIds.Concat(coldToDeleteIds).ToHashSet();
        if (idsMovedToVeryCold.Count > 0)
        {
            var existedVcIds = await db.EpisodeListVeryCold
                                       .Where(vc => idsMovedToVeryCold.Contains(vc.Id))
                                       .Select(vc => vc.Id)
                                       .ToHashSetAsync();

            var veryColdCandidates =
                oldHotList.Where(e => idsMovedToVeryCold.Contains(e.Id))
                          .Select(e => new EpisodeVeryCold
                           {
                               Id         = e.Id,
                               SubjectId  = e.SubjectId,
                               EpisodeNum = e.EpisodeNum ?? 0,
                               AddInDate  = DateTime.UtcNow
                           })
                          .Concat(
                                  oldColdList.Where(e => idsMovedToVeryCold.Contains(e.Id))
                                             .Select(e => new EpisodeVeryCold
                                              {
                                                  Id         = e.Id,
                                                  SubjectId  = e.SubjectId,
                                                  EpisodeNum = e.EpisodeNum ?? 0,
                                                  AddInDate  = DateTime.UtcNow
                                              }))
                          .Where(vc => !existedVcIds.Contains(vc.Id))
                          .ToList();

            if (veryColdCandidates.Count > 0)
                await db.EpisodeListVeryCold.AddRangeAsync(veryColdCandidates);
        }

        // ---------- 8. VeryCold：清除“回温”的 ----------
        var currentActiveIds = newHotIds.Union(newColdIds)
                                        .ToHashSet();

        await db.EpisodeListVeryCold
                .Where(vc => currentActiveIds.Contains(vc.Id))
                .ExecuteDeleteAsync();

        // ---------- 9. 提交 ----------
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public static readonly Regex BangumiRegex = new(@"subject/(?<id>\d+)");

    public static async Task UpdateByDandan()
    {
        var dandanAppId     = Environment.GetEnvironmentVariable("DandanAppId");
        var dandanAppSecret = Environment.GetEnvironmentVariable("DandanAppSecret");
        if (string.IsNullOrEmpty(dandanAppId) || string.IsNullOrEmpty(dandanAppSecret))
        {
            return;
        }

        var shortInfoList = await DandanPlayUtils.GetRecentAnime();
        if (shortInfoList == null || shortInfoList.Count == 0)
        {
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

        await db.SaveChangesAsync();
    }
}
