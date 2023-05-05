
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.dbng.Services;

public partial class PlayerService
{
    public async Task<PlayerDetailSummary> GetPlayerPlayerIdSummary(PlayerId playerId, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        PlayerDetailSummary summary = new()
        {
            GameModesPlayed = await GetPlayerIdGameModeCounts(context, playerId, token),
            Ratings = await GetPlayerIdRatings(context, playerId, token),
            Commanders = await GetPlayerIdCommandersPlayed(context, playerId, token),
        };

        (summary.CmdrPercentileRank, summary.StdPercentileRank) =
            await GetPercentileRank(
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Cmdr)?.Pos ?? 0,
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Std)?.Pos ?? 0);
        
        return summary;
    }



    private static async Task<List<CommanderInfo>> GetPlayerIdCommandersPlayed(ReplayContext context, PlayerId playerId, CancellationToken token)
    {
        return await (from p in context.Players
                      from rp in p.ReplayPlayers
                      where p.ToonId == playerId.ToonId
                        && p.RegionId == playerId.RegionId
                        && p.RealmId == playerId.RealmId
                      group rp by rp.Race into g
                      select new CommanderInfo()
                      {
                          Cmdr = g.Key,
                          Count = g.Count()
                      })
                    .ToListAsync(token);
    }

    private async Task<List<PlayerRatingDetailDto>> GetPlayerIdRatings(ReplayContext context, PlayerId playerId, CancellationToken token)
    {
        return await context.PlayerRatings
                .Where(x => x.Player.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId)
                .ProjectTo<PlayerRatingDetailDto>(mapper.ConfigurationProvider)
                .ToListAsync(token);
    }

    private static async Task<List<PlayerGameModeResult>> GetPlayerIdGameModeCounts(ReplayContext context, PlayerId playerId, CancellationToken token)
    {
        var gameModeGroup = from r in context.Replays
                            from rp in r.ReplayPlayers
                            where rp.Player.ToonId == playerId.ToonId
                                && rp.Player.RegionId == playerId.RegionId
                                && rp.Player.RealmId == playerId.RealmId
                            group r by new { r.GameMode, r.Playercount } into g
                            select new PlayerGameModeResult()
                            {
                                GameMode = g.Key.GameMode,
                                PlayerCount = g.Key.Playercount,
                                Count = g.Count(),
                            };
        return await gameModeGroup.ToListAsync(token);
    }

    public async Task<PlayerRatingDetails> GetPlayerIdPlayerRatingDetails(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        // return await DEBUGGetPlayerRatingDetails(toonId, ratingType, token);

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return new()
        {
            Teammates = await GetPlayerIdPlayerTeammates(context, playerId, ratingType, true, token),
            Opponents = await GetPlayerIdPlayerTeammates(context, playerId, ratingType, false, token),
            AvgTeamRating = await GetPlayerIdTeamRating(context, playerId, ratingType, true, token),
            CmdrsAvgGain = await GetPlayerIdPlayerCmdrAvgGain(playerId, ratingType, TimePeriod.Past90Days, token)
        };
    }

    public async Task<List<PlayerCmdrAvgGain>> GetPlayerIdPlayerCmdrAvgGain(PlayerId playerId, RatingType ratingType, TimePeriod timePeriod, CancellationToken token)
    {
        (var startTime, var endTime) = Data.TimeperiodSelected(timePeriod);
        if (endTime == DateTime.Today)
        {
            endTime = DateTime.Today.AddDays(2);
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var group = from p in context.Players
                    from rp in p.ReplayPlayers
                    where p.ToonId == playerId.ToonId
                        && p.RegionId == playerId.RegionId
                        && p.RealmId == playerId.RealmId
                        && rp.Replay.GameTime > startTime && rp.Replay.GameTime < endTime
                        && rp.Replay.ReplayRatingInfo.RatingType == ratingType
                    group rp by rp.Race into g
                    select new PlayerCmdrAvgGain
                    {
                        Commander = g.Key,
                        AvgGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.RatingChange), 2),
                        Count = g.Count(),
                        Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                    };
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        var items = await group.ToListAsync(token);

        if (ratingType == RatingType.Cmdr || ratingType == RatingType.CmdrTE)
        {
            items = items.Where(x => (int)x.Commander > 3).ToList();
        }
        else if (ratingType == RatingType.Std || ratingType == RatingType.StdTE)
        {
            items = items.Where(x => (int)x.Commander <= 3).ToList();
        }
        return items;
    }

    private async Task<double> GetPlayerIdTeamRating(ReplayContext context, PlayerId playerId, RatingType ratingType, bool inTeam, CancellationToken token)
    {
        var teamRatings = inTeam ? from p in context.Players
                                   from rp in p.ReplayPlayers
                                   from t in rp.Replay.ReplayPlayers
                                   where p.ToonId == playerId.ToonId
                                       && p.RealmId == playerId.RealmId
                                       && p.RegionId == playerId.RegionId
                                       && rp.Replay.ReplayRatingInfo != null
                                       && rp.Replay.ReplayRatingInfo.RatingType == ratingType
                                       && t != rp
                                       && t.Team == rp.Team
                                   select t.ReplayPlayerRatingInfo
                                : from p in context.Players
                                  from rp in p.ReplayPlayers
                                  from t in rp.Replay.ReplayPlayers
                                  where p.ToonId == playerId.ToonId
                                       && p.RealmId == playerId.RealmId
                                       && p.RegionId == playerId.RegionId
                                      && rp.Replay.ReplayRatingInfo != null
                                      && rp.Replay.ReplayRatingInfo.RatingType == ratingType
                                      && t.Team != rp.Team
                                  select t.ReplayPlayerRatingInfo;

        var avgRating = await teamRatings
            .Select(s => s.Rating)
            .DefaultIfEmpty()
            .AverageAsync(token);
        return Math.Round(avgRating, 2);
    }

    private async Task<List<PlayerMatchupInfo>> GetPlayerIdPlayerMatchups(ReplayContext context, PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        return await (from p in context.Players
                      from rp in p.ReplayPlayers
                      where p.ToonId == playerId.ToonId
                          && p.RealmId == playerId.RealmId
                          && p.RegionId == playerId.RegionId
                          && rp.Replay.ReplayRatingInfo != null
                          && rp.Replay.ReplayRatingInfo.RatingType == ratingType
                      group rp by new { rp.Race, rp.OppRace } into g
                      select new PlayerMatchupInfo
                      {
                          Commander = g.Key.Race,
                          Versus = g.Key.OppRace,
                          Count = g.Count(),
                          Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                      })
                    .ToListAsync(token);
    }

    private async Task<List<PlayerTeamResult>> GetPlayerIdPlayerTeammates(ReplayContext context, PlayerId playerId, RatingType ratingType, bool inTeam, CancellationToken token)
    {
        var teammateGroup = inTeam ?
                                from p in context.Players
                                from rp in p.ReplayPlayers
                                from t in rp.Replay.ReplayPlayers
                                where p.ToonId == playerId.ToonId
                                       && p.RealmId == playerId.RealmId
                                       && p.RegionId == playerId.RegionId
                                    && rp.Replay.ReplayRatingInfo != null
                                    && rp.Replay.ReplayRatingInfo.RatingType == ratingType
                                where t != rp && t.Team == rp.Team
                                group t by new { t.Player.ToonId, t.Player.Name } into g
                                where g.Count() > 10
                                select new PlayerTeamResultHelper()
                                {
                                    ToonId = g.Key.ToonId,
                                    Name = g.Key.Name,
                                    Count = g.Count(),
                                    Wins = g.Count(c => c.PlayerResult == PlayerResult.Win),
                                }
                            : from p in context.Players
                              from rp in p.ReplayPlayers
                              from t in rp.Replay.ReplayPlayers
                              where p.ToonId == playerId.ToonId
                                       && p.RealmId == playerId.RealmId
                                       && p.RegionId == playerId.RegionId
                                  && rp.Replay.ReplayRatingInfo != null
                                  && rp.Replay.ReplayRatingInfo.RatingType == ratingType
                              where t.Team != rp.Team
                              group t by new { t.Player.ToonId, t.Player.Name } into g
                              where g.Count() > 10
                              select new PlayerTeamResultHelper()
                              {
                                  ToonId = g.Key.ToonId,
                                  Name = g.Key.Name,
                                  Count = g.Count(),
                                  Wins = g.Count(c => c.PlayerResult == PlayerResult.Win),
                              };

        var results = await teammateGroup
            .ToListAsync(token);


        return results.Select(s => new PlayerTeamResult()
        {
            Name = s.Name,
            ToonId = s.ToonId,
            Count = s.Count,
            Wins = s.Wins,
        }).ToList();
    }

    public async Task<PlayerDetailResponse> GetPlayerIdPlayerDetails(PlayerDetailRequest request, CancellationToken token = default)
    {
        PlayerDetailResponse response = new();

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if ((int)request.TimePeriod < 3)
        {
            request.TimePeriod = TimePeriod.Past90Days;
        }

        response.CmdrStrengthItems = await GetPlayerIdCmdrStrengthItems(context, request, token);

        return response;
    }

    private async Task<List<CmdrStrengthItem>> GetPlayerIdCmdrStrengthItems(ReplayContext context, PlayerDetailRequest request, CancellationToken token)
    {
        (var startDate, var endDate) = Data.TimeperiodSelected(request.TimePeriod);

        var replays = context.Replays
            .Where(x => x.GameTime > startDate
                && x.ReplayRatingInfo != null
                && x.ReplayRatingInfo.LeaverType == LeaverType.None
                && x.ReplayRatingInfo.RatingType == request.RatingType);

        if (endDate != DateTime.MinValue && (DateTime.Today - endDate).TotalDays > 2)
        {
            replays = replays.Where(x => x.GameTime < endDate);
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var group = request.Interest == Commander.None
            ?
                from r in replays
                from rp in r.ReplayPlayers
                where rp.Player.ToonId == request.RequestNames.ToonId
                    && rp.Player.RegionId == request.RequestNames.RegionId
                    && rp.Player.RealmId == request.RequestNames.RealmId
                group rp by rp.Race into g
                select new CmdrStrengthItem()
                {
                    Commander = g.Key,
                    Matchups = g.Count(),
                    AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.Rating), 2),
                    AvgRatingGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.RatingChange), 2),
                    Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                }
            :
                from r in replays
                from rp in r.ReplayPlayers
                where rp.Player.ToonId == request.RequestNames.ToonId
                    && rp.Race == request.Interest
                group rp by rp.OppRace into g
                select new CmdrStrengthItem()
                {
                    Commander = g.Key,
                    Matchups = g.Count(),
                    AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.Rating), 2),
                    AvgRatingGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.RatingChange), 2),
                    Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                }
        ;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        var items = await group.ToListAsync(token);

        if (request.RatingType == RatingType.Cmdr || request.RatingType == RatingType.CmdrTE)
        {
            items = items.Where(x => (int)x.Commander > 3).ToList();
        }
        else if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            items = items.Where(x => (int)x.Commander <= 3).ToList();
        }
        return items;
    }
}
