using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
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
        var xmlContent = Encoding.UTF8.GetString(danmaku.ToXml());

        const string region  = "auto"; // R2固定
        const string service = "s3";

        var now         = DateTime.UtcNow;
        var host        = $"{R2BucketName}.{new Uri(R2Endpoint!).Host}";
        var uriPath     = $"/{objectKey}";
        var payloadHash = ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(xmlContent)));

        var headers = new Dictionary<string, string>
        {
            ["host"]                 = host,
            ["x-amz-content-sha256"] = payloadHash,
            ["x-amz-date"]           = now.ToString("yyyyMMddTHHmmssZ")
        };

        var signedHeaders = string.Join(";", headers.Keys.OrderBy(k => k));
        var canonicalHeaders = string.Join("", headers.OrderBy(kv => kv.Key)
                                                      .Select(kv => $"{kv.Key}:{kv.Value}\n"));

        var canonicalRequest       = $"PUT\n{uriPath}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        var hashedCanonicalRequest = ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));

        var dateStamp    = now.ToString("yyyyMMdd");
        var scope        = $"{dateStamp}/{region}/{service}/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{headers["x-amz-date"]}\n{scope}\n{hashedCanonicalRequest}";

        var signingKey = GetSignatureKey(R2SecretAccessKey!, dateStamp, region, service);
        var signature  = ToHex(HmacSha256(signingKey, stringToSign));

        var authorization =
            $"AWS4-HMAC-SHA256 Credential={R2AccessKeyId}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}";

        // 构造 HTTP 请求
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Put, $"https://{host}{uriPath}")
        {
            Content = new StringContent(xmlContent, Encoding.UTF8, "application/xml")
        };

        foreach (var kv in headers)
            request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        request.Headers.TryAddWithoutValidation("Authorization", authorization);

        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"✅ 成功上传至 R2：{objectKey}");
        }
        else
        {
            var err = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ 上传失败 ({response.StatusCode}): {objectKey}\n{err}");
        }
    }

    // 签名辅助方法
    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static byte[] GetSignatureKey(string secretKey, string date, string region, string service)
    {
        var kDate    = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secretKey), date);
        var kRegion  = HmacSha256(kDate, region);
        var kService = HmacSha256(kRegion, service);
        var kSigning = HmacSha256(kService, "aws4_request");
        return kSigning;
    }

    private static string ToHex(byte[] bytes) =>
        BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
}
