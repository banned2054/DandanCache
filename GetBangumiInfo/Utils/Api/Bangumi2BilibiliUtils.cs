using GetBangumiInfo.Models.Anime;
using Newtonsoft.Json;

namespace GetBangumiInfo.Utils.Api;

public class Bangumi2BilibiliUtils
{
    private const           string   CachePath     = "bangumi-data-cache.json";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(1);

    /// <summary>
    /// Cache不存在的时候，下载新的数据，同时，仅保留需要的bangumi和bilibili的映射关系
    /// </summary>
    /// <returns>List of B2B</returns>
    private static async Task<string> DownloadBangumiDataJson()
    {
        var response = await NetUtils.FetchAsync<string>("https://unpkg.com/bangumi-data@0.3/dist/data.json",
                                                         null,
                                                         true);
        var responseJson = JsonConvert.DeserializeObject<BangumiDataResponse>(response);
        var animeList    = responseJson!.Items;

        var mappingList = animeList!.Select(anime =>
                                     {
                                         var bangumiInfo  = anime.Sites?.FirstOrDefault(s => s.Site == "bangumi");
                                         var bilibiliInfo = anime.Sites?.FirstOrDefault(s => s.Site == "bilibili");

                                         if (bangumiInfo?.Id is not null && bilibiliInfo?.Id is not null)
                                         {
                                             return new B2B
                                             {
                                                 BangumiId  = int.Parse(bangumiInfo.Id),
                                                 BilibiliId = int.Parse(bilibiliInfo.Id)
                                             };
                                         }

                                         return null;
                                     })
                                    .Where(m => m != null)!
                                    .OrderBy(m => m.BangumiId)
                                    .ToList();
        var jsonStr = JsonConvert.SerializeObject(mappingList, Formatting.Indented);
        await File.WriteAllTextAsync(CachePath, jsonStr);
        return jsonStr;
    }

    private static async Task<string> GetBangumiDataJson()
    {
        if (!File.Exists(CachePath)) return await DownloadBangumiDataJson();
        var fileInfo = new FileInfo(CachePath);
        if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc >= CacheDuration) return await DownloadBangumiDataJson();
        try
        {
            var jsonStr = await File.ReadAllTextAsync(CachePath);
            return jsonStr;
        }
        catch
        {
            return await DownloadBangumiDataJson();
        }
    }

    /// <summary>
    /// parser bangumi id to bilibili id, if not found, then return -1
    /// </summary>
    /// <param name="subjectId">bangumi id</param>
    /// <returns></returns>
    public static async Task<int> Parser(int subjectId)
    {
        var json      = await GetBangumiDataJson();
        var animeList = JsonConvert.DeserializeObject<List<B2B>>(json);
        if (animeList == null || animeList.Count == 0) return -1;
        int left = 0, right = animeList.Count - 1;

        while (left <= right)
        {
            var mid = (left + right) / 2;
            if (animeList[mid].BangumiId == subjectId)
            {
                return animeList[mid].BilibiliId;
            }

            if (animeList[mid].BangumiId < subjectId)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return -1;
    }
}
