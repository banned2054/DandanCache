using System.Security.Cryptography;
using System.Text;

namespace DanmakuUpdate.Utils;

internal class DandanUtils
{
    public static string GenerateSignature(string appId, long timestamp, string path, string appSecret)
    {
        using var sha256 = SHA256.Create();

        var data = appId + timestamp + path + appSecret;
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
