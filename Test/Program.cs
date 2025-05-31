using GetBangumiInfo.Database;
using GetBangumiInfo.Utils;

namespace Test;

internal class Program
{
    private static void Main()
    {
        DotNetEnv.Env.Load();

        var db            = new MyDbContext();
        var subjectIdList = db.EpisodeList.Select(e => e.SubjectId).Distinct();
        foreach (var subjectId in subjectIdList)
        {
            var element = BangumiUtils.GetSubjectInfo(subjectId);
            if (element == null) continue;
            if (!element.Value.TryGetProperty("date", out var dateProp)) continue;
            var dateStr = dateProp.GetString();
            if (!DateTime.TryParse(dateStr, out var date)) continue;
            var weekday = ((int)date.DayOfWeek + 6) % 7 + 1; // 周一=1，周日=7
            Console.WriteLine($"Weekday: {weekday}");
        }
    }
}
