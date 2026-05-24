using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public sealed class ReplayUserRatingCollect
{
    public int ReplayUserRatingCollectId { get; set; }
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }

    [MaxLength(64)]
    public string IpHash { get; set; } = string.Empty;

    public int Score { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [Precision(0)]
    public DateTime? ProcessedAt { get; set; }
}
