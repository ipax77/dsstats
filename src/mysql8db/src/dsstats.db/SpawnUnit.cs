using System.ComponentModel.DataAnnotations.Schema;
using dsstats.shared;

namespace dsstats.db;

public sealed class SpawnUnit
{
    public int SpawnUnitId { get; set; }
    public int Count { get; set; }
    public byte[] PositionsBinary { get; set; } = [];
    public int UnitId { get; set; }
    public Unit? Unit { get; set; }
    public int SpawnId { get; set; }
    public Spawn? Spawn { get; set; }

    [NotMapped]
    public List<Position> Positions
    {
        get => DecodePositions(PositionsBinary);
        set => PositionsBinary = EncodePositions(value);
    }

    private static byte[] EncodePositions(List<Position> positions)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        foreach (var p in positions)
        {
            bw.Write(p.X);
            bw.Write(p.Y);
        }

        return ms.ToArray();
    }

    private static List<Position> DecodePositions(byte[] data)
    {
        var result = new List<Position>();

        if (data == null || data.Length == 0)
            return result;

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        while (ms.Position < ms.Length)
        {
            int x = br.ReadInt32();
            int y = br.ReadInt32();
            result.Add(new Position(x, y));
        }

        return result;
    }
}

