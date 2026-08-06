using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;

namespace dsstats.service.Services;

internal sealed partial class DsstatsService
{
#pragma warning disable CA2234 // Pass system uri objects instead of strings

    public async Task<bool> Update(CancellationToken ct)
    {
        var config = await GetConfig();
        if (!config.CheckForUpdates)
        {
            return false;
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient("update");
            (var latestVersion, var sha256hash) = await GetLatestVersion(httpClient, ct);

            if (latestVersion <= CurrentVersion)
            {
                return false;
            }
            logger.LogWarning("New version available {latestVersion}", latestVersion.ToString());
            var msiFilePath = Path.Combine(appFolder, "dsstats.installer.msi");
            if (!await DownloadInstaller(httpClient, msiFilePath, sha256hash, ct))
            {
                logger.LogError("Update msi file integrity check failed.");
                return false;
            }

            var startInfo = new ProcessStartInfo("msiexec")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("/i");
            startInfo.ArgumentList.Add(msiFilePath);
            startInfo.ArgumentList.Add("/quiet");
            startInfo.ArgumentList.Add("/norestart");

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                logger.LogError("Failed starting the update installer.");
                return false;
            }

            logger.LogWarning("Update installer started. Suspending replay processing for service upgrade.");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError("Update failed: {error}", ex.Message);
            return false;
        }
    }

    internal static async Task<bool> DownloadInstaller(
        HttpClient httpClient,
        string destinationPath,
        string expectedSha256Hash,
        CancellationToken ct)
    {
        var downloadPath = destinationPath + ".download";
        try
        {
            using var response = await httpClient.GetAsync(
                "dsstats.installer.msi",
                HttpCompletionOption.ResponseHeadersRead,
                ct);
            response.EnsureSuccessStatusCode();

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            await using var destination = new FileStream(
                downloadPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 65_536,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = ArrayPool<byte>.Shared.Rent(65_536);

            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer, ct)) > 0)
                {
                    hash.AppendData(buffer, 0, bytesRead);
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            await destination.FlushAsync(ct);
            var actualSha256Hash = Convert.ToHexString(hash.GetHashAndReset());
            if (!string.Equals(actualSha256Hash, expectedSha256Hash, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            destination.Close();
            File.Move(downloadPath, destinationPath, overwrite: true);
            return true;
        }
        finally
        {
            TryDelete(downloadPath);
        }
    }

    private async Task<(Version, string)> GetLatestVersion(HttpClient httpClient, CancellationToken ct)
    {
        try
        {
            var stream = await httpClient.GetStreamAsync("latest.yml", ct);

            using var reader = new StreamReader(stream);
            var versionInfo = await reader.ReadLineAsync(ct);

            if (Version.TryParse(GetMetadataValue(versionInfo), out var version))
            {
                if (CurrentVersion < version)
                {
                    var hashInfo = await reader.ReadLineAsync(ct);
                    return (version, GetMetadataValue(hashInfo) ?? "");
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

    private static string? GetMetadataValue(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var separatorIndex = line.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex < 0 ? null : line[(separatorIndex + 1)..].Trim();
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
#pragma warning restore CA2234 // Pass system uri objects instead of strings

}
