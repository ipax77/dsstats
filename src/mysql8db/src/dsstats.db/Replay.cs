
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public sealed class Replay
{
    public int ReplayId { get; set; }
    [Precision(0)]
    public DateTime GameTime { get; set; }
    [Precision(0)]
    public DateTime? Imported { get; set; }
    public GameMode GameMode { get; set; }
    public bool IsTE { get; set; }
    public string ReplayHash { get; set; } = string.Empty;
    public Region Region { get; set; }
    public int Duration { get; set; }
    public int PlayerCount { get; set; }
    public int WinnerTeam { get; set; }
    public int Bunker { get; set; }
    public int Cannon { get; set; }
    public int Minkillsum { get; set; }
    public int Maxkillsum { get; set; }
    public int Minarmy { get; set; }
    public int Minincome { get; set; }
    public int Maxleaver { get; set; }
    public int Views { get; set; }
    public byte[] MiddleBinary { get; set; } = [];
    [MaxLength(15)]
    public string CommandersTeam1 { get; set; } = null!;
    [MaxLength(15)]
    public string CommandersTeam2 { get; set; } = null!;
    public ICollection<ReplayPlayer> ReplayPlayers { get; set; } = [];
    public ICollection<ReplayRating> ReplayRatings { get; set; } = [];
    [NotMapped]
    public MiddleControl? MiddleControlData
    {
        get => DecodeMiddleControl(MiddleBinary);
        set => MiddleBinary = EncodeMiddleControl(value);
    }

    private static byte[] EncodeMiddleControl(MiddleControl? data)
    {
        if (data is null)
        {
            return [];
        }
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(data.FirstTeam);
        writer.Write(data.Gameloops.Count);
        foreach (var loop in data.Gameloops)
        {
            writer.Write(loop);
        }

        return ms.ToArray();
    }

    private static MiddleControl? DecodeMiddleControl(byte[] blob)
    {
        var changes = new List<int>();

        if (blob is null || blob.Length == 0)
            return null;

        using var ms = new MemoryStream(blob);
        using var reader = new BinaryReader(ms);

        var firstTeam = reader.ReadInt32();
        int count = reader.ReadInt32();

        for (int i = 0; i < count && ms.Position < ms.Length; i++)
        {
            changes.Add(reader.ReadInt32());
        }

        return new(firstTeam, changes);
    }
}

