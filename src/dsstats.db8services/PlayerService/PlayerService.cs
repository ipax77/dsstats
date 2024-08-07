﻿using AutoMapper;
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace dsstats.db8services;

public partial class PlayerService : IPlayerService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;
    private readonly IMapper mapper;
    private readonly ILogger<PlayerService> logger;
    private readonly string connectionString;
    private readonly bool IsSqlite;

    public PlayerService(ReplayContext context,
                         IOptions<DbImportOptions> dbOptions,
                         IMemoryCache memoryCache,
                         IMapper mapper,
                         ILogger<PlayerService> logger)
    {
        this.context = context;
        this.connectionString = dbOptions.Value.ImportConnectionString;
        this.IsSqlite = dbOptions.Value.IsSqlite;
        this.memoryCache = memoryCache;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task<string?> GetPlayerIdName(PlayerId playerId)
    {
        return await context.Players
            .Where(x => x.ToonId == playerId.ToonId
                && x.RealmId == playerId.RealmId
                && x.RegionId == playerId.RegionId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync();
    }

    public async Task<PlayerDetailResponse> GetPlayerDetails(PlayerDetailRequest request, CancellationToken token = default)
    {
        PlayerDetailResponse response = new();

        if ((int)request.TimePeriod < 3)
        {
            request.TimePeriod = TimePeriod.Past90Days;
        }

        response.CmdrStrengthItems = await GetCmdrStrengthItems(context, request, token);

        return response;
    }

    private async Task<List<CmdrStrengthItem>> GetCmdrStrengthItems(ReplayContext context, PlayerDetailRequest request, CancellationToken token)
    {
        (var startDate, var endDate) = Data.TimeperiodSelected(request.TimePeriod);
        bool noEnd = endDate < DateTime.Today.AddDays(-2);

        var group = request.Interest == Commander.None
            ?
                from r in context.Replays
                from rp in r.ReplayPlayers
                join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                where r.GameTime >= startDate
                    && (noEnd || r.GameTime < endDate)
                    && rp.Player.ToonId == request.RequestNames.ToonId
                    && rp.Player.RealmId == request.RequestNames.RealmId
                    && rp.Player.RegionId == request.RequestNames.RegionId
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
                 join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                 where r.GameTime >= startDate
                     && (noEnd || r.GameTime < endDate)
                     && rp.Player.ToonId == request.RequestNames.ToonId
                     && rp.Player.RealmId == request.RequestNames.RealmId
                     && rp.Player.RegionId == request.RequestNames.RegionId
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

    public async Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId,
                                                                           RatingType ratingType,
                                                                           CancellationToken token)
    {
        var query = IsSqlite ? from p in context.Players
                               from rp in p.ReplayPlayers
                               join r in context.Replays on rp.ReplayId equals r.ReplayId
                               join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                               join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                               where p.ToonId == playerId.ToonId
                                && p.RealmId == playerId.RealmId
                                && p.RegionId == playerId.RegionId
                                && rr.RatingType == ratingType
                               group new { r, rpr } by new { r.GameTime.Year, Week = context.Strftime("'%W'", r.GameTime) } into g
                               select new ReplayPlayerChartDto()
                               {
                                   Replay = new(g.Key.Year, g.Key.Week),
                                   ReplayPlayerRatingInfo = new()
                                   {
                                       Rating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                                       Games = g.Max(m => m.rpr.Games)
                                   }
                               }
                    : from p in context.Players
                      from rp in p.ReplayPlayers
                      join r in context.Replays on rp.ReplayId equals r.ReplayId
                      join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                      join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                      where p.ToonId == playerId.ToonId
                       && p.RealmId == playerId.RealmId
                       && p.RegionId == playerId.RegionId
                       && rr.RatingType == ratingType
                      group new { r, rpr } by new { r.GameTime.Year, Week = context.Week(r.GameTime) } into g
                      select new ReplayPlayerChartDto()
                      {
                          Replay = new(g.Key.Year, g.Key.Week),
                          ReplayPlayerRatingInfo = new()
                          {
                              Rating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                              Games = g.Max(m => m.rpr.Games)
                          }
                      };

        return await query.ToListAsync();
    }

    public async Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId,
                                                                       RatingCalcType ratingCalcType,
                                                                       RatingType ratingType,
                                                                       CancellationToken token)
    {
        if (IsSqlite)
        {
            return await GetSqlitePlayerRatingChartData(playerId, ratingType, token);
        }

        return ratingCalcType switch
        {
            RatingCalcType.Combo => await GetComboPlayerRatingChartData(playerId, ratingType, token),
            RatingCalcType.Arcade => await GetArcadelayerRatingChartData(playerId, ratingType, token),
            RatingCalcType.Dsstats => await GetDsstatsPlayerRatingChartData(playerId, ratingType, token),
            _ => new()
        };
    }

    private async Task<List<ReplayPlayerChartDto>> GetArcadelayerRatingChartData(PlayerId playerId,
                                                           RatingType ratingType,
                                                           CancellationToken token)
    {
        var query = from rp in context.ArcadeReplayDsPlayers
                    join r in context.ArcadeReplays on rp.ArcadeReplayId equals r.ArcadeReplayId
                    join rr in context.ArcadeReplayRatings on r.ArcadeReplayId equals rr.ArcadeReplayId
                    join rpr in context.ArcadeReplayDsPlayerRatings on rp.ArcadeReplayDsPlayerId equals rpr.ArcadeReplayDsPlayerId
                    where rp.Player!.ToonId == playerId.ToonId
                     && rp.Player.RealmId == playerId.RealmId
                     && rp.Player.RegionId == playerId.RegionId
                     && rr.RatingType == ratingType
                    group new { r, rpr } by new { r.CreatedAt.Year, Week = context.Week(r.CreatedAt) } into g
                    select new ReplayPlayerChartDto()
                    {
                        Replay = new(g.Key.Year, g.Key.Week),
                        ReplayPlayerRatingInfo = new()
                        {
                            Rating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                            Games = g.Max(m => m.rpr.Games)
                        }
                    };
        return await query.ToListAsync(token);
    }

    private async Task<List<ReplayPlayerChartDto>> GetComboPlayerRatingChartData(PlayerId playerId,
                                                           RatingType ratingType,
                                                           CancellationToken token)
    {
        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                     && rr.RatingType == ratingType
                    group new { r, rpr } by new { r.GameTime.Year, Week = context.Week(r.GameTime) } into g
                    select new ReplayPlayerChartDto()
                    {
                        Replay = new(g.Key.Year, g.Key.Week),
                        ReplayPlayerRatingInfo = new()
                        {
                            Rating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                            Games = g.Max(m => m.rpr.Games)
                        }
                    };
        return await query.ToListAsync(token);
    }

    private async Task<List<ReplayPlayerChartDto>> GetDsstatsPlayerRatingChartData(PlayerId playerId,
                                                               RatingType ratingType,
                                                               CancellationToken token)
    {
        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                     && rr.RatingType == ratingType
                    group new { r, rpr } by new { r.GameTime.Year, Week = context.Week(r.GameTime) } into g
                    select new ReplayPlayerChartDto()
                    {
                        Replay = new(g.Key.Year, g.Key.Week),
                        ReplayPlayerRatingInfo = new()
                        {
                            Rating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                            Games = g.Max(m => m.rpr.Games)
                        }
                    };
        return await query.ToListAsync(token);
    }

    private async Task<List<ReplayPlayerChartDto>> GetSqlitePlayerRatingChartData(PlayerId playerId,
                                                                   RatingType ratingType,
                                                                   CancellationToken token)
    {
        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                     && rr.RatingType == ratingType
                    group new
                    {
                        r,
                        rpr
                    } by new
                    {
                        r.GameTime.Year,
                        Week = context.Strftime("'%W'", r.GameTime)
                    } into g
                    select new ReplayPlayerChartDto()
                    {
                        Replay = new(g.Key.Year, g.Key.Week),
                        ReplayPlayerRatingInfo = new()
                        {
                            Rating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                            Games = g.Max(m => m.rpr.Games)
                        }
                    };
        return await query.ToListAsync(token);
    }

}
