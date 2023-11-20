using System.ComponentModel.DataAnnotations;

namespace dsstats.db8;

public class SkipReplay
{
    public int SkipReplayId { get; set; }
    [MaxLength(500)]
    public string Path { get; set; } = null!;
}
