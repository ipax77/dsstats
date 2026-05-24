using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public sealed class ReplaySpawnPlayback
{
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }
    public ushort FormatVersion { get; set; }
    public SpawnPlaybackCompression Compression { get; set; }
    public int CompressedLength { get; set; }
    public int UncompressedLength { get; set; }
    public int UnitCount { get; set; }
    public byte[] Payload { get; set; } = [];
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
}
