using System.Buffers.Binary;

namespace dsstats.play;

internal static class SpawnPlaybackBinaryPayloads
{
    public const string UnitRowsDatasetId = "unitRows";
    public const string PathRowsDatasetId = "pathRows";
    public const string PathPointsDatasetId = "pathPoints";
    public const string KillGameloopsDatasetId = "killGameloops";

    public const int UnitRowIntCount = 11;
    public const int UnitRowByteStride = UnitRowIntCount * sizeof(int);
    public const int UnitRowUnitIndexOffset = 0;
    public const int UnitRowPlayerIndexOffset = 1;
    public const int UnitRowUnitKindIndexOffset = 2;
    public const int UnitRowSpawnNumberOffset = 3;
    public const int UnitRowSpawnGameloopOffset = 4;
    public const int UnitRowExpiresGameloopOffset = 5;
    public const int UnitRowKillOffsetOffset = 7;
    public const int UnitRowKillCountOffset = 8;

    public static SpawnPlaybackReplayNgMetadata CreateMetadata(SpawnPlaybackReplayNg replay)
    {
        var payloads = replay.BinaryPayloads;
        var metadata = new SpawnPlaybackBinaryPayloadMetadataNg[payloads.Count];
        for (int i = 0; i < payloads.Count; i++)
        {
            var payload = payloads[i];
            metadata[i] = new(
                payload.DatasetId,
                payload.Count,
                payload.Format,
                payload.XOffset,
                payload.YOffset,
                payload.ByteStride);
        }

        return new(
            replay.DurationGameloop,
            replay.StepGameloops,
            replay.Bounds,
            replay.Stats,
            replay.Summary,
            replay.MiddleControl,
            replay.Landmarks,
            replay.Snapshots,
            replay.Players,
            replay.UnitKinds,
            metadata);
    }

    public static byte[] GetPayloadBytes(SpawnPlaybackReplayNg replay, string datasetId)
    {
        return GetPayload(replay, datasetId)?.Bytes ?? [];
    }

    public static SpawnPlaybackBinaryPayloadNg? GetPayload(SpawnPlaybackReplayNg replay, string datasetId)
    {
        foreach (var payload in replay.BinaryPayloads)
        {
            if (string.Equals(payload.DatasetId, datasetId, StringComparison.Ordinal))
            {
                return payload;
            }
        }

        return null;
    }

    public static int ReadUnitRowInt(ReadOnlySpan<byte> bytes, int rowIndex, int valueOffset)
    {
        return ReadInt32(bytes, checked(rowIndex * UnitRowByteStride + valueOffset * sizeof(int)));
    }

    public static int ReadInt32(ReadOnlySpan<byte> bytes, int offset)
    {
        return offset >= 0 && offset + sizeof(int) <= bytes.Length
            ? BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(offset, sizeof(int)))
            : 0;
    }
}
