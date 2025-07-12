using GetBangumiInfo.Controllers;
using GetBangumiInfo.Database;
using GetBangumiInfo.Utils.Api;

namespace GetBangumiInfo;

internal class Program
{
    private static async Task Main()
    {
        DotNetEnv.Env.Load();

        var dandanAppId     = Environment.GetEnvironmentVariable("DandanAppId");
        var dandanAppSecret = Environment.GetEnvironmentVariable("DandanAppSecret");
        var connectSetting  = Environment.GetEnvironmentVariable("DatabaseConnectSetting");

        if (string.IsNullOrWhiteSpace(dandanAppId))
        {
            throw new Exception("App Id is empty");
        }

        if (string.IsNullOrWhiteSpace(dandanAppSecret))
        {
            throw new Exception("App Secret is empty");
        }

        if (string.IsNullOrEmpty(connectSetting))
        {
            throw new Exception("Database Connect Setting Not Found.");
        }

        if (Environment.GetEnvironmentVariable("IsLocal") != "true")
        {
            await BangumiUtils.DownloadDumpFile();
            await BangumiUtils.UnzipDumpFile();
        }

        var db = new MyDbContext();
        await UpdateController.UpdateByDandan();
        await UpdateController.UpdateBangumi();
    }
}
