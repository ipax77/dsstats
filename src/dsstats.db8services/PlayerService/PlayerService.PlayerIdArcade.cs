
using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class PlayerService
{
    public async Task<PlayerDetailSummary> GetPlayerPlayerIdArcadeSummary(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        PlayerDetailSummary summary = new()
        {
            GameModesPlayed = await GetPlayerIdArcadeGameModeCounts(playerId, token),
            Ratings = await GetPlayerIdArcadeRatings(playerId, token),
            Commanders = await GetPlayerIdCommandersPlayed(playerId, ratingType, token),
            ChartDtos = await GetArcadePlayerRatingChartData(playerId, ratingType, token),
            MvpInfo = await GetMvpInfo(playerId, ratingType)
        };

        (summary.CmdrPercentileRank, summary.StdPercentileRank) =
            await GetPercentileRank(
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Cmdr)?.Pos ?? 0,
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Std)?.Pos ?? 0);

        return summary;
    }

    public async Task<List<ReplayPlayerChartDto>> GetArcadePlayerRatingChartData(PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var replaysQuery = from p in context.ArcadePlayers
                           from rp in p.ArcadeReplayPlayers
                           join r in context.ArcadeReplays on rp.ArcadeReplayId equals r.ArcadeReplayId
                           join rr in context.ArcadeReplayRatings on r.ArcadeReplayId equals rr.ArcadeReplayId
                           join rpr in context.ArcadeReplayPlayerRatings on rp.ArcadeReplayPlayerId equals rpr.ArcadeReplayPlayerId
                           orderby r.CreatedAt
                           where p.ProfileId == playerId.ToonId
                            && p.RegionId == playerId.RegionId
                            && p.RealmId == playerId.RealmId
                            && rr.RatingType == ratingType
                           group new { rp, rpr } by new { Year = r.CreatedAt.Year, Week = context.Week(r.CreatedAt) } into g
                           select new ReplayPlayerChartDto()
                           {
                               Replay = new ReplayChartDto()
                               {
                                   Year = g.Key.Year,
                                   Week = g.Key.Week,
                               },
                               ReplayPlayerRatingInfo = new RepPlayerRatingChartDto()
                               {
                                   Rating = Math.Round(g.Average(a => a.rpr.Rating)),
                                   Games = g.Max(m => m.rpr.Games)
                               }
                           };
        return await replaysQuery.ToListAsync(token);
    }

    private async Task<List<PlayerRatingDetailDto>> GetPlayerIdArcadeRatings(PlayerId playerId, CancellationToken token)
    {
        return await context.ArcadePlayerRatings
                .Include(i => i.ArcadePlayerRatingChange)
                .Where(x => x.ArcadePlayer!.ProfileId == playerId.ToonId
                    && x.ArcadePlayer.RealmId == playerId.RealmId
                    && x.ArcadePlayer.RegionId == playerId.RegionId)
                .Select(s => new PlayerRatingDetailDto()
                {
                    RatingType = (RatingType)s.RatingType,
                    Rating = Math.Round(s.Rating, 2),
                    Pos = s.Pos,
                    Games = s.Games,
                    Wins = s.Wins,
                    Consistency = Math.Round(s.Consistency, 2),
                    Confidence = Math.Round(s.Confidence, 2),
                    Player = new PlayerRatingPlayerDto()
                    {
                        Name = s.ArcadePlayer!.Name,
                        ToonId = s.ArcadePlayer.ProfileId,
                        RegionId = s.ArcadePlayer.RegionId,
                        RealmId = s.ArcadePlayer.RealmId,
                    },
                    PlayerRatingChange = s.ArcadePlayerRatingChange == null ? null : new PlayerRatingChangeDto()
                    {
                        Change24h = s.ArcadePlayerRatingChange.Change24h,
                        Change10d = s.ArcadePlayerRatingChange.Change10d,
                        Change30d = s.ArcadePlayerRatingChange.Change30d
                    }
                })
                .ToListAsync(token);
    }

    private async Task<List<PlayerGameModeResult>> GetPlayerIdArcadeGameModeCounts(PlayerId playerId, CancellationToken token)
    {
        var gameModeGroup = from r in context.ArcadeReplays
                            from rp in r.ArcadeReplayPlayers
                            where rp.ArcadePlayer!.ProfileId == playerId.ToonId
                                && rp.ArcadePlayer.RealmId == playerId.RealmId
                                && rp.ArcadePlayer.RegionId == playerId.RegionId
                            group r by new { r.GameMode, r.PlayerCount } into g
                            select new PlayerGameModeResult()
                            {
                                GameMode = g.Key.GameMode,
                                PlayerCount = g.Key.PlayerCount,
                                Count = g.Count(),
                            };
        return await gameModeGroup.ToListAsync(token);
    }

    public async Task<PlayerRatingDetails> GetPlayerIdArcadePlayerRatingDetails(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        // return await DEBUGGetPlayerRatingDetails(toonId, ratingType, token);

        return new()
        {
            Teammates = await GetPlayerIdArcadePlayerTeammates(context, playerId, ratingType, token),
            Opponents = await GetPlayerIdArcadePlayerOpponents(context, playerId, ratingType, token),
            AvgTeamRating = await GetPlayerIdArcadeTeamRating(context, playerId, ratingType, true, token),
            CmdrsAvgGain = await GetPlayerIdPlayerCmdrAvgGain(playerId, ratingType, TimePeriod.Past90Days, token)
        };
    }

    private async Task<double> GetPlayerIdArcadeTeamRating(ReplayContext context, PlayerId playerId, RatingType ratingType, bool inTeam, CancellationToken token)
    {
        var teamRatings = from p in context.ArcadePlayers
                          from rp in p.ArcadeReplayPlayers
                          from t in rp.ArcadeReplay!.ArcadeReplayPlayers
                          where p.ProfileId == playerId.ToonId
                              && p.RealmId == playerId.RealmId
                              && p.RegionId == playerId.RegionId
                              && rp.ArcadeReplay!.ArcadeReplayRating != null
                              && rp.ArcadeReplay.ArcadeReplayRating.RatingType == ratingType
                              && (!inTeam || t != rp)
                              && (inTeam ? t.Team == rp.Team : t.Team != rp.Team)
                              && t.ArcadePlayer!.ProfileId > 0
                          select t.ArcadeReplayPlayerRating;

        var avgRating = await teamRatings
            .Select(s => s.Rating)
            .DefaultIfEmpty()
            .AverageAsync(token);
        return Math.Round(avgRating, 2);
    }

    private async Task<List<PlayerTeamResult>> GetPlayerIdArcadePlayerTeammates(ReplayContext context, PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var replays = GetArcadeRatingReplays(context, ratingType);
        var teammateGroup = from r in replays
                                from rp in r.ArcadeReplayPlayers
                                from t in r.ArcadeReplayPlayers
                                join p in context.ArcadePlayers on rp.ArcadePlayerId equals p.ArcadePlayerId
                                join tp in context.ArcadePlayers on t.ArcadePlayerId equals tp.ArcadePlayerId
                                where p.ProfileId == playerId.ToonId
                                    && p.RegionId == playerId.RegionId
                                    && p.RealmId == playerId.RealmId
                                    && t != rp
                                    && t.Team == rp.Team
                                    && tp.ProfileId > 0
                                group new { t, tp } by new { tp.Name, tp.ArcadePlayerId, tp.ProfileId, tp.RealmId, tp.RegionId } into g
                                orderby g.Count() descending
                                where g.Count() > 10
                                select new AracdePlayerTeamResultHelper()
                                {
                                    PlayerId = new(g.Key.ProfileId, g.Key.RealmId, g.Key.RegionId),
                                    Name = g.Key.Name,
                                    ArcadePlayerId = g.Key.ArcadePlayerId,
                                    Count = g.Count(),
                                    Wins = g.Count(c => c.t.PlayerResult == PlayerResult.Win)
                                };

        var results = await teammateGroup
            .ToListAsync(token);

        return results.Select(s => new PlayerTeamResult()
        {
            Name = s.Name,
            PlayerId = s.PlayerId,
            Count = s.Count,
            Wins = s.Wins
        }).ToList();
    }

    private async Task<List<PlayerTeamResult>> GetPlayerIdArcadePlayerOpponents(ReplayContext context, PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var replays = GetArcadeRatingReplays(context, ratingType);
        var teammateGroup = from r in replays
                              from rp in r.ArcadeReplayPlayers
                              from o in r.ArcadeReplayPlayers
                              join p in context.ArcadePlayers on rp.ArcadePlayerId equals p.ArcadePlayerId
                              join op in context.ArcadePlayers on o.ArcadePlayerId equals op.ArcadePlayerId
                              where p.ProfileId == playerId.ToonId
                                && p.RegionId == playerId.RegionId
                                && p.RealmId == playerId.RealmId
                                && o.Team != rp.Team
                                && op.ProfileId > 0
                              group new { o, op } by new { op.Name, op.ArcadePlayerId, op.ProfileId, op.RealmId, op.RegionId } into g
                              orderby g.Count() descending
                              where g.Count() > 10
                              select new AracdePlayerTeamResultHelper()
                              {
                                  PlayerId = new(g.Key.ProfileId, g.Key.RealmId, g.Key.RegionId),
                                  Name = g.Key.Name,
                                  ArcadePlayerId = g.Key.ArcadePlayerId,
                                  Count = g.Count(),
                                  Wins = g.Count(c => c.o.PlayerResult == PlayerResult.Win)
                              };

        var results = await teammateGroup
            .ToListAsync(token);

        return results.Select(s => new PlayerTeamResult()
        {
            Name = s.Name,
            PlayerId = s.PlayerId,
            Count = s.Count,
            Wins = s.Wins
        }).ToList();
    }


    private static IQueryable<ArcadeReplay> GetArcadeRatingReplays(ReplayContext context, RatingType ratingType)
    {
        var gameModes = ratingType switch
        {
            RatingType.Cmdr => new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic },
            RatingType.Std => new List<GameMode>() { GameMode.Standard },
            RatingType.CmdrTE => new List<GameMode>() { GameMode.Commanders },
            RatingType.StdTE => new List<GameMode>() { GameMode.Standard },
            _ => new List<GameMode>()
        };

        var intModes = gameModes.Select(s => (int)s).ToList();

        return context.ArcadeReplays
        .Where(x => intModes.Contains((int)x.GameMode))
        .AsNoTracking();
    }
}

internal record AracdePlayerTeamResultHelper
{
    public PlayerId PlayerId { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public int ArcadePlayerId { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
}