using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace GetBangumiInfo.Utils;

internal class FileUtils
{
    public static void UnzipFile(string zipPath, string extractPath)
    {
        ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles : true);
    }

    public static IEnumerable<JsonElement> ReverseSearchEpisodes(int subjectId)
    {
        const int    limit    = 20;
        const string fileName = "episode.jsonlines";

        var results = new List<JsonElement>();
        var path    = Path.Combine(AppContext.BaseDirectory, fileName);

        using var fs     = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var       pos    = fs.Length - 1;
        var       buffer = new List<byte>();

        while (pos >= 0)
        {
            fs.Seek(pos--, SeekOrigin.Begin);
            var b = fs.ReadByte();
            if (b == '\n')
            {
                if (buffer.Count <= 0) continue;
                var line = Encoding.UTF8.GetString(buffer.AsEnumerable().Reverse().ToArray()).Trim();
                buffer.Clear();

                if (!StringUtils.QuickFilter(line, subjectId)) continue;
                try
                {
                    var doc  = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    if (StringUtils.IsValid(root, subjectId))
                    {
                        results.Add(root);
                        if (results.Count >= limit * 3) break; // 先多收集一些，后面再排序取前 N
                    }
                }
                catch
                {
                    /* ignore broken json */
                }
            }
            else
            {
                buffer.Add((byte)b);
            }
        }

        // 最前一行
        if (buffer.Count <= 0)
            return results
                  .Where(e => e.TryGetProperty("airdate", out var d) && DateTime.TryParse(d.GetString(), out _))
                  .OrderByDescending(e => DateTime.Parse(e.GetProperty("airdate").GetString()!))
                  .Take(limit)
                  .ToList();
        {
            var line = Encoding.UTF8.GetString(buffer.AsEnumerable().Reverse().ToArray()).Trim();
            if (!StringUtils.QuickFilter(line, subjectId))
                return results
                      .Where(e => e.TryGetProperty("airdate", out var d) && DateTime.TryParse(d.GetString(), out _))
                      .OrderByDescending(e => DateTime.Parse(e.GetProperty("airdate").GetString()!))
                      .Take(limit)
                      .ToList();
            try
            {
                var doc  = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (StringUtils.IsValid(root, subjectId))
                    results.Add(root);
            }
            catch
            {
                // ignored
            }
        }

        return results
              .Where(e => e.TryGetProperty("airdate", out var d) && DateTime.TryParse(d.GetString(), out _))
              .OrderByDescending(e => DateTime.Parse(e.GetProperty("airdate").GetString()!))
              .Take(limit)
              .ToList();
    }
}
