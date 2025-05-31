using RestSharp;
using System.Net;
using System.Net.Sockets;

namespace GetBangumiInfo.Utils;

public class NetUtils
{
    // Fetch文本内容
    public static async Task<string> FetchAsync(string                      url,
                                                Dictionary<string, string>? headers     = null,
                                                bool                        enableProxy = false)
    {
        var options = new RestClientOptions
        {
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        };

        var proxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
        if (enableProxy && !string.IsNullOrWhiteSpace(proxy) && IsProxyAvailable(proxy))
        {
            var proxyUri = new Uri(proxy);
            options.Proxy = new WebProxy(proxyUri.Host, proxyUri.Port);
        }

        var client  = new RestClient(options);
        var request = new RestRequest(url.Trim());

        if (headers != null)
        {
            foreach (var (key, value) in headers)
            {
                request.AddHeader(key, value);
            }
        }

        var response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful)
        {
            throw new HttpRequestException($"Failed to fetch URL: {url}, Status: {response.StatusCode}");
        }

        return response.Content ?? string.Empty;
    }

    public static async Task DownloadAsync(string url,
                                           string savePath,
                                           bool   enableProxy = false)
    {
        var options = new RestClientOptions
        {
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        };


        var proxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
        if (enableProxy && !string.IsNullOrWhiteSpace(proxy) && IsProxyAvailable(proxy))
        {
            var proxyUri = new Uri(proxy);
            options.Proxy = new WebProxy(proxyUri.Host, proxyUri.Port);
        }

        var client  = new RestClient(options);
        var request = new RestRequest(url);

        var response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful || response.RawBytes == null)
        {
            throw new HttpRequestException($"Download failed: {url}, Status: {response.StatusCode}");
        }

        var data = response.RawBytes;

        if (string.IsNullOrWhiteSpace(savePath)) return; // 未指定路径，返回数据
        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllBytesAsync(savePath, data);
    }

    public static bool IsValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsProxyAvailable(string proxy)
    {
        try
        {
            var       uri    = new Uri(proxy);
            using var client = new TcpClient();
            var       task   = client.ConnectAsync(uri.Host, uri.Port);
            return task.Wait(1000) && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
