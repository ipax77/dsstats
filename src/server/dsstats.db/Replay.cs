using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public class Replay
{
    public int ReplayId { get; set; }
    [MaxLength(200)]
    public string? FileName { get; set; }
    [MaxLength(50)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Version { get; set; } = string.Empty;
    public GameMode GameMode { get; set; }
    public int RegionId { get; set; }
    public bool TE { get; set; }
    public int PlayerCount { get; set; }
    [Precision(0)]
    public DateTime Gametime { get; set; }
    public int BaseBuild { get; set; }
    public int Duration { get; set; }
    public int Cannon { get; set; }
    public int Bunker { get; set; }
    public int WinnerTeam { get; set; }
    public int[] MiddleChanges { get; set; } = [];
    [MaxLength(64)]
    public string ReplayHash { get; set; } = string.Empty;
    [MaxLength(64)]
    public string CompatHash { get; set; } = string.Empty;
    [Precision(0)]
    public DateTime Imported { get; set; }
    public bool Uploaded { get; set; }
    public ICollection<ReplayPlayer> Players { get; set; } = [];
    public ICollection<ReplayRating> Ratings { get; set; } = [];
}
