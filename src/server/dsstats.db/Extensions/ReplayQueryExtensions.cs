using dsstats.shared;

namespace dsstats.db.Extensions;

public static class ReplayQueryExtensions
{
    public static IQueryable<ReplayCalcDto> ToReplayCalcDtos(
        this IQueryable<Replay> query)
    {
        return query
            .Where(x => x.Duration >= 300 &&
                        x.WinnerTeam > 0 &&
                        x.PlayerCount > 1)
            .OrderBy(x => x.Gametime)
            .ThenBy(x => x.ReplayId)
            .Select(s => ToDto(s));
    }

    public static ReplayCalcDto ToDto(Replay s)
    {
        return new ReplayCalcDto
        {
            ReplayId = s.ReplayId,
            Gametime = s.Gametime,
            GameMode = s.GameMode,
            PlayerCount = s.PlayerCount,
            WinnerTeam = s.WinnerTeam,
            TE = s.TE,
            Players = s.Players.Select(t => new PlayerCalcDto
            {
                ReplayPlayerId = t.ReplayPlayerId,
                IsLeaver = t.Duration < s.Duration - 90,
                IsMvp = t.IsMvp,
                Team = t.TeamId,
                Race = t.Race,
                PlayerId = t.PlayerId
            }).ToList()
        };
    }

    public static ReplayCalcDto? ToReplayCalcDto(
    this Replay replay)
    {
        if (replay.Duration < 300 ||
            replay.WinnerTeam <= 0 ||
            replay.PlayerCount <= 1)
        {
            return null;
        }

        return ToDto(replay);
    }
}
