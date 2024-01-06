
using AutoMapper.QueryableExtensions;
using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class PlayerService
{
    public async Task<PlayerDetailSummary> GetPlayerPlayerIdSummary(PlayerId playerId,
                                                                    RatingType ratingType,
                                                                    RatingCalcType ratingCalcType,
                                                                    CancellationToken token = default)
    {
        if (IsSqlite)
        {
            return await GetPlayerPlayerIdDsstatsSummary(playerId, ratingType, token);
        }

        return ratingCalcType switch
        {
            RatingCalcType.Dsstats => await GetPlayerPlayerIdDsstatsSummary(playerId, ratingType, token),
            RatingCalcType.Arcade => await GetPlayerPlayerIdArcadeSummary(playerId, ratingType, token),
            RatingCalcType.Combo => await GetPlayerPlayerIdComboSummary(playerId, ratingType, token),
            _ => throw new NotImplementedException()
        };
    }

    private async Task<PlayerDetailSummary> GetPlayerPlayerIdDsstatsSummary(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        PlayerDetailSummary summary = new()
        {
            GameModesPlayed = await GetPlayerIdGameModeCounts(playerId, token),
            Ratings = await GetPlayerIdDsstatsRatings(playerId, token),
            Commanders = await GetPlayerIdCommandersPlayed(playerId, ratingType, token),
            ChartDtos = await GetPlayerRatingChartData(playerId, RatingCalcType.Dsstats, ratingType, token)
        };

        (summary.CmdrPercentileRank, summary.StdPercentileRank) =
            await GetPercentileRank(
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Cmdr)?.Pos ?? 0,
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Std)?.Pos ?? 0,
                RatingCalcType.Dsstats);

        return summary;
    }

    public async Task<List<CommanderInfo>> GetPlayerIdCommandersPlayed(PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                    where p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                     && (ratingType == RatingType.None || rr.RatingType == ratingType)
                    group rp by rp.Race into g
                    orderby g.Count() descending
                    select new CommanderInfo()
                    {
                        Cmdr = g.Key,
                        Count = g.Count()
                    };

        return await query.ToListAsync();
    }

    private async Task<MvpInfo?> GetMvpInfo(PlayerId playerId, RatingType ratingType)
    {
        return await context.PlayerRatings
            .Where(x => x.RatingType == ratingType
                && x.Player.ToonId == playerId.ToonId
                && x.Player.RealmId == playerId.RealmId
                && x.Player.RegionId == playerId.RegionId)
            .Select(s => new MvpInfo()
            {
                Games= s.Games,
                Mvp = s.Mvp,
                MainCount = s.MainCount,
                Main = s.Main
            })
            .FirstOrDefaultAsync();
    }

    private async Task<List<PlayerRatingDetailDto>> GetPlayerIdDsstatsRatings(PlayerId playerId, CancellationToken token)
    {
        return await context.PlayerRatings
                .Include(i => i.PlayerRatingChange)
                .Where(x => x.Player!.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId)
                .Select(s => new PlayerRatingDetailDto()
                {
                    RatingType = s.RatingType,
                    Rating = Math.Round(s.Rating, 2),
                    Pos = s.Pos,
                    Games = s.Games,
                    Wins = s.Wins,
                    Mvp = s.Mvp,
                    Consistency = Math.Round(s.Consistency, 2),
                    Confidence = Math.Round(s.Confidence, 2),
                    Player = new PlayerRatingPlayerDto()
                    {
                        Name = s.Player.Name,
                        ToonId = s.Player.ToonId,
                        RegionId = s.Player.RegionId,
                        RealmId = s.Player.RealmId,
                        IsUploader = s.Player.UploaderId != null
                    },
                    PlayerRatingChange = s.PlayerRatingChange == null ? null : new PlayerRatingChangeDto()
                    {
                        Change24h = s.PlayerRatingChange.Change24h,
                        Change10d = s.PlayerRatingChange.Change10d,
                        Change30d = s.PlayerRatingChange.Change30d
                    }
                })
                .ToListAsync(token);
    }

    private async Task<List<PlayerGameModeResult>> GetPlayerIdGameModeCounts(PlayerId playerId, CancellationToken token)
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

    public async Task<PlayerRatingDetails> GetPlayerIdPlayerRatingDetails(PlayerId playerId,
                                                                          RatingType ratingType,
                                                                          RatingCalcType ratingCalcType,
                                                                          CancellationToken token = default)
    {
        return ratingCalcType switch
        {
            RatingCalcType.Dsstats => await GetPlayerIdDsstatsPlayerRatingDetails(playerId, ratingType, token),
            RatingCalcType.Arcade => await GetPlayerIdArcadePlayerRatingDetails(playerId, ratingType, token),
            RatingCalcType.Combo => await GetPlayerIdComboPlayerRatingDetails(playerId, ratingType, token),
            _ => throw new NotImplementedException()
        };
    }

    private async Task<PlayerRatingDetails> GetPlayerIdDsstatsPlayerRatingDetails(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        return new()
        {
            Teammates = await GetPlayerIdPlayerTeammates(context, playerId, ratingType, token),
            Opponents = await GetPlayerIdPlayerOpponents(context, playerId, ratingType, token),
            AvgTeamRating = await GetPlayerIdTeamRating(context, playerId, ratingType, true, token),
            CmdrsAvgGain = await GetPlayerIdPlayerCmdrAvgGain(playerId, ratingType, TimePeriod.Past90Days, token)
        };
    }

    public async Task<List<PlayerCmdrAvgGain>> GetPlayerIdPlayerCmdrAvgGain(PlayerId playerId, RatingType ratingType, TimePeriod timePeriod, CancellationToken token)
    {
        (var startTime, var endTime) = Data.TimeperiodSelected(timePeriod);
        bool noEnd = endTime < DateTime.Today.AddDays(-2);

        var group = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where p.ToonId == playerId.ToonId
                        && p.RegionId == playerId.RegionId
                        && p.RealmId == playerId.RealmId
                        && r.GameTime > startTime
                        && (noEnd || rp.Replay.GameTime < endTime)
                        && rr.RatingType == ratingType
                    group new { rp, rpr } by rp.Race into g
                    orderby g.Count() descending
                    select new PlayerCmdrAvgGain
                    {
                        Commander = g.Key,
                        AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                        Count = g.Count(),
                        Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win)
                    };

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
                                   join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                                   where p.ToonId == playerId.ToonId
                                       && p.RealmId == playerId.RealmId
                                       && p.RegionId == playerId.RegionId
                                       && rr.RatingType == ratingType
                                       && t != rp
                                       && t.Team == rp.Team
                                   select t.ReplayPlayerRatingInfo
                                : from p in context.Players
                                  from rp in p.ReplayPlayers
                                  from t in rp.Replay.ReplayPlayers
                                  join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                                  where p.ToonId == playerId.ToonId
                                       && p.RealmId == playerId.RealmId
                                       && p.RegionId == playerId.RegionId
                                      && rr.RatingType == ratingType
                                      && t.Team != rp.Team
                                  select t.ReplayPlayerRatingInfo;

        var avgRating = await teamRatings
            .Select(s => s.Rating)
            .DefaultIfEmpty()
            .AverageAsync(token);
        return Math.Round(avgRating, 2);
    }

    private async Task<List<PlayerTeamResult>> GetPlayerIdPlayerTeammates(ReplayContext context, PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var teammateGroup = from p in context.Players
                            from rp in p.ReplayPlayers
                            from t in rp.Replay.ReplayPlayers
                            join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                            join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                            where p.ToonId == playerId.ToonId
                                && p.RealmId == playerId.RealmId
                                && p.RegionId == playerId.RegionId
                                && rr.RatingType == ratingType
                            where t != rp && t.Team == rp.Team
                            group new { t, rpr } by new { t.Player.ToonId, t.Player.RealmId, t.Player.RegionId, t.Player.Name } into g
                            orderby g.Count() descending
                            where g.Count() > 10
                            select new PlayerTeamResultHelper()
                            {
                                PlayerId = new(g.Key.ToonId, g.Key.RealmId, g.Key.RegionId),
                                Name = g.Key.Name,
                                Count = g.Count(),
                                Wins = g.Count(c => c.t.PlayerResult == PlayerResult.Win),
                                AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2)
                            };
                            

        var results = await teammateGroup
            .ToListAsync(token);


        return results.Select(s => new PlayerTeamResult()
        {
            Name = s.Name,
            PlayerId = s.PlayerId,
            Count = s.Count,
            Wins = s.Wins,
            AvgGain = s.AvgGain
        }).ToList();
    }

    private async Task<List<PlayerTeamResult>> GetPlayerIdPlayerOpponents(ReplayContext context, PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var teammateGroup = from p in context.Players
                            from rp in p.ReplayPlayers
                            from o in rp.Replay.ReplayPlayers
                            join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                            join rpr in context.RepPlayerRatings on o.ReplayPlayerId equals rpr.ReplayPlayerId
                            where p.ToonId == playerId.ToonId
                                    && p.RealmId == playerId.RealmId
                                    && p.RegionId == playerId.RegionId
                                && rr.RatingType == ratingType
                            where o.Team != rp.Team
                            group new { o, rpr } by new { o.Player.ToonId, o.Player.RealmId, o.Player.RegionId, o.Player.Name } into g
                            where g.Count() > 10
                            select new PlayerTeamResultHelper()
                            {
                                PlayerId = new(g.Key.ToonId, g.Key.RealmId, g.Key.RegionId),
                                Name = g.Key.Name,
                                Count = g.Count(),
                                Wins = g.Count(c => c.o.PlayerResult == PlayerResult.Win),
                                AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2)
                            };

        var results = await teammateGroup
            .ToListAsync(token);


        return results.Select(s => new PlayerTeamResult()
        {
            Name = s.Name,
            PlayerId = s.PlayerId,
            Count = s.Count,
            Wins = s.Wins,
            AvgGain = s.AvgGain
        }).ToList();
    }

    public async Task<PlayerDetailResponse> GetPlayerIdPlayerDetails(PlayerDetailRequest request, CancellationToken token = default)
    {
        PlayerDetailResponse response = new();

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
        bool noEnd = endDate < DateTime.Today.AddDays(-2);

        var group = request.Interest == Commander.None
            ?
                from r in context.Replays
                from rp in r.ReplayPlayers
                join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                where r.GameTime >= startDate
                    && (noEnd || r.GameTime < endDate)
                    && rr.RatingType == request.RatingType
                    && rp.Player.ToonId == request.RequestNames.ToonId
                    && rp.Player.RegionId == request.RequestNames.RegionId
                    && rp.Player.RealmId == request.RequestNames.RealmId
                group new { rp, rpr } by rp.Race into g
                select new CmdrStrengthItem()
                {
                    Commander = g.Key,
                    Matchups = g.Count(),
                    AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                    AvgRatingGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                    Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win)
                }
            :
                from r in context.Replays
                from rp in r.ReplayPlayers
                join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                where r.GameTime >= startDate
                    && (noEnd || r.GameTime < endDate)
                    && rr.RatingType == request.RatingType
                    && rp.Player.ToonId == request.RequestNames.ToonId
                    && rp.Player.RegionId == request.RequestNames.RegionId
                    && rp.Player.RealmId == request.RequestNames.RealmId
                    && rp.Race == request.Interest
                group new { rp, rpr } by rp.OppRace into g
                select new CmdrStrengthItem()
                {
                    Commander = g.Key,
                    Matchups = g.Count(),
                    AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                    AvgRatingGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                    Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win)
                }
        ;

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
