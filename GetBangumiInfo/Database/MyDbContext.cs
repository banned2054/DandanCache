using GetBangumiInfo.Models.Database;
using Microsoft.EntityFrameworkCore;

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
        var connectSetting = Environment.GetEnvironmentVariable("ConnectSetting ");
        optionsBuilder.UseNpgsql(connectSetting);
    }
}