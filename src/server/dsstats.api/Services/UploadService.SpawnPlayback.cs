using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Upload;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text.Json;

namespace dsstats.api.Services;

public partial class UploadService
{
    private static readonly JsonSerializerOptions UploadJsonOptions = new(JsonSerializerDefaults.Web);
    private const string SingleSpawnPlaybackSidecarPartName = "sidecar";

    public async Task<ReplayImportResultDto> ProcessSpawnPlaybackUploadAsync(
        string? replay,
        IFormFile sidecar,
        ushort formatVersion,
        SpawnPlaybackCompression compression,
        int compressedLength,
        int uncompressedLength,
        int unitCount,
        CancellationToken token)
    {
        if (sidecar.Length == 0)
        {
            return FailedSingle("Invalid sidecar payload.");
        }

        ReplayDto? replayDto;
        try
        {
            replayDto = JsonSerializer.Deserialize<ReplayDto>(replay ?? string.Empty, UploadJsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize spawn playback replay payload.");
            return FailedSingle("Invalid replay payload.");
        }

        if (replayDto is null)
        {
            return FailedSingle("Invalid replay payload.");
        }

        if (sidecar.Length != compressedLength)
        {
            return FailedSingle("Sidecar compressed length does not match payload length.");
        }

        if (formatVersion != SpawnPlaybackSidecarCodec.FormatVersion
            || compressedLength <= 0
            || uncompressedLength <= 0
            || unitCount <= 0)
        {
            return FailedSingle("Invalid sidecar metadata.");
        }

        if (!ImportService.IsSupportedCompression(compression))
        {
            return FailedSingle("Unsupported sidecar compression.");
        }

        var replayHash = replayDto.ComputeHash();
        var uploadRequest = new UploadRequestDto
        {
            AppGuid = Guid.Empty,
            Replays = [replayDto],
        };
        SpawnPlaybackUploadManifestEntryDto[] manifest =
        [
            new()
            {
                ReplayHash = replayHash,
                PartName = SingleSpawnPlaybackSidecarPartName,
                FormatVersion = formatVersion,
                Compression = compression,
                CompressedLength = compressedLength,
                UncompressedLength = uncompressedLength,
                UnitCount = unitCount,
            }
        ];

        var packageDirectory = CreateSpawnPlaybackPackageDirectory();
        try
        {
            await StoreSpawnPlaybackRequest(packageDirectory, uploadRequest, token);
            await StoreSpawnPlaybackManifest(packageDirectory, manifest, token);
            await StoreSpawnPlaybackSidecar(packageDirectory, SingleSpawnPlaybackSidecarPartName, sidecar, token);

            await QueueSpawnPlaybackPackage(uploadRequest, packageDirectory, token);
            return new()
            {
                Success = true,
                ReplayHash = replayHash,
            };
        }
        catch (OperationCanceledException)
        {
            TryDeleteDirectory(packageDirectory);
            throw;
        }
        catch (Exception ex)
        {
            TryDeleteDirectory(packageDirectory);
            logger.LogError(ex, "Error queueing spawn playback upload");
            return FailedSingle("Unknown Error");
        }
    }

    public async Task<ReplayImportBatchResultDto> ProcessSpawnPlaybackUploadAsync(
        IFormFile? request,
        string? manifest,
        IFormFileCollection files,
        CancellationToken token)
    {
        if (request is null || request.Length == 0)
        {
            return FailedBatch("Invalid replay payload.");
        }

        if (string.IsNullOrWhiteSpace(manifest))
        {
            return FailedBatch("Missing sidecar manifest.");
        }

        try
        {
            JsonSerializer.Deserialize<List<SpawnPlaybackUploadManifestEntryDto>>(
                manifest,
                UploadJsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize spawn playback import manifest.");
            return FailedBatch("Invalid sidecar manifest json.");
        }

        List<IFormFile> sidecarFiles = [];
        HashSet<string> payloadPartNames = new(StringComparer.Ordinal);
        foreach (var file in files)
        {
            if (string.Equals(file.Name, "request", StringComparison.Ordinal))
            {
                continue;
            }

            if (!payloadPartNames.Add(file.Name))
            {
                return FailedBatch("Duplicate sidecar payload.");
            }

            sidecarFiles.Add(file);
        }

        var packageDirectory = CreateSpawnPlaybackPackageDirectory();

        try
        {
            var requestPath = Path.Combine(packageDirectory, SpawnPlaybackUploadPackage.RequestFileName);
            await using (var requestFile = File.Create(requestPath))
            {
                await request.CopyToAsync(requestFile, token);
            }

            UploadRequestDto? uploadRequest;
            try
            {
                await using var requestStream = File.OpenRead(requestPath);
                await using var gzip = new GZipStream(requestStream, CompressionMode.Decompress);
                uploadRequest = await JsonSerializer.DeserializeAsync<UploadRequestDto>(
                    gzip,
                    UploadJsonOptions,
                    token);
            }
            catch (InvalidDataException ex)
            {
                logger.LogWarning(ex, "Failed to decompress spawn playback import request.");
                return FailedBatchWithCleanup(packageDirectory, "Invalid replay payload gzip stream.");
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to deserialize spawn playback import request.");
                return FailedBatchWithCleanup(packageDirectory, "Invalid replay payload json.");
            }

            if (uploadRequest is null)
            {
                return FailedBatchWithCleanup(packageDirectory, "Invalid replay payload.");
            }

            await StoreSpawnPlaybackManifest(packageDirectory, manifest, token);

            foreach (var file in sidecarFiles)
            {
                await StoreSpawnPlaybackSidecar(packageDirectory, file.Name, file, token);
            }

            await QueueSpawnPlaybackPackage(uploadRequest, packageDirectory, token);
            return new()
            {
                Success = true,
                ReplayHashes = uploadRequest.Replays.Select(replay => replay.ComputeHash()).ToList(),
            };
        }
        catch (OperationCanceledException)
        {
            TryDeleteDirectory(packageDirectory);
            throw;
        }
        catch (Exception ex)
        {
            TryDeleteDirectory(packageDirectory);
            logger.LogError(ex, "Error queueing spawn playback upload");
            return FailedBatch("Unknown Error");
        }
    }

    private string CreateSpawnPlaybackPackageDirectory()
    {
        var packageDirectory = Path.Combine(
            storageOptions.BlobBaseDir,
            "spawn-playbacks",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packageDirectory);
        return packageDirectory;
    }

    private static async Task StoreSpawnPlaybackRequest(
        string packageDirectory,
        UploadRequestDto uploadRequest,
        CancellationToken token)
    {
        var requestPath = Path.Combine(packageDirectory, SpawnPlaybackUploadPackage.RequestFileName);
        await using var fs = File.Create(requestPath);
        await using var gz = new GZipStream(fs, CompressionLevel.Fastest);
        await JsonSerializer.SerializeAsync(gz, uploadRequest, UploadJsonOptions, token);
    }

    private static Task StoreSpawnPlaybackManifest(
        string packageDirectory,
        IReadOnlyList<SpawnPlaybackUploadManifestEntryDto> manifest,
        CancellationToken token)
    {
        var manifestJson = JsonSerializer.Serialize(manifest, UploadJsonOptions);
        return StoreSpawnPlaybackManifest(packageDirectory, manifestJson, token);
    }

    private static Task StoreSpawnPlaybackManifest(
        string packageDirectory,
        string manifest,
        CancellationToken token)
    {
        var manifestPath = Path.Combine(packageDirectory, SpawnPlaybackUploadPackage.ManifestFileName);
        return File.WriteAllTextAsync(manifestPath, manifest, token);
    }

    private static async Task StoreSpawnPlaybackSidecar(
        string packageDirectory,
        string partName,
        IFormFile sidecar,
        CancellationToken token)
    {
        var payloadPath = SpawnPlaybackUploadPackage.GetPayloadFilePath(packageDirectory, partName);
        await using var payloadFile = File.Create(payloadPath);
        await sidecar.CopyToAsync(payloadFile, token);
    }

    private async Task QueueSpawnPlaybackPackage(
        UploadRequestDto uploadRequest,
        string packageDirectory,
        CancellationToken token)
    {
        List<int> playerIds = [];
        foreach (var requestName in uploadRequest.RequestNames)
        {
            var playerId = importService.GetOrCreatePlayerId(
                requestName.Name,
                requestName.RegionId,
                requestName.RealmId,
                requestName.ToonId);
            playerIds.Add(playerId);
        }

        await using var context = await contextFactory.CreateDbContextAsync(token);
        var uploadJob = new UploadJob
        {
            PlayerIds = playerIds.ToArray(),
            Version = uploadRequest.AppVersion,
            BlobFilePath = packageDirectory,
            CreatedAt = DateTime.UtcNow,
        };
        context.UploadJobs.Add(uploadJob);
        await context.SaveChangesAsync(token);

        await uploadChannel.Writer.WriteAsync(uploadJob, token);
    }

    private static ReplayImportResultDto FailedSingle(string error)
    {
        return new()
        {
            Success = false,
            Error = error,
        };
    }

    private static ReplayImportBatchResultDto FailedBatch(string error)
    {
        return new()
        {
            Success = false,
            Error = error,
        };
    }

    private static ReplayImportBatchResultDto FailedBatchWithCleanup(string packageDirectory, string error)
    {
        TryDeleteDirectory(packageDirectory);
        return FailedBatch(error);
    }

    private static void TryDeleteDirectory(string packageDirectory)
    {
        try
        {
            if (Directory.Exists(packageDirectory))
            {
                Directory.Delete(packageDirectory, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup after a failed request.
        }
    }
}
