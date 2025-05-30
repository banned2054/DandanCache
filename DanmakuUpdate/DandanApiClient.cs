using DanmakuUpdate.Utils;
using RestSharp;

namespace DanmakuUpdate;

internal class DandanApiClient
{
    private readonly RestClient _client;
    private readonly string     _appId;
    private readonly string     _appSecret;

    public DandanApiClient(string appId, string appSecret)
    {
        _client    = new RestClient("https://api.dandanplay.net");
        _appId     = appId;
        _appSecret = appSecret;
    }

    public async Task<string> GetDanmakuAsync(int episodeId)
    {
        var path      = $"/api/v2/comment/{episodeId}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = DandanUtils.GenerateSignature(_appId, timestamp, path, _appSecret);

        var request = new RestRequest(path);
        request.AddHeader("X-AppId", _appId);
        request.AddHeader("X-Timestamp", timestamp.ToString());
        request.AddHeader("X-Signature", signature);

        var response = await _client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            throw new Exception($"API 请求失败: {response.StatusCode} - {response.Content}");
        }

        return response.Content!;
    }
}
