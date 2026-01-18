using dsstats.shared.Upload;
using System.IO.Compression;
using System.Text.Json;

namespace dsstats.api.Services;

public partial class UploadService
{
    private readonly string blobBaseDir = "/data/ds/replayblobs";
    private readonly string replayBaseDir = "/data/ds/replayblobs";

    private async Task<string> StoreBlob(UploadDto upload)
    {
        var appDir = Path.Combine(blobBaseDir, upload.AppGuid.ToString("N"));
        Directory.CreateDirectory(appDir);
        var blobFileName = $"{Guid.NewGuid():N}.blob";
        var blobFilePath = Path.Combine(appDir, blobFileName);
        var blobBytes = Convert.FromBase64String(upload.Base64ReplayBlob);
        await File.WriteAllBytesAsync(blobFilePath, blobBytes);
        return blobFilePath;
    }

    private async Task<string> StoreBlob(UploadRequestDto request)
    {
        var appDir = Path.Combine(blobBaseDir, request.AppGuid.ToString("N"));
        Directory.CreateDirectory(appDir);

        var fileName = $"{Guid.NewGuid():N}.json.gz";
        var filePath = Path.Combine(appDir, fileName);

        await using var fs = File.Create(filePath);
        await using var gz = new GZipStream(fs, CompressionLevel.Fastest);
        await JsonSerializer.SerializeAsync(gz, request.Replays);

        return filePath;
    }

    private async Task<string> StoreReplay(Guid guid, IFormFile file)
    {
        var appDir = Path.Combine(replayBaseDir);
        Directory.CreateDirectory(appDir);
        var blobFileName = $"{guid}_{Guid.NewGuid():N}.SC2Replay";
        var replayFilePath = Path.Combine(appDir, blobFileName);

        using (var stream = File.Create(replayFilePath))
        {
            await file.CopyToAsync(stream);
        }

        return replayFilePath;
    }
}
