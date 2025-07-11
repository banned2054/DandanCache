using System.Globalization;
using TimeZoneConverter;

namespace GetBangumiInfo.Utils;

public class TimeUtils
{
    /// <summary>
    /// 获取北京时间的现在的时间
    /// </summary>
    public static DateTimeOffset GetNow()
    {
        var tz = TZConvert.GetTimeZoneInfo("Asia/Shanghai");
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz); // 正确保留 +08:00
    }


    /// <summary>
    /// 获取北京时间的星期（0=周一，6=周日）
    /// </summary>
    public static int GetWeekday(DateTimeOffset time)
    {
        var weekday = time.DayOfWeek;
        return ((int)weekday + 6) % 7;
    }

    /// <summary>
    /// 获取北京时间的星期（0=周一，6=周日）
    /// </summary>
    public static int GetNowWeekday() => GetWeekday(GetNow());


    /// <summary>
    /// 判断一个时间是否在北京时间的最近三个月内
    /// </summary>
    public static bool IsWithinThreeMonths(DateTimeOffset inputTime)
    {
        var today          = GetNow().Date;
        var threeMonthsAgo = today.AddMonths(-3);
        return inputTime.Date >= threeMonthsAgo && inputTime.Date <= today;
    }

    /// <summary>
    /// 判断一个时间是否在北京时间的最近三个月内
    /// </summary>
    public static bool IsWithinThreeDays(DateTimeOffset inputTime)
    {
        var today          = GetNow().Date;
        var threeMonthsAgo = today.AddDays(-3);
        return inputTime.Date >= threeMonthsAgo && inputTime.Date <= today;
    }

    public static DateTimeOffset? ParseString(string timeStr, string format = "yyyy-MM-dd HH:mm:ss")
    {
        if (string.IsNullOrEmpty(timeStr)) return null;
        var dt = DateTime.ParseExact(timeStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None);
        return new DateTimeOffset(dt, TimeSpan.FromHours(8)); // 明确标记为北京时间
    }

    /// <summary>
    /// 从 Unix 时间戳转换为北京时间的时间（DateTimeOffset）
    /// </summary>
    public static DateTimeOffset ParseUnix(long unixTime)
    {
        var utcTime     = DateTimeOffset.FromUnixTimeSeconds(unixTime);
        var beijingZone = TZConvert.GetTimeZoneInfo("Asia/Shanghai");
        return TimeZoneInfo.ConvertTime(utcTime, beijingZone);
    }
}
