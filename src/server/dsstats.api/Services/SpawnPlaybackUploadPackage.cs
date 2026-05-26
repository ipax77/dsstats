using System.Security.Cryptography;
using System.Text;

namespace dsstats.api.Services;

internal static class SpawnPlaybackUploadPackage
{
    public const string RequestFileName = "request.json.gz";
    public const string ManifestFileName = "manifest.json";
    public const string PayloadExtension = ".sidecar";

    public static string GetPayloadFilePath(string packageDirectory, string partName)
    {
        var fileName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(partName)))
            + PayloadExtension;
        return Path.Combine(packageDirectory, fileName);
    }
}
