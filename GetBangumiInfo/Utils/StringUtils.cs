using System.Text.Json;

namespace GetBangumiInfo.Utils;

internal class StringUtils
{
    public static bool QuickFilter(string line, int subjectId)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;

        // 快速字符串筛选（避免 JSON 反序列化）
        if (line.Contains("\"name\":\"\",\"name_cn\":\"\"")) return false;
        if (line.Contains("\"airdate\":\"\"")) return false;
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

        return DateTime.TryParse(airdateStr, out var airdate) && DateUtils.IsWithinThreeMonths(airdate);
    }
}