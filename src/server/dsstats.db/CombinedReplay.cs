using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public class CombinedReplay
{
    public int CombinedReplayId { get; set; }
    public int? ReplayId { get; set; }
    public int? ArcadeReplayId { get; set; }
    public GameMode GameMode { get; set; }
    public int Duration { get; set; }
    [Precision(0)]
    public DateTime Gametime { get; set; }
    public bool TE { get; set; }
    public int PlayerCount { get; set; }
    public int WinnerTeam { get; set; }
    [Precision(0)]
    public DateTime Imported { get; set; }
}
