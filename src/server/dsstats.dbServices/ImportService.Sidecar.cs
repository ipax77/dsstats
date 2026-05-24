using dsstats.shared;
using dsstats.shared.Upload;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices;

public sealed record SpawnPlaybackUploadPayload(
    string PartName,
    long Length,
    Func<Stream> OpenReadStream);

public partial class ImportService
{
    public async Task<ReplayImportBatchResultDto> InsertReplayImportsWithSidecars(
        UploadRequestDto request,
        IReadOnlyList<SpawnPlaybackUploadManifestEntryDto> manifestEntries,
        IReadOnlyDictionary<string, SpawnPlaybackUploadPayload> payloadsByPartName,
        CancellationToken token = default)
    {
        var prepared = await PrepareReplayImportsWithSidecars(
            request,
            manifestEntries,
            payloadsByPartName,
            token);

        if (!prepared.Success)
        {
            return new()
            {
                Success = false,
                Error = prepared.Error,
            };
        }

        await InsertReplayImports(prepared.Imports);
        return new()
        {
            Success = true,
            ReplayHashes = prepared.ReplayHashes,
        };
    }

    private async Task<PreparedReplayImports> PrepareReplayImportsWithSidecars(
        UploadRequestDto request,
        IReadOnlyList<SpawnPlaybackUploadManifestEntryDto> manifestEntries,
        IReadOnlyDictionary<string, SpawnPlaybackUploadPayload> payloadsByPartName,
        CancellationToken token)
    {
        var entriesByReplayHash = new Dictionary<string, SpawnPlaybackUploadManifestEntryDto>(StringComparer.Ordinal);
        foreach (var entry in manifestEntries)
        {
            if (!IsPotentialManifestEntry(entry))
            {
                LogSkippedManifestEntry(entry, payloadLength: null, "Invalid sidecar manifest.");
                continue;
            }

            if (!entriesByReplayHash.ContainsKey(entry.ReplayHash))
            {
                entriesByReplayHash.Add(entry.ReplayHash, entry);
            }
            else
            {
                LogSkippedManifestEntry(entry, payloadLength: null, "Duplicate sidecar manifest entry.");
            }
        }

        List<ReplayImportDto> imports = new(request.Replays.Count);
        List<string> replayHashes = new(request.Replays.Count);
        foreach (var replay in request.Replays)
        {
            var replayHash = replay.ComputeHash();
            replayHashes.Add(replayHash);

            SpawnPlaybackEncodedSidecar? sidecar = null;
            if (entriesByReplayHash.Remove(replayHash, out var entry))
            {
                var error = ValidateManifestEntry(entry);
                if (error is not null)
                {
                    LogSkippedSidecar(replay, replayHash, entry, payloadLength: null, error);
                    imports.Add(new(replay, null));
                    continue;
                }

                if (!payloadsByPartName.TryGetValue(entry.PartName, out var payload))
                {
                    LogSkippedSidecar(replay, replayHash, entry, payloadLength: null, "Missing sidecar payload.");
                    imports.Add(new(replay, null));
                    continue;
                }

                var payloadBytes = await ReadSidecarPayload(entry, payload, token);
                if (!payloadBytes.Success)
                {
                    LogSkippedSidecar(replay, replayHash, entry, payload.Length, payloadBytes.Error!);
                    imports.Add(new(replay, null));
                    continue;
                }

                sidecar = new(
                    payloadBytes.Payload!,
                    entry.CompressedLength,
                    entry.UncompressedLength,
                    entry.UnitCount,
                    entry.FormatVersion,
                    entry.Compression);

                replay.SpawnPlayback = new()
                {
                    Available = true,
                    FormatVersion = entry.FormatVersion,
                    CompressedLength = entry.CompressedLength,
                    UncompressedLength = entry.UncompressedLength,
                    UnitCount = entry.UnitCount,
                };
            }

            imports.Add(new(replay, sidecar));
        }

        foreach (var orphan in entriesByReplayHash.Values)
        {
            payloadsByPartName.TryGetValue(orphan.PartName, out var payload);
            LogSkippedManifestEntry(
                orphan,
                payload?.Length,
                "Replay is not in the upload request.");
        }

        return new()
        {
            Success = true,
            Imports = imports,
            ReplayHashes = replayHashes,
        };
    }

    private void LogSkippedSidecar(
        ReplayDto replay,
        string replayHash,
        SpawnPlaybackUploadManifestEntryDto entry,
        long? payloadLength,
        string reason)
    {
        logger.LogWarning(
            "Skipping spawn playback sidecar for replay {ReplayHash}, file {FileName}, title {Title}, part {PartName}: {Reason} Compression={Compression}, FormatVersion={FormatVersion}, ManifestCompressedLength={CompressedLength}, PayloadLength={PayloadLength}, UncompressedLength={UncompressedLength}, UnitCount={UnitCount}",
            replayHash,
            replay.FileName,
            replay.Title,
            entry.PartName,
            reason,
            entry.Compression,
            entry.FormatVersion,
            entry.CompressedLength,
            payloadLength,
            entry.UncompressedLength,
            entry.UnitCount);
    }

    private void LogSkippedManifestEntry(
        SpawnPlaybackUploadManifestEntryDto entry,
        long? payloadLength,
        string reason)
    {
        logger.LogWarning(
            "Skipping spawn playback sidecar manifest entry for replay {ReplayHash}, part {PartName}: {Reason} Compression={Compression}, FormatVersion={FormatVersion}, ManifestCompressedLength={CompressedLength}, PayloadLength={PayloadLength}, UncompressedLength={UncompressedLength}, UnitCount={UnitCount}",
            entry.ReplayHash,
            entry.PartName,
            reason,
            entry.Compression,
            entry.FormatVersion,
            entry.CompressedLength,
            payloadLength,
            entry.UncompressedLength,
            entry.UnitCount);
    }

    private static bool IsPotentialManifestEntry(SpawnPlaybackUploadManifestEntryDto entry)
    {
        return !string.IsNullOrWhiteSpace(entry.ReplayHash)
            && !string.IsNullOrWhiteSpace(entry.PartName);
    }

    private static string? ValidateManifestEntry(SpawnPlaybackUploadManifestEntryDto entry)
    {
        if (!IsPotentialManifestEntry(entry))
        {
            return "Invalid sidecar manifest.";
        }

        if (entry.FormatVersion != SpawnPlaybackSidecarCodec.FormatVersion
            || !IsSupportedCompression(entry.Compression)
            || entry.CompressedLength <= 0
            || entry.UncompressedLength <= 0
            || entry.UnitCount <= 0)
        {
            return "Invalid sidecar metadata.";
        }

        return null;
    }

    public static bool IsSupportedCompression(SpawnPlaybackCompression compression)
    {
        return compression is SpawnPlaybackCompression.Brotli or SpawnPlaybackCompression.GZip;
    }

    private static async Task<SidecarPayloadReadResult> ReadSidecarPayload(
        SpawnPlaybackUploadManifestEntryDto entry,
        SpawnPlaybackUploadPayload payload,
        CancellationToken token)
    {
        if (payload.Length != entry.CompressedLength)
        {
            return SidecarPayloadReadResult.Failed("Sidecar compressed length does not match payload length.");
        }

        var bytes = new byte[checked((int)payload.Length)];
        await using var stream = payload.OpenReadStream();
        await stream.ReadExactlyAsync(bytes, token);
        return new() { Success = true, Payload = bytes };
    }

    private sealed class PreparedReplayImports
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public List<string> ReplayHashes { get; init; } = [];
        public List<ReplayImportDto> Imports { get; init; } = [];

        public static PreparedReplayImports Failed(string error)
        {
            return new()
            {
                Success = false,
                Error = error,
            };
        }
    }

    private sealed class SidecarPayloadReadResult
    {
        public bool Success { get; init; }
        public byte[]? Payload { get; init; }
        public string? Error { get; init; }

        public static SidecarPayloadReadResult Failed(string error)
        {
            return new()
            {
                Success = false,
                Error = error,
            };
        }
    }
}
