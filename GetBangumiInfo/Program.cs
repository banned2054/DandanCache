using GetBangumiInfo.Controllers;
using GetBangumiInfo.Utils.Api;

namespace GetBangumiInfo;

internal class Program
{
    private static async Task Main()
    {
        DotNetEnv.Env.Load();
        if (Environment.GetEnvironmentVariable("IsLocal") != "true")
        {
            await BangumiUtils.DownloadDumpFile();
            await BangumiUtils.UnzipDumpFile();
        }

        //await UpdateController.UpdateBangumi();
        await UpdateController.UpdateByDandan();
    }
}
