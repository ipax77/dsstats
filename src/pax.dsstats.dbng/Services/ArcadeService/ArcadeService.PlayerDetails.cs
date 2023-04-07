using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class ArcadeService
{
    public async Task<PlayerDetailsResult> GetPlayerDetails(int profileId, int regionId, RatingType ratingType, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return new()
        {
            GameModes = await GetGameModeCounts(profileId, regionId, context, token),
        };
    }

    public async Task<PlayerDetailsGroupResult> GetPlayerGroupDetails(int profileId,
                                                                      int regionId,
                                                                      RatingType ratingType,
                                                                      ReplayContext context,
                                                                      CancellationToken token)
    {
        return new PlayerDetailsGroupResult()
        {
            Teammates = await GetPlayerTeammates(profileId, regionId, ratingType, true, context, token),
            Opponents = await GetPlayerTeammates(profileId, regionId, ratingType, false, context, token),
        };
    }

    private async Task<List<PlayerTeamResult>> GetPlayerTeammates(int profileId, int regionId, RatingType ratingType, bool inTeam, ReplayContext context, CancellationToken token)
    {
        var replays = GetRatingReplays(context, ratingType);
        var teammateGroup = inTeam ?
                                from r in replays
                                from rp in r.ArcadeReplayPlayers
                                from t in r.ArcadeReplayPlayers
                                where rp.ArcadePlayer.ProfileId == profileId && rp.ArcadePlayer.RegionId == regionId
                                where t.Team == rp.Team
                                group t by t.ArcadePlayer.ProfileId into g
                                where g.Count() > 10
                                select new PlayerTeamResultHelper()
                                {
                                    ToonId = g.Key,
                                    Count = g.Count(),
                                    Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                                }
                            : from r in replays
                              from rp in r.ArcadeReplayPlayers
                              from t in r.ArcadeReplayPlayers
                              where rp.ArcadePlayer.ProfileId == profileId && rp.ArcadePlayer.RegionId == regionId
                              where t.Team != rp.Team
                              group t by t.ArcadePlayer.ProfileId into g
                              where g.Count() > 10
                              select new PlayerTeamResultHelper()
                              {
                                  ToonId = g.Key,
                                  Count = g.Count(),
                                  Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                              };

        var results = await teammateGroup
            .ToListAsync(token);

        var rtoonIds = results.Select(s => s.ToonId).ToList();
        var names = (await context.ArcadePlayers
            .Where(x => rtoonIds.Contains(x.ProfileId))
            .Select(s => new { s.ProfileId, s.Name })
            .ToListAsync(token)).ToDictionary(k => k.ProfileId, v => v.Name);

        return results.Select(s => new PlayerTeamResult()
        {
            Name = names[s.ToonId],
            ToonId = s.ToonId,
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
}
