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
    int UnitCount);

public static class SpawnPlaybackSidecarCodec
{
    public const ushort FormatVersion = 1;
    public const SpawnPlaybackCompression Compression = SpawnPlaybackCompression.Brotli;

    private const uint Magic = 0x42505344; // DSPB
    private const byte HasDiedGameloop = 1;
    private const byte HasDiedPosition = 2;

    public static byte[] Encode(SpawnPlaybackSidecarDto dto)
    {
        return EncodeWithMetadata(dto).Payload;
    }

    public static SpawnPlaybackEncodedSidecar EncodeWithMetadata(SpawnPlaybackSidecarDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        using var raw = new MemoryStream();
        WriteUncompressed(raw, dto);
        int uncompressedLength = checked((int)raw.Length);

        using var compressed = new MemoryStream();
        using (var brotli = new BrotliStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
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
            WriteVarUInt(stream, (uint)Math.Max(0, player.GamePos));
            WriteVarUInt(stream, (uint)player.Units.Count);
            foreach (var unit in player.Units.OrderBy(unit => unit.SpawnGameloop).ThenBy(unit => unit.UnitIndex))
            {
                WriteVarUInt(stream, (uint)Math.Max(0, unit.UnitIndex));
                WriteVarUInt(stream, (uint)stringIds[unit.Name]);
                WriteVarUInt(stream, (uint)Math.Max(0, unit.SpawnNumber));
                WriteVarUInt(stream, (uint)Math.Max(0, unit.SpawnGameloop));
                WriteVarUInt(stream, (uint)Math.Max(0, unit.SpawnX));
                WriteVarUInt(stream, (uint)Math.Max(0, unit.SpawnY));

                byte flags = 0;
                if (unit.DiedGameloop is not null)
                {
                    flags |= HasDiedGameloop;
                }
                if (unit.DiedX is not null && unit.DiedY is not null)
                {
                    flags |= HasDiedPosition;
                }
                stream.WriteByte(flags);

                if ((flags & HasDiedGameloop) != 0)
                {
                    WriteVarUInt(stream, (uint)Math.Max(0, unit.DiedGameloop!.Value - unit.SpawnGameloop));
                }
                if ((flags & HasDiedPosition) != 0)
                {
                    WriteVarUInt(stream, (uint)Math.Max(0, unit.DiedX!.Value));
                    WriteVarUInt(stream, (uint)Math.Max(0, unit.DiedY!.Value));
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
                int spawnX = checked((int)ReadVarUInt(stream));
                int spawnY = checked((int)ReadVarUInt(stream));
                int flags = stream.ReadByte();
                if (flags < 0)
                {
                    throw new EndOfStreamException();
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
                    diedX = checked((int)ReadVarUInt(stream));
                    diedY = checked((int)ReadVarUInt(stream));
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
}
