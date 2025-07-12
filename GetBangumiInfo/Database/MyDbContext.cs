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

        optionsBuilder.UseNpgsql(connectSetting, opt =>
        {
            opt.EnableRetryOnFailure(3); // 最多重试3次
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mapping>()
                    .Property(e => e.AirDate)
                    .HasConversion(
                                   v => v.HasValue ? v.Value.ToUniversalTime() : default,
                                   v => new DateTimeOffset(v.DateTime, TimeSpan.FromHours(8))
                                  );
    }
}
