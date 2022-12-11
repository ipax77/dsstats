
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<PlayerDetailsResult> GetPlayerDetails(int toonId, RatingType ratingType, CancellationToken token)
    {
        return await GetPlayerDetails(new List<int>() { toonId }, ratingType, token);
    }

    public async Task<PlayerDetailsResult> GetPlayerDetails(List<int> toonIds, RatingType ratingType, CancellationToken token)
    {
        return new PlayerDetailsResult()
        {
            Ratings = await context.PlayerRatings
                .Where(x => toonIds.Contains(x.Player.ToonId)
                    && x.RatingType == ratingType)
                .ProjectTo<PlayerRatingDetailDto>(mapper.ConfigurationProvider)
                .ToListAsync(token),
            Teammates = await GetPlayerTeammates(toonIds, ratingType, true, token),
            Opponents = await GetPlayerTeammates(toonIds, ratingType, false, token),
            Matchups = await GetPlayerMatchups(toonIds, ratingType, token),
            GameModes = await GetGameModeCounts(toonIds, token)
        };
    }

    private async Task<List<PlayerGameModeResult>> GetGameModeCounts(List<int> toonIds, CancellationToken token)
    {
        var gameModeGroup = from r in context.Replays
                            from rp in r.ReplayPlayers
                            where r.Duration >= 300 && r.WinnerTeam > 0
                            where toonIds.Contains(rp.Player.ToonId)
                            group r by new { r.GameMode, r.Playercount } into g
                            select new PlayerGameModeResult()
                            {
                                GameMode = g.Key.GameMode,
                                PlayerCount = g.Key.Playercount,
                                Count = g.Count(),
                            };
        return await gameModeGroup.ToListAsync(token);
    }

    private async Task<List<PlayerMatchupInfo>> GetPlayerMatchups(List<int> toonIds, RatingType ratingType, CancellationToken token)
    {
        var replays = GetRatingReplays(context, ratingType);

        var countGroup = from r in replays
                         from rp in r.ReplayPlayers
                         where toonIds.Contains(rp.Player.ToonId)
                         group rp by new { rp.Race, rp.OppRace } into g
                         where g.Count() > 10
                         select new PlayerMatchupInfo
                         {
                             Commander = g.Key.Race,
                             Versus = g.Key.OppRace,
                             Count = g.Count(),
                             Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                         };

        var matchups = await countGroup.ToListAsync(token);

        int cmdrLimit = ratingType switch
        {
            RatingType.Std => 0,
            RatingType.Cmdr => 3,
            _ => 0
        };

        return matchups
            .Where(x => (int)x.Commander > cmdrLimit && (int)x.Versus > cmdrLimit)
            .ToList();
    }

    private async Task<List<PlayerTeamResult>> GetPlayerTeammates(List<int> toonIds, RatingType ratingType, bool inTeam, CancellationToken token)
    {
        var replays = GetRatingReplays(context, ratingType);
        var teammateGroup = inTeam ?
                                from r in replays
                                from rp in r.ReplayPlayers
                                from t in r.ReplayPlayers
                                where toonIds.Contains(rp.Player.ToonId)
                                where t.Team == rp.Team
                                group t by t.Name into g
                                where g.Count() > 10
                                select new PlayerTeamResult()
                                {
                                    Name = g.Key,
                                    Count = g.Count(),
                                    Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                                }
                            : from r in replays
                              from rp in r.ReplayPlayers
                              from t in r.ReplayPlayers
                              where toonIds.Contains(rp.Player.ToonId)
                              where t.Team != rp.Team
                              group t by t.Name into g
                              where g.Count() > 10
                              select new PlayerTeamResult()
                              {
                                  Name = g.Key,
                                  Count = g.Count(),
                                  Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                              };

        return await teammateGroup
            .ToListAsync(token);
    }

    public static IQueryable<Replay> GetRatingReplays(ReplayContext context, RatingType ratingType)
    {
        var gameModes = ratingType switch
        {
            RatingType.Cmdr => new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic },
            RatingType.Std => new List<GameMode>() { GameMode.Standard },
            _ => new List<GameMode>()
        };
        var playerCount = 6;

        return context.Replays
        .Where(r => r.Playercount == playerCount
            && r.Duration >= 300
            && r.WinnerTeam > 0
            && gameModes.Contains(r.GameMode))
        .AsNoTracking();
    }
}
