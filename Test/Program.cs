using GetBangumiInfo.Utils;

namespace Test;

internal class Program
{
    private static void Main()
    {
        var todayWeekday     = DateUtils.GetBeijingWeekday() + 1;
        var yesterdayWeekday = (todayWeekday + 6) % 7;
        Console.WriteLine(todayWeekday);
        Console.WriteLine(yesterdayWeekday);
    }
}
