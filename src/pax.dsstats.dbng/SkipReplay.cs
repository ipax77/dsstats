using System.ComponentModel.DataAnnotations;

namespace pax.dsstats.dbng;

public class SkipReplay
{
    public int SkipReplayId { get; set; }
    [MaxLength(500)]
    public string Path { get; set; } = null!;
}
