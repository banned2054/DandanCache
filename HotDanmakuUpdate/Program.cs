using GetBangumiInfo.Database;

namespace HotDanmakuUpdate;

internal class Program
{
    private static async Task Main()
    {
        var db = new MyDbContext();

        var dandanAppId     = Environment.GetEnvironmentVariable("DandanAppId");
        var dandanAppSecret = Environment.GetEnvironmentVariable("DandanAppSecret");
        if (string.IsNullOrEmpty(dandanAppId) || string.IsNullOrEmpty(dandanAppSecret))
        {
            return;
        }

        var client = new DandanApiClient(dandanAppId, dandanAppSecret);
        foreach (var episode in db.EpisodeList)
        {
            var danmaku = await client.GetDanmakuAsync(episode.Id);
        }
    }
}
