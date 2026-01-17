using System.Diagnostics;
using System.Security.Cryptography;

namespace dsstats.service.Services;

internal sealed partial class DsstatsService
{
#pragma warning disable CA2234 // Pass system uri objects instead of strings

    public async Task Update(CancellationToken ct)
    {
        var config = await GetConfig();
        if (!config.CheckForUpdates)
        {
            return;
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient("update");
            (var latestVersion, var sha256hash) = await GetLatestVersion(httpClient, ct);

            if (latestVersion <= CurrentVersion)
            {
                return;
            }
            logger.LogWarning("New version available {latestVersion}", latestVersion.ToString());
            byte[] binfileBytes = await httpClient.GetByteArrayAsync("dsstats.installer.msi", ct);

            if (!CheckHash(binfileBytes, sha256hash))
            {
                logger.LogError("Update msi file integrity check failed.");
                return;
            }

            var msiFilePath = Path.Combine(appFolder, "dsstats.installer.msi");
            await File.WriteAllBytesAsync(msiFilePath, binfileBytes, ct);

#pragma warning disable CA2000 // Dispose objects before losing scope
            var process = new Process();
#pragma warning restore CA2000 // Dispose objects before losing scope
            process.StartInfo.FileName = "msiexec";
            process.StartInfo.Arguments = "/i " + msiFilePath;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("Update failed: {error}", ex.Message);
        }
    }

    private static bool CheckHash(byte[] binfileBytes, string sha256hash)
    {
        var fileHash = SHA256.HashData(binfileBytes);
        string hash = Convert.ToHexString(fileHash);
        return string.Equals(hash, sha256hash, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(Version, string)> GetLatestVersion(HttpClient httpClient, CancellationToken ct)
    {
        try
        {
            var stream = await httpClient.GetStreamAsync("latest.yml", ct);

            using var reader = new StreamReader(stream);
            var versionInfo = await reader.ReadLineAsync(ct);

            if (versionInfo != null
                && Version.TryParse(versionInfo.Split(' ').LastOrDefault(), out var version))
            {
                if (CurrentVersion < version)
                {
                    var hashInfo = await reader.ReadLineAsync(ct);
                    return (version, hashInfo?.Split(' ').LastOrDefault() ?? "");
                }
                else
                {
                    return (version, "");
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("Failed getting latest version: {ex}", ex.Message);
        }
        return (new(0, 0, 0), "");
    }
#pragma warning restore CA2234 // Pass system uri objects instead of strings

}