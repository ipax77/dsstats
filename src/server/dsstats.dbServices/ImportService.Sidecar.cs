using dsstats.shared;
using dsstats.shared.Upload;

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

    private static async Task<PreparedReplayImports> PrepareReplayImportsWithSidecars(
        UploadRequestDto request,
        IReadOnlyList<SpawnPlaybackUploadManifestEntryDto> manifestEntries,
        IReadOnlyDictionary<string, SpawnPlaybackUploadPayload> payloadsByPartName,
        CancellationToken token)
    {
        var entriesByReplayHash = new Dictionary<string, SpawnPlaybackUploadManifestEntryDto>(StringComparer.Ordinal);
        foreach (var entry in manifestEntries)
        {
            var error = ValidateManifestEntry(entry);
            if (error is not null)
            {
                return PreparedReplayImports.Failed(error);
            }

            if (!entriesByReplayHash.TryAdd(entry.ReplayHash, entry))
            {
                return PreparedReplayImports.Failed("Duplicate sidecar manifest entry.");
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
                if (!payloadsByPartName.TryGetValue(entry.PartName, out var payload))
                {
                    return PreparedReplayImports.Failed("Missing sidecar payload.");
                }

                var payloadBytes = await ReadSidecarPayload(entry, payload, token);
                if (!payloadBytes.Success)
                {
                    return PreparedReplayImports.Failed(payloadBytes.Error!);
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

        if (entriesByReplayHash.Count > 0)
        {
            return PreparedReplayImports.Failed("Sidecar manifest contains a replay hash that is not in the upload request.");
        }

        return new()
        {
            Success = true,
            Imports = imports,
            ReplayHashes = replayHashes,
        };
    }

    private static string? ValidateManifestEntry(SpawnPlaybackUploadManifestEntryDto entry)
    {
        if (string.IsNullOrWhiteSpace(entry.ReplayHash)
            || string.IsNullOrWhiteSpace(entry.PartName))
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
