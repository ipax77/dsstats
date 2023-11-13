using dsstats.db8;
using dsstats.shared;

namespace dsstats.db8services;

public partial class ReplaysService
{
    private IQueryable<ReplayListDto> FilterRatings(ReplaysRequest request, IQueryable<Replay> replays)
    {
        if (request.Filter?.ReplaysRatingRequest is null)
        {
            return from r in replays
                   join rr in context.ComboReplayRatings
                    on r.ReplayId equals rr.ReplayId into g
                   from grr in g.DefaultIfEmpty()
                   select new ReplayListDto()
                   {
                       GameTime = r.GameTime,
                       Duration = r.Duration,
                       WinnerTeam = r.WinnerTeam,
                       GameMode = r.GameMode,
                       TournamentEdition = r.TournamentEdition,
                       ReplayHash = r.ReplayHash,
                       DefaultFilter = r.DefaultFilter,
                       CommandersTeam1 = r.CommandersTeam1,
                       CommandersTeam2 = r.CommandersTeam2,
                       MaxLeaver = r.Maxleaver,
                       Exp2Win = grr.ExpectationToWin
                   };
        }

        return request.Filter.ReplaysRatingRequest.RatingCalcType switch
        {
            RatingCalcType.Combo => FilterComboRatings(request.Filter.ReplaysRatingRequest, replays),
            _ => replays.Select(s => new ReplayListDto()
            {
                GameTime = s.GameTime,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                GameMode = s.GameMode,
                TournamentEdition = s.TournamentEdition,
                ReplayHash = s.ReplayHash,
                DefaultFilter = s.DefaultFilter,
                CommandersTeam1 = s.CommandersTeam1,
                CommandersTeam2 = s.CommandersTeam2,
                MaxLeaver = s.Maxleaver,
            })
        };
    }

    private IQueryable<ReplayListDto> FilterComboRatings(ReplaysRatingRequest request, IQueryable<Replay> replays)
    {
        var query = from r in replays
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    where rr.RatingType == request.RatingType
                        && (request.AvgMinRating == 0 || rr.AvgRating >= request.AvgMinRating)
                        && (request.FromExp2Win == 0 || rr.ExpectationToWin >= request.FromExp2Win / 100.0)
                        && (request.ToExp2Win == 0 || rr.ExpectationToWin <= request.ToExp2Win / 100.0)
                        && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    select new ReplayListDto()
                    {
                        GameTime = r.GameTime,
                        Duration = r.Duration,
                        WinnerTeam = r.WinnerTeam,
                        GameMode = r.GameMode,
                        TournamentEdition = r.TournamentEdition,
                        ReplayHash = r.ReplayHash,
                        DefaultFilter = r.DefaultFilter,
                        CommandersTeam1 = r.CommandersTeam1,
                        CommandersTeam2 = r.CommandersTeam2,
                        MaxLeaver = r.Maxleaver,
                        Exp2Win = rr.ExpectationToWin,
                        AvgRating = rr.AvgRating
                    };
        return query;
    }
}
