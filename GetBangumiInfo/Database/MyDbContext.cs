using GetBangumiInfo.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace GetBangumiInfo.Database;

public class MyDbContext : DbContext
{
    /// <summary>
    /// 热表，每小时更新
    /// </summary>
    public DbSet<Episode> EpisodeList { get; set; } = null!;

    /// <summary>
    /// 半冷，每天更新
    /// </summary>
    public DbSet<EpisodeCold> EpisodeListCold { get; set; } = null!;

    /// <summary>
    /// 半冷，每天更新
    /// </summary>
    public DbSet<EpisodeVeryCold> EpisodeListVeryCold { get; set; } = null!;

    /// <summary>
    /// bangumi、bilibili、弹弹play的id映射
    /// </summary>
    public DbSet<Mapping> MappingList { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectSetting = Environment.GetEnvironmentVariable("DatabaseConnectSetting");
        if (string.IsNullOrEmpty(connectSetting))
        {
            throw new Exception("Database Connect Setting Not Found.");
        }

        optionsBuilder.UseNpgsql(connectSetting);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 联合主键：Episode（热表）
        modelBuilder.Entity<Episode>()
                    .HasKey(e => new { e.SubjectId, e.EpisodeNum });

        // 联合主键：EpisodeCold（冷表）
        modelBuilder.Entity<EpisodeCold>()
                    .HasKey(e => new { e.SubjectId, e.EpisodeNum });

        // 联合主键：EpisodeVeryCold（极冷表）
        modelBuilder.Entity<EpisodeVeryCold>()
                    .HasKey(e => new { e.SubjectId, e.EpisodeNum });
    }
}
