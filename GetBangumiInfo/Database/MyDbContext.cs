using System.Net;
using GetBangumiInfo.Models.Database;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace GetBangumiInfo.Database;

public class MyDbContext : DbContext
{
    /// <summary>
    /// 热表，每小时更新
    /// </summary>
    public DbSet<Episode> EpisodeList { get; set; }

    /// <summary>
    /// 半冷，每天更新
    /// </summary>
    public DbSet<EpisodeCold> EpisodeListCold { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectHost    = Environment.GetEnvironmentVariable("DatabaseHost");
        var connectSetting = Environment.GetEnvironmentVariable("DatabaseConnectSetting");
        if (string.IsNullOrEmpty(connectHost) || string.IsNullOrEmpty(connectSetting)) return;

        var hostEntries = Dns.GetHostAddresses(connectHost);
        var ipv4Address = hostEntries.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

        if (ipv4Address == null)
        {
            Console.WriteLine("Failed to resolve IPv4 address.");
            return;
        }

        // 构造完整连接字符串：Host=xxx + connectSetting（后半部分）
        var fullConnectionString = $"Host={ipv4Address};{connectSetting}";

        optionsBuilder.UseNpgsql(fullConnectionString);
    }
}
