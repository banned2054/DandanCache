using GetBangumiInfo.Database;

namespace DanmakuUpdate;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length == 0) return;
        var db = new MyDbContext();

        var dandanAppId     = Environment.GetEnvironmentVariable("DandanAppId");
        var dandanAppSecret = Environment.GetEnvironmentVariable("DandanAppSecret");
        if (string.IsNullOrEmpty(dandanAppId) || string.IsNullOrEmpty(dandanAppSecret))
        {
            return;
        }

        var client = new DandanApiClient(dandanAppId, dandanAppSecret);
        if (args[0] == "hot")
        {
            foreach (var episode in db.EpisodeList)
            {
                var danmaku = await client.GetDanmakuAsync(episode.Id);
            }
        }
        else
        {
            foreach (var episode in db.EpisodeListCold)
            {
                var danmaku = await client.GetDanmakuAsync(episode.Id);
            }
        }
    }
}
