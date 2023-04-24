using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.dbng.Services;

public partial class ArcadeService
{
    public async Task<ArcadePlayerDetails> GetPlayerDetails(ArcadePlayerId playerId, CancellationToken token)
    {
        var arcadePlayerId = await GetArcadePalyerId(playerId, token);
        if (arcadePlayerId == 0)
        {
            return new();
        }
        return await GetPlayerDetails(arcadePlayerId, token);
    }

    private async Task<int> GetArcadePalyerId(ArcadePlayerId playerId, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.ArcadePlayers
            .Where(x => x.ProfileId == playerId.ProfileId
                && x.RealmId == playerId.RealmId
                && x.RegionId == playerId.RegionId)
            .Select(s => s.ArcadePlayerId)
            .FirstOrDefaultAsync(token);
    }

    public async Task<ArcadePlayerDetails> GetPlayerDetails(int arcadePlayerId, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return new()
        {
            ArcadePlayer = await context.ArcadePlayers
                .Where(x => x.ArcadePlayerId == arcadePlayerId)
                .ProjectTo<ArcadePlayerDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(),
            PlayerRatings = await context.ArcadePlayerRatings
                .Where(x => x.ArcadePlayerId == arcadePlayerId)
                .ProjectTo<ArcadePlayerRatingDetailDto>(mapper.ConfigurationProvider)
                .ToListAsync(token)
        };
    }

    private async Task<List<KeyValuePair<GameMode, int>>> GetGameModes(int arcadePlayerId, ReplayContext context, CancellationToken token)
    {
        var group = from p in context.ArcadePlayers
                    from rp in p.ArcadeReplayPlayers
                    where p.ArcadePlayerId == arcadePlayerId
                    group rp.ArcadeReplay by rp.ArcadeReplay.GameMode into g
                    select new
                    {
                        g.Key,
                        Count = g.Count()
                    };

        var gamemodes = await group.ToListAsync(token);

        if (gamemodes.Any())
        {
            return gamemodes.Select(s => new KeyValuePair<GameMode, int>(s.Key, s.Count)).ToList();
        }
        else
        {
            return new();
        }
    }

    public async Task<ArcadePlayerMoreDetails> GetMorePlayerDatails(ArcadePlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return new()
        {
            Teammates = await GetPlayerTeammates(playerId, ratingType, true, context, token),
            Opponents = await GetPlayerTeammates(playerId, ratingType, false, context, token),
            AvgTeamRating = await GetTeamRating(playerId, ratingType, true, context, token)
        };
    }

    private async Task<double> GetTeamRating(ArcadePlayerId playerId, RatingType ratingType, bool inTeam, ReplayContext context, CancellationToken token)
    {
        var teamRatings = inTeam ? from p in context.ArcadePlayers
                                   from rp in p.ArcadeReplayPlayers
                                   from t in rp.ArcadeReplay.ArcadeReplayPlayers
                                   where p.ProfileId == playerId.ProfileId
                                       && p.RealmId == playerId.RealmId
                                       && p.RegionId == playerId.RegionId
                                       && rp.ArcadeReplay.ArcadeReplayRating != null
                                       && rp.ArcadeReplay.ArcadeReplayRating.RatingType == ratingType
                                       && t != rp
                                       && t.Team == rp.Team
                                       && t.ArcadePlayer.ProfileId > 0
                                   select t.ArcadeReplayPlayerRating
                                : from p in context.ArcadePlayers
                                  from rp in p.ArcadeReplayPlayers
                                  from t in rp.ArcadeReplay.ArcadeReplayPlayers
                                  where p.ProfileId == playerId.ProfileId
                                       && p.RealmId == playerId.RealmId
                                       && p.RegionId == playerId.RegionId
                                       && rp.ArcadeReplay.ArcadeReplayRating != null
                                       && rp.ArcadeReplay.ArcadeReplayRating.RatingType == ratingType
                                       && t.Team != rp.Team
                                       && t.ArcadePlayer.ProfileId > 0
                                  select t.ArcadeReplayPlayerRating;

        var avgRating = await teamRatings
            .Select(s => s.Rating)
            .DefaultIfEmpty()
            .AverageAsync(token);
        return Math.Round(avgRating, 2);
    }

    private async Task<List<PlayerTeamResult>> GetPlayerTeammates(ArcadePlayerId playerId, RatingType ratingType, bool inTeam, ReplayContext context, CancellationToken token)
    {
        var replays = GetRatingReplays(context, ratingType);
        var teammateGroup = inTeam ?
                                from r in replays
                                from rp in r.ArcadeReplayPlayers
                                from t in r.ArcadeReplayPlayers
                                where rp.ArcadePlayer.ProfileId == playerId.ProfileId
                                    && rp.ArcadePlayer.RegionId == playerId.RegionId
                                    && rp.ArcadePlayer.RealmId == playerId.RealmId
                                    && t != rp
                                    && t.Team == rp.Team
                                    && t.ArcadePlayer.ProfileId > 0
                                group t by new { t.ArcadePlayer.Name, t.ArcadePlayer.ArcadePlayerId, t.ArcadePlayer.ProfileId, t.ArcadePlayer.RealmId, t.ArcadePlayer.RegionId } into g
                                where g.Count() > 10
                                select new AracdePlayerTeamResultHelper()
                                {
                                    PlayerId = new(g.Key.ProfileId, g.Key.RealmId, g.Key.RegionId),
                                    Name = g.Key.Name,
                                    ArcadePlayerId = g.Key.ArcadePlayerId,
                                    Count = g.Count(),
                                    Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                                }
                            : from r in replays
                              from rp in r.ArcadeReplayPlayers
                              from t in r.ArcadeReplayPlayers
                              where rp.ArcadePlayer.ProfileId == playerId.ProfileId
                                && rp.ArcadePlayer.RegionId == playerId.RegionId
                                && rp.ArcadePlayer.RealmId == playerId.RealmId
                                && t.Team != rp.Team
                                && t.ArcadePlayer.ProfileId > 0
                              group t by new { t.ArcadePlayer.Name, t.ArcadePlayer.ArcadePlayerId, t.ArcadePlayer.ProfileId, t.ArcadePlayer.RealmId, t.ArcadePlayer.RegionId } into g
                              where g.Count() > 10
                              select new AracdePlayerTeamResultHelper()
                              {
                                  PlayerId = new(g.Key.ProfileId, g.Key.RealmId, g.Key.RegionId),
                                  Name = g.Key.Name,
                                  ArcadePlayerId = g.Key.ArcadePlayerId,
                                  Count = g.Count(),
                                  Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                              };

        var results = await teammateGroup
            .ToListAsync(token);

        return results.Select(s => new PlayerTeamResult()
        {
            Name = s.Name,
            ToonId = s.ArcadePlayerId,
            Count = s.Count,
            Wins = s.Wins
        }).ToList();
    }

    private static IQueryable<ArcadeReplay> GetRatingReplays(ReplayContext context, RatingType ratingType)
    {
        var gameModes = ratingType switch
        {
            RatingType.Cmdr => new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic },
            RatingType.Std => new List<GameMode>() { GameMode.Standard },
            RatingType.CmdrTE => new List<GameMode>() { GameMode.Commanders },
            RatingType.StdTE => new List<GameMode>() { GameMode.Standard },
            _ => new List<GameMode>()
        };

        return context.ArcadeReplays
        .Where(x => gameModes.Contains(x.GameMode))
        .AsNoTracking();
    }

    private async Task<List<PlayerGameModeResult>> GetGameModeCounts(int profileId, int regionId, ReplayContext context, CancellationToken token)
    {
        var gameModeGroup = from r in context.ArcadeReplays
                            from rp in r.ArcadeReplayPlayers
                            where rp.ArcadePlayer.ProfileId == profileId && rp.ArcadePlayer.RegionId == regionId
                            group r by new { r.GameMode, r.PlayerCount } into g
                            select new PlayerGameModeResult()
                            {
                                GameMode = g.Key.GameMode,
                                PlayerCount = g.Key.PlayerCount,
                                Count = g.Count(),
                            };
        return await gameModeGroup.ToListAsync(token);
    }

    public async Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId, RatingType ratingType)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var replaysQuery = from p in context.ArcadePlayers
                           from rp in p.ArcadeReplayPlayers
                           orderby rp.ArcadeReplay.CreatedAt
                           where p.ProfileId == playerId.ToonId
                            && p.RegionId == playerId.RegionId
                            && p.RealmId == playerId.RealmId
                            && rp.ArcadeReplay.ArcadeReplayRating != null
                            && rp.ArcadeReplay.ArcadeReplayRating.RatingType == ratingType
                           group rp by new { Year = rp.ArcadeReplay.CreatedAt.Year, Week = context.Week(rp.ArcadeReplay.CreatedAt) } into g
                           select new ReplayPlayerChartDto()
                           {
                               Replay = new ReplayChartDto()
                               {
                                   // GameTime = new DateTime(g.Key.Year, g.Key.Month, 1),
                                   Year = g.Key.Year,
                                   Week = g.Key.Week,
                               },
                               ReplayPlayerRatingInfo = new RepPlayerRatingChartDto()
                               {
                                   Rating = MathF.Round(g.Average(a => a.ArcadeReplayPlayerRating.Rating)),
                                   Games = g.Max(m => m.ArcadeReplayPlayerRating.Games)
                               }
                           };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        return await replaysQuery.ToListAsync();
    }
}

internal record AracdePlayerTeamResultHelper
{
    public ArcadePlayerId PlayerId { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public int ArcadePlayerId { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
}