using dsstats.db;
using dsstats.shared;

namespace dsstats.dbServices.Extensions;

public static class ReplayExtensions
{
    public static ReplayMatchDto ToReplayMatchDto(this Replay replay)
    {
        return new ReplayMatchDto
        {
            ReplayId = replay.ReplayId,
            Gametime = replay.Gametime,
            Duration = replay.Duration,
            GameMode = replay.GameMode,
            PlayerCount = replay.Players.Count,
            WinnerTeam = replay.WinnerTeam,
            Players = replay.Players.Select(x => new PlayerMatchDto()
            {
                ReplayPlayerId = x.ReplayPlayerId,
                Team = x.TeamId,
                ToonId = new()
                {
                    Region = x.Player!.ToonId.Region,
                    Realm = x.Player.ToonId.Realm,
                    Id = x.Player.ToonId.Id,
                }
            }).ToList(),
        };
    }
}

public static class ArcadeReplayExtension
{
    public static ReplayMatchDto ToReplayMatchDto(this ArcadeReplay replay)
    {
        return new ReplayMatchDto
        {
            ReplayId = replay.ArcadeReplayId,
            Gametime = replay.CreatedAt,
            Duration = replay.Duration,
            GameMode = replay.GameMode,
            PlayerCount = replay.PlayerCount,
            WinnerTeam = replay.WinnerTeam,
            Players = replay.Players.Select(x => new PlayerMatchDto()
            {
                ReplayPlayerId = x.ArcadeReplayPlayerId,
                Team = x.Team,
                ToonId = new()
                {
                    Region = x.Player!.ToonId.Region,
                    Realm = x.Player.ToonId.Realm,
                    Id = x.Player.ToonId.Id,
                }
            }).ToList()
        };
    }
}