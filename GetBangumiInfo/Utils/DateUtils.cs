using TimeZoneConverter;

namespace GetBangumiInfo.Utils;

internal class DateUtils
{
    public static DateTime GetBeijingNow()
    {
        var tz = TZConvert.GetTimeZoneInfo("Asia/Shanghai");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    }

    public static int GetBeijingWeekday()
    {
        return ((int)GetBeijingNow().DayOfWeek + 6) % 7;
    }

    public static bool IsWithinThreeMonths(DateTime date)
    {
        var today          = GetBeijingNow().Date; // 可以在调用处先传入 today，避免重复获取
        var threeMonthsAgo = today.AddMonths(-3);
        return date.Date >= threeMonthsAgo && date.Date <= today;
    }
}
