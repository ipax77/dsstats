using dsstats.shared;
using dsstats.shared.DetailBuild;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public enum ReplayBuildDetailStatus
{
    Detected = 1,
    NotDetectable = 2,
}

public class ReplayBuildDetail
{
    public int ReplayBuildDetailId { get; set; }
    public int DetectionVersion { get; set; }
    public ReplayBuildDetailStatus Status { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    [Precision(0)]
    public DateTime UpdatedAt { get; set; }
    [MaxLength(200)]
    public string? FailureReason { get; set; }
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }
    public ICollection<ReplayPlayerBuildDetail> PlayerBuilds { get; set; } = [];
    public ICollection<ReplayTeamBuildDetail> TeamBuilds { get; set; } = [];
}

public class ReplayPlayerBuildDetail
{
    public int ReplayPlayerBuildDetailId { get; set; }
    public int GamePos { get; set; }
    public int TeamId { get; set; }
    public Commander Commander { get; set; }
    public int Build { get; set; }
    public bool GasFirst { get; set; }
    public int Lane { get; set; }
    public int OppGamePos { get; set; }
    public Commander OppCommander { get; set; }
    public int OppBuild { get; set; }
    public bool OppGasFirst { get; set; }
    public bool Won { get; set; }
    public int ReplayBuildDetailId { get; set; }
    public ReplayBuildDetail? ReplayBuildDetail { get; set; }
    public int ReplayPlayerId { get; set; }
    public ReplayPlayer? ReplayPlayer { get; set; }
    public int OppReplayPlayerId { get; set; }
    public ReplayPlayer? OppReplayPlayer { get; set; }
}

public class ReplayTeamBuildDetail
{
    public int ReplayTeamBuildDetailId { get; set; }
    public int TeamId { get; set; }
    public TeamBuild TeamBuild { get; set; }
    public int ReplayBuildDetailId { get; set; }
    public ReplayBuildDetail? ReplayBuildDetail { get; set; }
    public int LeaderReplayPlayerId { get; set; }
    public ReplayPlayer? LeaderReplayPlayer { get; set; }
    public int FollowerReplayPlayerId { get; set; }
    public ReplayPlayer? FollowerReplayPlayer { get; set; }
}
