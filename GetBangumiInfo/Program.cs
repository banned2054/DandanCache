using GetBangumiInfo.Controllers;

namespace GetBangumiInfo;

internal class Program
{
    private static async Task Main()
    {
        DotNetEnv.Env.Load();
        await UpdateController.UpdateBangumi();
        await UpdateController.UpdateByDandan();
    }
}
