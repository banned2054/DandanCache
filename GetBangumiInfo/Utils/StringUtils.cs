using System.Text.Json;
using System.Text.RegularExpressions;

namespace GetBangumiInfo.Utils;

public class StringUtils
{
    private static readonly Regex BangumiUrlRegex = new(@"(?:bgm|bangumi)\.tv/subject/(?<subjectId>\d+)");

    public static bool IsPropertyNull(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return true;
        if (line.Contains("\"name\":\"\",\"name_cn\":\"\"")) return true;
        return line.Contains("\"airdate\":\"\"");
    }

    public static bool QuickFilter(string line, int subjectId)
    {
        if (IsPropertyNull(line)) return false;
        return line.Contains($"\"subject_id\":{subjectId},");
    }

    public static bool IsValid(JsonElement json, int subjectId)
    {
        if (!json.TryGetProperty("subject_id", out var sid) || sid.GetInt32() != subjectId)
            return false;

        if (!json.TryGetProperty("airdate", out var airdateProp))
            return false;

        var airdateStr = airdateProp.GetString();
        if (string.IsNullOrWhiteSpace(airdateStr)) return false;

        return DateTime.TryParse(airdateStr, out var airdate) && TimeUtils.IsWithinThreeMonths(airdate);
    }

    public static bool IsBangumiUrl(string url)
    {
        return BangumiUrlRegex.IsMatch(url);
    }

    public static int GetBangumiIdFromUrl(string url)
    {
        if (!IsBangumiUrl(url)) return -1;
        var match = BangumiUrlRegex.Match(url);
        return int.Parse(match.Groups["subjectId"].Value);
    }
}
