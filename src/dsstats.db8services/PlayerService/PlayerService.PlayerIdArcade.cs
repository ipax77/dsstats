
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
            ChartDtos = await GetPlayerRatingChartData(playerId, RatingCalcType.Arcade, ratingType, token),
            MvpInfo = await GetMvpInfo(playerId, ratingType)
        };

        (summary.CmdrPercentileRank, summary.StdPercentileRank) =
            await GetPercentileRank(
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Cmdr)?.Pos ?? 0,
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Std)?.Pos ?? 0,
                RatingCalcType.Arcade);

        return summary;
    }

    private async Task<List<PlayerRatingDetailDto>> GetPlayerIdArcadeRatings(PlayerId playerId, CancellationToken token)
    {
        return await context.ArcadePlayerRatings
                .Include(i => i.ArcadePlayerRatingChange)
                .Where(x => x.Player!.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId)
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
                        Name = s.Player!.Name,
                        ToonId = s.Player.ToonId,
                        RegionId = s.Player.RegionId,
                        RealmId = s.Player.RealmId,
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
        var gameModeGroup = from rp in context.ArcadeReplayDsPlayers
                            where rp.Player!.ToonId == playerId.ToonId
                                && rp.Player.RealmId == playerId.RealmId
                                && rp.Player.RegionId == playerId.RegionId
                            group rp.ArcadeReplay by new { rp.ArcadeReplay!.GameMode, rp.ArcadeReplay.PlayerCount } into g
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
        var teamRatings = from rp in context.ArcadeReplayDsPlayers
                          from t in rp.ArcadeReplay!.ArcadeReplayDsPlayers
                          where rp.Player!.ToonId == playerId.ToonId
                              && rp.Player.RealmId == playerId.RealmId
                              && rp.Player.RegionId == playerId.RegionId
                              && rp.ArcadeReplay!.ArcadeReplayRating != null
                              && rp.ArcadeReplay.ArcadeReplayRating.RatingType == ratingType
                              && (!inTeam || t != rp)
                              && (inTeam ? t.Team == rp.Team : t.Team != rp.Team)
                              && t.Player!.ToonId > 0
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
                            from rp in r.ArcadeReplayDsPlayers
                            from t in r.ArcadeReplayDsPlayers
                            join p in context.Players on rp.PlayerId equals p.PlayerId
                            join tp in context.Players on t.PlayerId equals tp.PlayerId
                            join rpr in context.ArcadeReplayDsPlayerRatings on rp.ArcadeReplayDsPlayerId equals rpr.ArcadeReplayDsPlayerId
                            where p.ToonId == playerId.ToonId
                                && p.RegionId == playerId.RegionId
                                && p.RealmId == playerId.RealmId
                                && t != rp
                                && t.Team == rp.Team
                                && tp.ToonId > 0
                            group new { t, tp, rpr } by new { tp.Name, tp.PlayerId, tp.ToonId, tp.RealmId, tp.RegionId } into g
                            orderby g.Count() descending
                            where g.Count() > 10
                            select new AracdePlayerTeamResultHelper()
                            {
                                PlayerId = new(g.Key.ToonId, g.Key.RealmId, g.Key.RegionId),
                                Name = g.Key.Name,
                                ArcadePlayerId = g.Key.PlayerId,
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

    private async Task<List<PlayerTeamResult>> GetPlayerIdArcadePlayerOpponents(ReplayContext context, PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var replays = GetArcadeRatingReplays(context, ratingType);
        var teammateGroup = from r in replays
                            from rp in r.ArcadeReplayDsPlayers
                            from o in r.ArcadeReplayDsPlayers
                            join p in context.Players on rp.PlayerId equals p.PlayerId
                            join op in context.Players on o.PlayerId equals op.PlayerId
                            join rpr in context.ArcadeReplayDsPlayerRatings on o.ArcadeReplayDsPlayerId equals rpr.ArcadeReplayDsPlayerId
                            where p.ToonId == playerId.ToonId
                              && p.RegionId == playerId.RegionId
                              && p.RealmId == playerId.RealmId
                              && o.Team != rp.Team
                              && op.ToonId > 0
                            group new { o, op, rpr } by new { op.Name, op.PlayerId, op.ToonId, op.RealmId, op.RegionId } into g
                            orderby g.Count() descending
                            where g.Count() > 10
                            select new AracdePlayerTeamResultHelper()
                            {
                                PlayerId = new(g.Key.ToonId, g.Key.RealmId, g.Key.RegionId),
                                Name = g.Key.Name,
                                ArcadePlayerId = g.Key.PlayerId,
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
    public double AvgGain { get; set; }
}