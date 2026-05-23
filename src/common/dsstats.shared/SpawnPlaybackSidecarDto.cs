using System.IO.Compression;
using System.Text;

namespace dsstats.shared;

public enum SpawnPlaybackCompression : byte
{
    Brotli = 1
}

public sealed record SpawnPlaybackSidecarDto(
    int DurationGameloop,
    int StepGameloops,
    IReadOnlyList<SpawnPlaybackPlayerSidecarDto> Players,
    IReadOnlyList<SpawnPlaybackSnapshotSidecarDto> Snapshots);

public sealed record SpawnPlaybackPlayerSidecarDto(
    int GamePos,
    IReadOnlyList<SpawnPlaybackUnitSidecarDto> Units);

public sealed record SpawnPlaybackUnitSidecarDto(
    int UnitIndex,
    string Name,
    int SpawnNumber,
    int SpawnGameloop,
    int SpawnX,
    int SpawnY,
    int? DiedGameloop,
    int? DiedX,
    int? DiedY,
    IReadOnlyList<int> KillGameloops);

public sealed record SpawnPlaybackSnapshotSidecarDto(
    int SpawnNumber,
    int StartGameloop,
    int EndGameloop);

public sealed record SpawnPlaybackEncodedSidecar(
    byte[] Payload,
    int CompressedLength,
    int UncompressedLength,
    int UnitCount,
    ushort FormatVersion = SpawnPlaybackSidecarCodec.FormatVersion,
    SpawnPlaybackCompression Compression = SpawnPlaybackSidecarCodec.Compression);

public sealed record ReplayImportDto(
    ReplayDto Replay,
    SpawnPlaybackEncodedSidecar? SpawnPlayback);

public static class SpawnPlaybackSidecarCodec
{
    public const ushort FormatVersion = 2;
    public const SpawnPlaybackCompression Compression = SpawnPlaybackCompression.Brotli;

    private const uint Magic = 0x42505344; // DSPB
    private const byte HasDiedGameloop = 1;
    private const byte HasDiedPosition = 2;
    private const byte HasPackedSpawnPosition = 4;
    private const byte HasPackedDiedPosition = 8;
    private const int SpawnCellBits = 9;
    private const int SpawnCellCount = 403;
    private const int MapPositionBits = 16;
    private const int MapWidth = 256;
    private const int MapHeight = 240;
    private const int MapPositionStride = MapWidth + 1;
    private const int Team2SpawnOffset = 81;
    private const int SpawnMinU = -9;
    private const int SpawnMaxU = 25;
    private const int SpawnMinV = 317;
    private const int SpawnMaxV = 339;

    public static byte[] Encode(SpawnPlaybackSidecarDto dto)
    {
        return EncodeWithMetadata(dto).Payload;
    }

    public static SpawnPlaybackEncodedSidecar EncodeWithMetadata(
        SpawnPlaybackSidecarDto dto,
        CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        ArgumentNullException.ThrowIfNull(dto);
        using var raw = new MemoryStream();
        WriteUncompressed(raw, dto);
        int uncompressedLength = checked((int)raw.Length);

        using var compressed = new MemoryStream();
        using (var brotli = new BrotliStream(compressed, compressionLevel, leaveOpen: true))
        {
            raw.Position = 0;
            raw.CopyTo(brotli);
        }

        byte[] payload = compressed.ToArray();
        return new(payload, payload.Length, uncompressedLength, GetUnitCount(dto));
    }

    public static SpawnPlaybackSidecarDto Decode(ReadOnlySpan<byte> compressed)
    {
        if (compressed.IsEmpty)
        {
            throw new InvalidDataException("Spawn playback sidecar payload is empty.");
        }

        using var source = new MemoryStream(compressed.ToArray());
        using var brotli = new BrotliStream(source, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        brotli.CopyTo(raw);
        raw.Position = 0;

        return ReadUncompressed(raw);
    }

    public static int GetUncompressedLength(SpawnPlaybackSidecarDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        using var raw = new MemoryStream();
        WriteUncompressed(raw, dto);
        return checked((int)raw.Length);
    }

    public static int GetUnitCount(SpawnPlaybackSidecarDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        int count = 0;
        foreach (var player in dto.Players)
        {
            count = checked(count + player.Units.Count);
        }
        return count;
    }

    private static void WriteUncompressed(Stream stream, SpawnPlaybackSidecarDto dto)
    {
        WriteUInt32(stream, Magic);
        WriteVarUInt(stream, FormatVersion);
        WriteVarUInt(stream, (uint)Math.Max(0, dto.DurationGameloop));
        WriteVarUInt(stream, (uint)Math.Max(0, dto.StepGameloops));

        var stringIds = CreateStringTable(dto, out var strings);
        WriteVarUInt(stream, (uint)strings.Count);
        foreach (var value in strings)
        {
            WriteString(stream, value);
        }

        WriteVarUInt(stream, (uint)dto.Players.Count);
        foreach (var player in dto.Players.OrderBy(player => player.GamePos))
        {
            var rows = player.Units
                .OrderBy(unit => unit.SpawnGameloop)
                .ThenBy(unit => unit.UnitIndex)
                .Select(unit => CreateUnitWriteRow(player.GamePos, unit, stringIds[unit.Name]))
                .ToList();

            int packedSpawnCount = 0;
            foreach (var row in rows)
            {
                if ((row.Flags & HasPackedSpawnPosition) != 0)
                {
                    packedSpawnCount++;
                }
            }

            var spawnCellBytes = packedSpawnCount == 0
                ? []
                : new byte[(packedSpawnCount * SpawnCellBits + 7) / 8];
            var spawnCellWriter = new BitWriter(spawnCellBytes);
            foreach (var row in rows)
            {
                if ((row.Flags & HasPackedSpawnPosition) != 0)
                {
                    spawnCellWriter.WriteBits((uint)row.SpawnCellId, SpawnCellBits);
                }
            }
            spawnCellWriter.Flush();

            WriteVarUInt(stream, (uint)Math.Max(0, player.GamePos));
            WriteVarUInt(stream, (uint)rows.Count);
            WriteVarUInt(stream, (uint)spawnCellBytes.Length);
            stream.Write(spawnCellBytes);

            foreach (var row in rows)
            {
                var unit = row.Unit;
                WriteVarUInt(stream, (uint)Math.Max(0, unit.UnitIndex));
                WriteVarUInt(stream, (uint)row.UnitNameId);
                WriteVarUInt(stream, (uint)Math.Max(0, unit.SpawnNumber));
                WriteVarUInt(stream, (uint)Math.Max(0, unit.SpawnGameloop));
                stream.WriteByte(row.Flags);

                if ((row.Flags & HasPackedSpawnPosition) == 0)
                {
                    WriteVarUInt(stream, (uint)Math.Max(0, unit.SpawnX));
                    WriteVarUInt(stream, (uint)Math.Max(0, unit.SpawnY));
                }

                if ((row.Flags & HasDiedGameloop) != 0)
                {
                    WriteVarUInt(stream, (uint)Math.Max(0, unit.DiedGameloop!.Value - unit.SpawnGameloop));
                }
                if ((row.Flags & HasDiedPosition) != 0)
                {
                    if ((row.Flags & HasPackedDiedPosition) != 0)
                    {
                        WriteUInt16(stream, checked((ushort)row.DiedPositionId));
                    }
                    else
                    {
                        WriteVarUInt(stream, (uint)Math.Max(0, unit.DiedX!.Value));
                        WriteVarUInt(stream, (uint)Math.Max(0, unit.DiedY!.Value));
                    }
                }

                WriteGameloopDeltas(stream, unit.SpawnGameloop, unit.KillGameloops);
            }
        }

        WriteVarUInt(stream, (uint)dto.Snapshots.Count);
        foreach (var snapshot in dto.Snapshots.OrderBy(snapshot => snapshot.StartGameloop).ThenBy(snapshot => snapshot.SpawnNumber))
        {
            WriteVarUInt(stream, (uint)Math.Max(0, snapshot.SpawnNumber));
            WriteVarUInt(stream, (uint)Math.Max(0, snapshot.StartGameloop));
            WriteVarUInt(stream, (uint)Math.Max(0, snapshot.EndGameloop - snapshot.StartGameloop));
        }
    }

    private static SpawnPlaybackSidecarDto ReadUncompressed(Stream stream)
    {
        uint magic = ReadUInt32(stream);
        if (magic != Magic)
        {
            throw new InvalidDataException("Invalid spawn playback sidecar payload.");
        }

        ushort version = checked((ushort)ReadVarUInt(stream));
        if (version != FormatVersion)
        {
            throw new NotSupportedException($"Unsupported spawn playback sidecar format version {version}.");
        }

        int durationGameloop = checked((int)ReadVarUInt(stream));
        int stepGameloops = checked((int)ReadVarUInt(stream));

        var strings = new string[checked((int)ReadVarUInt(stream))];
        for (int i = 0; i < strings.Length; i++)
        {
            strings[i] = ReadString(stream);
        }

        var players = new List<SpawnPlaybackPlayerSidecarDto>(checked((int)ReadVarUInt(stream)));
        for (int playerIndex = 0; playerIndex < players.Capacity; playerIndex++)
        {
            int gamePos = checked((int)ReadVarUInt(stream));
            var units = new List<SpawnPlaybackUnitSidecarDto>(checked((int)ReadVarUInt(stream)));
            int spawnCellByteCount = checked((int)ReadVarUInt(stream));
            var spawnCellBytes = new byte[spawnCellByteCount];
            stream.ReadExactly(spawnCellBytes);
            var spawnCellReader = new BitReader(spawnCellBytes);

            for (int unitIndex = 0; unitIndex < units.Capacity; unitIndex++)
            {
                int unitSpawnIndex = checked((int)ReadVarUInt(stream));
                int unitId = checked((int)ReadVarUInt(stream));
                if ((uint)unitId >= (uint)strings.Length)
                {
                    throw new InvalidDataException("Invalid unit string table id.");
                }

                string name = strings[unitId];
                int spawnNumber = checked((int)ReadVarUInt(stream));
                int spawnGameloop = checked((int)ReadVarUInt(stream));
                int flags = stream.ReadByte();
                if (flags < 0)
                {
                    throw new EndOfStreamException();
                }

                int spawnX;
                int spawnY;
                if ((flags & HasPackedSpawnPosition) != 0)
                {
                    (spawnX, spawnY) = GetSpawnPositionFromCellId(gamePos, checked((int)spawnCellReader.ReadBits(SpawnCellBits)));
                }
                else
                {
                    spawnX = checked((int)ReadVarUInt(stream));
                    spawnY = checked((int)ReadVarUInt(stream));
                }

                int? diedGameloop = null;
                if ((flags & HasDiedGameloop) != 0)
                {
                    diedGameloop = checked(spawnGameloop + (int)ReadVarUInt(stream));
                }

                int? diedX = null;
                int? diedY = null;
                if ((flags & HasDiedPosition) != 0)
                {
                    if ((flags & HasPackedDiedPosition) != 0)
                    {
                        int positionId = ReadUInt16(stream);
                        diedX = positionId % MapPositionStride;
                        diedY = positionId / MapPositionStride;
                    }
                    else
                    {
                        diedX = checked((int)ReadVarUInt(stream));
                        diedY = checked((int)ReadVarUInt(stream));
                    }
                }

                units.Add(new(
                    unitSpawnIndex,
                    name,
                    spawnNumber,
                    spawnGameloop,
                    spawnX,
                    spawnY,
                    diedGameloop,
                    diedX,
                    diedY,
                    ReadGameloopDeltas(stream, spawnGameloop)));
            }

            players.Add(new(gamePos, units));
        }

        var snapshots = new List<SpawnPlaybackSnapshotSidecarDto>(checked((int)ReadVarUInt(stream)));
        for (int i = 0; i < snapshots.Capacity; i++)
        {
            int spawnNumber = checked((int)ReadVarUInt(stream));
            int startGameloop = checked((int)ReadVarUInt(stream));
            int duration = checked((int)ReadVarUInt(stream));
            snapshots.Add(new(spawnNumber, startGameloop, checked(startGameloop + duration)));
        }

        return new(durationGameloop, stepGameloops, players, snapshots);
    }

    private static Dictionary<string, int> CreateStringTable(SpawnPlaybackSidecarDto dto, out List<string> strings)
    {
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var unit in dto.Players.SelectMany(player => player.Units))
        {
            if (!ids.ContainsKey(unit.Name))
            {
                ids[unit.Name] = ids.Count;
            }
        }

        strings = ids.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToList();
        return ids;
    }

    private static UnitWriteRow CreateUnitWriteRow(
        int gamePos,
        SpawnPlaybackUnitSidecarDto unit,
        int unitNameId)
    {
        byte flags = 0;
        int spawnCellId = 0;
        int diedPositionId = 0;
        if (unit.DiedGameloop is not null)
        {
            flags |= HasDiedGameloop;
        }
        if (unit.DiedX is not null && unit.DiedY is not null)
        {
            flags |= HasDiedPosition;
        }
        if (TryGetSpawnPositionCellId(gamePos, unit.SpawnX, unit.SpawnY, out spawnCellId))
        {
            flags |= HasPackedSpawnPosition;
        }
        if ((flags & HasDiedPosition) != 0
            && TryGetMapPositionId(unit.DiedX!.Value, unit.DiedY!.Value, out diedPositionId))
        {
            flags |= HasPackedDiedPosition;
        }

        return new(unit, unitNameId, flags, spawnCellId, diedPositionId);
    }

    public static bool TryGetSpawnPositionCellId(int gamePos, int x, int y, out int cellId)
    {
        if (gamePos >= 4)
        {
            x += Team2SpawnOffset;
            y += Team2SpawnOffset;
        }

        int u = x - y;
        int v = x + y;
        if (u < SpawnMinU || u > SpawnMaxU || v < SpawnMinV || v > SpawnMaxV || ((u + v) & 1) != 0)
        {
            cellId = 0;
            return false;
        }

        int rowIndex = v - SpawnMinV;
        int cellsBeforeRow = rowIndex / 2 * 35 + (rowIndex % 2 == 0 ? 0 : 18);
        int cellInRow = (u - SpawnMinU) / 2;
        int id = cellsBeforeRow + cellInRow;

        if ((uint)id >= SpawnCellCount)
        {
            cellId = 0;
            return false;
        }

        cellId = id;
        return true;
    }

    public static (int X, int Y) GetSpawnPositionFromCellId(int gamePos, int cellId)
    {
        if ((uint)cellId >= SpawnCellCount)
        {
            throw new InvalidDataException("Invalid spawn position cell id.");
        }

        int pairIndex = cellId / 35;
        int cellInPair = cellId % 35;
        int rowIndex;
        int offset;
        if (cellInPair < 18)
        {
            rowIndex = pairIndex * 2;
            offset = cellInPair * 2;
        }
        else
        {
            rowIndex = pairIndex * 2 + 1;
            offset = (cellInPair - 18) * 2 + 1;
        }

        int v = SpawnMinV + rowIndex;
        int u = SpawnMinU + offset;
        int x = (u + v) / 2;
        int y = (v - u) / 2;
        return gamePos >= 4
            ? (x - Team2SpawnOffset, y - Team2SpawnOffset)
            : (x, y);
    }

    private static bool TryGetMapPositionId(int x, int y, out int positionId)
    {
        if ((uint)x > MapWidth || (uint)y > MapHeight)
        {
            positionId = 0;
            return false;
        }

        positionId = y * MapPositionStride + x;
        return true;
    }

    private static void WriteGameloopDeltas(Stream stream, int startGameloop, IReadOnlyList<int> gameloops)
    {
        WriteVarUInt(stream, (uint)gameloops.Count);
        int previous = startGameloop;
        foreach (var gameloop in gameloops.Order())
        {
            WriteVarUInt(stream, (uint)Math.Max(0, gameloop - previous));
            previous = gameloop;
        }
    }

    private static int[] ReadGameloopDeltas(Stream stream, int startGameloop)
    {
        int count = checked((int)ReadVarUInt(stream));
        if (count == 0)
        {
            return [];
        }

        var gameloops = new int[count];
        int previous = startGameloop;
        for (int i = 0; i < count; i++)
        {
            previous = checked(previous + (int)ReadVarUInt(stream));
            gameloops[i] = previous;
        }
        return gameloops;
    }

    private static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteVarUInt(stream, (uint)bytes.Length);
        stream.Write(bytes);
    }

    private static string ReadString(Stream stream)
    {
        int length = checked((int)ReadVarUInt(stream));
        if (length == 0)
        {
            return string.Empty;
        }

        var bytes = new byte[length];
        stream.ReadExactly(bytes);
        return Encoding.UTF8.GetString(bytes);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BitConverter.TryWriteBytes(buffer, value);
        stream.Write(buffer);
    }

    private static uint ReadUInt32(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        stream.ReadExactly(buffer);
        return BitConverter.ToUInt32(buffer);
    }

    private static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        BitConverter.TryWriteBytes(buffer, value);
        stream.Write(buffer);
    }

    private static ushort ReadUInt16(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        stream.ReadExactly(buffer);
        return BitConverter.ToUInt16(buffer);
    }

    private static void WriteVarUInt(Stream stream, uint value)
    {
        while (value >= 0x80)
        {
            stream.WriteByte((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }

        stream.WriteByte((byte)value);
    }

    private static uint ReadVarUInt(Stream stream)
    {
        uint result = 0;
        int shift = 0;
        while (shift <= 28)
        {
            int next = stream.ReadByte();
            if (next < 0)
            {
                throw new EndOfStreamException();
            }

            result |= (uint)(next & 0x7F) << shift;
            if ((next & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
        }

        throw new InvalidDataException("Invalid variable-length integer.");
    }

    private sealed class BitWriter(byte[] buffer)
    {
        private int byteIndex;
        private int currentByte;
        private int bitCount;

        public void WriteBits(uint value, int bits)
        {
            for (int i = 0; i < bits; i++)
            {
                currentByte |= (int)((value >> i) & 1) << bitCount;
                bitCount++;
                if (bitCount == 8)
                {
                    FlushByte();
                }
            }
        }

        public void Flush()
        {
            if (bitCount > 0)
            {
                FlushByte();
            }
        }

        private void FlushByte()
        {
            if ((uint)byteIndex >= (uint)buffer.Length)
            {
                throw new InvalidDataException("Spawn position bit buffer overflow.");
            }

            buffer[byteIndex++] = (byte)currentByte;
            currentByte = 0;
            bitCount = 0;
        }
    }

    private sealed class BitReader(byte[] buffer)
    {
        private int byteIndex;
        private int currentByte;
        private int bitsRemaining;

        public uint ReadBits(int bits)
        {
            uint value = 0;
            for (int i = 0; i < bits; i++)
            {
                if (bitsRemaining == 0)
                {
                    if ((uint)byteIndex >= (uint)buffer.Length)
                    {
                        throw new EndOfStreamException();
                    }
                    currentByte = buffer[byteIndex++];
                    bitsRemaining = 8;
                }

                value |= (uint)(currentByte & 1) << i;
                currentByte >>= 1;
                bitsRemaining--;
            }

            return value;
        }
    }

    private sealed record UnitWriteRow(
        SpawnPlaybackUnitSidecarDto Unit,
        int UnitNameId,
        byte Flags,
        int SpawnCellId,
        int DiedPositionId);
}
