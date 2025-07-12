using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using GetBangumiInfo.Database;
using GetBangumiInfo.Models.Danmaku;
using GetBangumiInfo.Utils.Api;

namespace DanmakuUpdate;

internal class Program
{
    private static readonly string? DandanAppId     = Environment.GetEnvironmentVariable("DandanAppId");
    private static readonly string? DandanAppSecret = Environment.GetEnvironmentVariable("DandanAppSecret");

    private static readonly string? R2AccessKeyId     = Environment.GetEnvironmentVariable("R2AccessKeyId");
    private static readonly string? R2SecretAccessKey = Environment.GetEnvironmentVariable("R2SecretAccessKey");
    private static readonly string? R2Endpoint        = Environment.GetEnvironmentVariable("R2Endpoint");
    public static           string? R2BucketName      = "danmaku";

    private static AmazonS3Client? _client;

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

        _client = new AmazonS3Client(
                                     R2AccessKeyId,
                                     R2SecretAccessKey,
                                     new AmazonS3Config
                                     {
                                         ServiceURL     = R2Endpoint,
                                         ForcePathStyle = true,
                                     });

        if (args[0] == "hot")
        {
            var episodeList = db.EpisodeList.ToList();
            foreach (var episode in episodeList)
            {
                var danmaku = await DandanPlayUtils.GetDanmakuAsync(episode.Id);
                var path    = $"{episode.SubjectId}/{episode.EpisodeNum}.xml";
                await UploadDanmakuXmlAsync(danmaku!, path);
            }
        }
        else
        {
            var episodeList = db.EpisodeListCold.ToList();
            foreach (var episode in episodeList)
            {
                var danmaku = await DandanPlayUtils.GetDanmakuAsync(episode.Id);
                var path    = $"{episode.SubjectId}/{episode.EpisodeNum}.xml";
                await UploadDanmakuXmlAsync(danmaku!, path);
            }
        }
    }

    public static async Task UploadDanmakuXmlAsync(ScraperDanmaku danmaku, string objectKey)
    {
        var xmlBytes = danmaku.ToXml();

        var request = new PutObjectRequest
        {
            BucketName  = R2BucketName,
            Key         = objectKey,
            ContentType = "application/xml",
            ContentBody = Encoding.UTF8.GetString(xmlBytes),
        };

        var response = await _client!.PutObjectAsync(request);

        Console.WriteLine(response.HttpStatusCode == System.Net.HttpStatusCode.OK
                              ? $"✅ 成功上传至 R2：{objectKey}"
                              : $"❌ 上传失败 ({response.HttpStatusCode}): {objectKey}");
    }
}
