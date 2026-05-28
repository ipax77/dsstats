using System.Buffers.Binary;

namespace dsstats.play;

public static partial class SpawnPlaybackFactoryNg
{
    private static byte[] CreateUnitRowBytes(ReadOnlySpan<UnitBinaryRow> rows)
    {
        byte[] bytes = AllocateBytes(rows.Length, SpawnPlaybackBinaryPayloads.UnitRowByteStride);
        for (int i = 0; i < rows.Length; i++)
        {
            WriteUnitRow(bytes, i, rows[i]);
        }

        return bytes;
    }

    private static int CompareUnitRows(UnitBinaryRow left, UnitBinaryRow right)
    {
        int spawnComparison = left.SpawnGameloop.CompareTo(right.SpawnGameloop);
        if (spawnComparison != 0)
        {
            return spawnComparison;
        }

        int expiresComparison = left.ExpiresGameloop.CompareTo(right.ExpiresGameloop);
        return expiresComparison != 0
            ? expiresComparison
            : left.UnitIndex.CompareTo(right.UnitIndex);
    }

    private static void WriteUnitRow(
        byte[] bytes,
        int rowIndex,
        UnitBinaryRow row)
    {
        int offset = checked(rowIndex * SpawnPlaybackBinaryPayloads.UnitRowByteStride);
        WriteInt32(bytes, offset, row.UnitIndex);
        WriteInt32(bytes, offset + sizeof(int), row.PlayerIndex);
        WriteInt32(bytes, offset + 2 * sizeof(int), row.UnitKindIndex);
        WriteInt32(bytes, offset + 3 * sizeof(int), row.SpawnNumber);
        WriteInt32(bytes, offset + 4 * sizeof(int), row.SpawnGameloop);
        WriteInt32(bytes, offset + 5 * sizeof(int), row.ExpiresGameloop);
        WriteInt32(bytes, offset + 6 * sizeof(int), row.PathIndex);
        WriteInt32(bytes, offset + 7 * sizeof(int), row.KillOffset);
        WriteInt32(bytes, offset + 8 * sizeof(int), row.KillCount);
        WriteInt32(bytes, offset + 9 * sizeof(int), row.DiedGameloop);
        WriteInt32(bytes, offset + 10 * sizeof(int), row.Flags);
    }

    private static void WriteKillGameloops(byte[] bytes, IReadOnlyList<int> killGameloops, ref int writeIndex)
    {
        for (int i = 0; i < killGameloops.Count; i++)
        {
            WriteInt32(bytes, checked(writeIndex * sizeof(int)), killGameloops[i]);
            writeIndex++;
        }
    }

    private static byte[] CreatePathRowBytes(List<PathKey> paths)
    {
        byte[] bytes = AllocateBytes(paths.Count, SpawnPlaybackBinaryPayloads.PathRowByteStride);
        int pointOffset = 0;
        for (int i = 0; i < paths.Count; i++)
        {
            int offset = i * SpawnPlaybackBinaryPayloads.PathRowByteStride;
            WriteInt32(bytes, offset, pointOffset);
            WriteInt32(bytes, offset + sizeof(int), paths[i].PointCount);
            WriteInt32(bytes, offset + 2 * sizeof(int), 0);
            pointOffset += paths[i].PointCount;
        }

        return bytes;
    }

    private static byte[] CreatePathPointBytes(List<PathKey> paths, int pathPointCount)
    {
        byte[] bytes = AllocateBytes(pathPointCount, SpawnPlaybackBinaryPayloads.PathPointByteStride);
        int pointIndex = 0;
        for (int i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            WritePathPoint(bytes, pointIndex++, path.Point0);
            WritePathPoint(bytes, pointIndex++, path.Point1);
            if (path.PointCount > 2)
            {
                WritePathPoint(bytes, pointIndex++, path.Point2);
            }
            if (path.PointCount > 3)
            {
                WritePathPoint(bytes, pointIndex++, path.Point3);
            }
            if (path.PointCount > 4)
            {
                WritePathPoint(bytes, pointIndex++, path.Point4);
            }
            if (path.PointCount > 5)
            {
                WritePathPoint(bytes, pointIndex++, path.Point5);
            }
        }

        return bytes;
    }

    private static void WritePathPoint(byte[] bytes, int pointIndex, PathPoint point)
    {
        int offset = checked(pointIndex * SpawnPlaybackBinaryPayloads.PathPointByteStride);
        WriteInt32(bytes, offset, point.X);
        WriteInt32(bytes, offset + sizeof(int), point.Y);
        WriteInt32(bytes, offset + 2 * sizeof(int), point.GameloopOffset);
    }

    private static byte[] AllocateBytes(int count, int byteStride)
    {
        return count == 0
            ? []
            : GC.AllocateUninitializedArray<byte>(checked(count * byteStride));
    }

    private static void WriteInt32(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset, sizeof(int)), value);
    }

    private readonly record struct UnitBinaryRow(
        int UnitIndex,
        int PlayerIndex,
        int UnitKindIndex,
        int SpawnNumber,
        int SpawnGameloop,
        int ExpiresGameloop,
        int PathIndex,
        int KillOffset,
        int KillCount,
        int DiedGameloop,
        int Flags);
}
