using GetBangumiInfo.Database;
using GetBangumiInfo.Models.Danmaku;
using GetBangumiInfo.Utils.Api;
using Minio;
using Minio.DataModel.Args;

namespace DanmakuUpdate;

internal class Program
{
    private static readonly string? DandanAppId     = Environment.GetEnvironmentVariable("DandanAppId");
    private static readonly string? DandanAppSecret = Environment.GetEnvironmentVariable("DandanAppSecret");

    private static readonly string? R2AccessKeyId     = Environment.GetEnvironmentVariable("R2AccessKeyId");
    private static readonly string? R2SecretAccessKey = Environment.GetEnvironmentVariable("R2SecretAccessKey");

    private static readonly string? R2Endpoint =
        Environment.GetEnvironmentVariable("R2Endpoint")?.Replace("https://", "");

    public static string? R2BucketName = "danmaku";

    private static async Task Main(string[] args)
    {
        if (args.Length == 0) return;
        var db = new MyDbContext();

        DotNetEnv.Env.Load();

        if (string.IsNullOrWhiteSpace(DandanAppId))
        {
            throw new Exception("Dandan App Id is empty");
        }

        if (string.IsNullOrWhiteSpace(DandanAppSecret))
        {
            throw new Exception("Dandan App Secret is empty");
        }

        if (string.IsNullOrWhiteSpace(R2AccessKeyId))
        {
            throw new Exception("R2 Access Key Id is empty");
        }

        if (string.IsNullOrWhiteSpace(R2SecretAccessKey))
        {
            throw new Exception("R2 Secret Access Key is empty");
        }

        if (string.IsNullOrWhiteSpace(R2Endpoint))
        {
            throw new Exception("R2 Endpoint is empty");
        }

        if (args[0] == "hot")
        {
            var episodeList = db.EpisodeList.ToList();
            foreach (var episode in episodeList)
            {
                var danmaku = await DandanPlayUtils.GetDanmakuAsync(episode.Id);
                if (danmaku == null)
                {
                    Console.WriteLine($"⚠️ Episode {episode.Id} 没有获取到弹幕");
                    continue;
                }

                var path = $"{episode.SubjectId}/{episode.EpisodeNum}.xml";
                await UploadDanmakuXmlAsync(danmaku!, path);
            }
        }
        else
        {
            var episodeList = db.EpisodeListCold.ToList();
            foreach (var episode in episodeList)
            {
                var danmaku = await DandanPlayUtils.GetDanmakuAsync(episode.Id);
                if (danmaku == null)
                {
                    Console.WriteLine($"⚠️ Episode {episode.Id} 没有获取到弹幕");
                    continue;
                }

                var path = $"{episode.SubjectId}/{episode.EpisodeNum}.xml";
                await UploadDanmakuXmlAsync(danmaku!, path);
            }
        }
    }


    public static async Task UploadDanmakuXmlAsync(ScraperDanmaku danmaku, string objectKey)
    {
        var       xmlBytes = danmaku.ToXml();
        using var stream   = new MemoryStream(xmlBytes);

        var minio = new MinioClient()
                   .WithEndpoint(R2Endpoint)
                   .WithCredentials(R2AccessKeyId, R2SecretAccessKey)
                   .WithSSL()
                   .Build();

        try
        {
            var bucketExists = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(R2BucketName));
            if (!bucketExists)
            {
                Console.WriteLine($"⚠️ R2 bucket '{R2BucketName}' 不存在！");
                return;
            }

            await minio.PutObjectAsync(new PutObjectArgs()
                                      .WithBucket(R2BucketName)
                                      .WithObject(objectKey)
                                      .WithStreamData(stream)
                                      .WithObjectSize(stream.Length)
                                      .WithContentType("application/xml"));

            Console.WriteLine($"✅ 已上传弹幕至 R2: {objectKey}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 上传失败: {ex.Message}");
        }
    }
}
