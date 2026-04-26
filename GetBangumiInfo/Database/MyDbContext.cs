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
        var host     = Environment.GetEnvironmentVariable("DatabaseHost");
        var port     = Environment.GetEnvironmentVariable("DatabasePort");
        var table    = Environment.GetEnvironmentVariable("DatabaseTable");
        var username = Environment.GetEnvironmentVariable("DatabaseUsername");
        var password = Environment.GetEnvironmentVariable("DatabasePassword");

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new Exception("Database host not found.");
        }

        if (string.IsNullOrWhiteSpace(port))
        {
            throw new Exception("Database port not found.");
        }

        if (string.IsNullOrWhiteSpace(table))
        {
            throw new Exception("Database table not found.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new Exception("Database username not found.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new Exception("Database password not found.");
        }

        var connectSetting =
            $"Host={host};"         +
            $"Port={port};"         +
            $"Database={table};"    +
            $"Username={username};" +
            $"Password={password};" +
            $"SSL Mode=Require;Trust Server Certificate=true";
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
