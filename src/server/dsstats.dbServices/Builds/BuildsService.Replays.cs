using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices.Builds;

public partial class BuildsService
{
    public async Task<List<ReplayListDto>> GetBuildReplays(BuildsRequest request)
    {
        bool noOpp = request.Versus == Commander.None;
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);
        var minDuration = request.Breakpoint switch
        {
            Breakpoint.Min5 => 5 * 60,
            Breakpoint.Min10 => 10 * 60,
            Breakpoint.Min15 => 15 * 60,
            _ => 0
        };
        var noDuration = minDuration == 0;
        var noMinRating = request.FromRating <= Data.MinBuildRating;
        var noMaxRating = request.ToRating >= Data.MaxBuildRating;
        var playerIds = request.Players.Select(s => s.PlayerId).ToHashSet();
        var noPlayers = playerIds.Count == 0;

        var replays = from r in context.Replays
                      where r.Gametime >= timeInfo.Start
                        && (!timeInfo.HasEnd || r.Gametime < timeInfo.End)
                        && (noDuration || r.Duration > minDuration)
                        // Check that at least one player matches our interest criteria
                        && r.Players.Any(p => p.Race == request.Interest
                            && (noOpp || p.OppRace == request.Versus))
                      // Join to ratings once
                      join rr in context.Set<ReplayRating>() on r.ReplayId equals rr.ReplayId
                      where rr.RatingType == request.RatingType
                        && rr.LeaverType == LeaverType.None
                        // Rating filter: only apply if we have specific players OR rating bounds
                        && (noPlayers && noMinRating && noMaxRating
                            || rr.ReplayPlayerRatings.Any(rpr =>
                                rpr.ReplayPlayer!.Race == request.Interest
                                && (noOpp || rpr.ReplayPlayer.OppRace == request.Versus)
                                && (noPlayers || playerIds.Contains(rpr.PlayerId))
                                && (noPlayers || noMinRating || rpr.RatingBefore >= request.FromRating)
                                && (noPlayers || noMaxRating || rpr.RatingBefore <= request.ToRating)))
                      orderby r.Gametime descending
                      select new ReplayList()
                      {
                          ReplayHash = r.ReplayHash,
                          GameTime = r.Gametime,
                          GameMode = r.GameMode,
                          Duration = r.Duration,
                          WinnerTeam = r.WinnerTeam,
                          Players = r.Players.Select(s => new ReplayPlayerList()
                          {
                              Name = s.Name,
                              Race = s.Race,
                              OppRace = s.OppRace,
                              Team = s.TeamId,
                              GamePos = s.GamePos,
                          }).ToList(),
                          RatingList = new()
                          {
                              Exp2Win = rr.ExpectedWinProbability,
                              AvgRating = rr.AvgRating,
                              LeaverType = rr.LeaverType,
                          },
                      };

        var list = await replays
            .Take(10)
            .ToListAsync();

        return list.Select(replay =>
        {
            var interestPlayer = replay.Players.FirstOrDefault(f =>
                f.Race == request.Interest && (noOpp || f.OppRace == request.Versus));
            replay.PlayerPos = interestPlayer?.GamePos ?? 0;
            return replay.GetDto();
        }).ToList();
    }
}
