
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using System.Globalization;

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
                .Where(x => toonIds.Contains(x.Player.ToonId))
                .ProjectTo<PlayerRatingDetailDto>(mapper.ConfigurationProvider)
                .ToListAsync(token),
            GameModes = await GetGameModeCounts(toonIds, token),
            Matchups = await GetPlayerMatchups(toonIds, ratingType, token),
        };
    }

    public async Task<PlayerDetailsGroupResult> GetPlayerGroupDetails(int toonId, RatingType ratingType, CancellationToken token)
    {
        return await GetPlayerGroupDetails(new List<int>() { toonId }, ratingType, token);
    }

    public async Task<PlayerDetailsGroupResult> GetPlayerGroupDetails(List<int> toonIds, RatingType ratingType, CancellationToken token)
    {
        return new PlayerDetailsGroupResult()
        {
            Teammates = await GetPlayerTeammates(toonIds, ratingType, true, token),
            Opponents = await GetPlayerTeammates(toonIds, ratingType, false, token),
        };
    }

    private async Task<List<PlayerGameModeResult>> GetGameModeCounts(List<int> toonIds, CancellationToken token)
    {
        var gameModeGroup = from r in context.Replays
                            from rp in r.ReplayPlayers
                                //where r.Duration >= 300 && r.WinnerTeam > 0
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

    public async Task<List<PlayerMatchupInfo>> GetPlayerMatchups(int toonId, RatingType ratingType, CancellationToken token)
    {
        return await GetPlayerMatchups(new List<int>() { toonId }, ratingType, token);
    }

    public async Task<List<PlayerMatchupInfo>> GetPlayerMatchups(List<int> toonIds, RatingType ratingType, CancellationToken token)
    {
        var replays = GetRatingReplays(context, ratingType);

        var countGroup = from r in replays
                         from rp in r.ReplayPlayers
                         where toonIds.Contains(rp.Player.ToonId)
                         group rp by new { rp.Race, rp.OppRace } into g
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
            RatingType.StdTE => 0,
            RatingType.CmdrTE => 3,
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
                                group t by new { t.Player.ToonId, t.Player.Name } into g
                                where g.Count() > 10
                                select new PlayerTeamResultHelper()
                                {
                                    ToonId = g.Key.ToonId,
                                    Name = g.Key.Name,
                                    Count = g.Count(),
                                    Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                                }
                            : from r in replays
                              from rp in r.ReplayPlayers
                              from t in r.ReplayPlayers
                              where toonIds.Contains(rp.Player.ToonId)
                              where t.Team != rp.Team
                              group t by new { t.Player.ToonId, t.Player.Name } into g
                              where g.Count() > 10
                              select new PlayerTeamResultHelper()
                              {
                                  ToonId = g.Key.ToonId,
                                  Name = g.Key.Name,
                                  Count = g.Count(),
                                  Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                              };

        var results = await teammateGroup
            .ToListAsync(token);

        return results.Select(s => new PlayerTeamResult()
        {
            Name = s.Name,
            ToonId = s.ToonId,
            Count = s.Count,
            Wins = s.Wins
        }).ToList();
    }

    private async Task<List<shared.PlayerRatingChange>> GetRatingChanges(List<int> toonIds, CancellationToken token)
    {
        DateTime fromDate = DateTime.Today.AddDays(-30);
#pragma warning disable CS8602 // Dereference of a possibly null reference.

        var plchanges = from p in context.Players
                        from rp in p.ReplayPlayers
                        where rp.Replay.GameTime > fromDate
                          && toonIds.Contains(p.ToonId)
                          && rp.ReplayPlayerRatingInfo != null
                        group rp.ReplayPlayerRatingInfo by rp.ReplayPlayerRatingInfo.ReplayRatingInfo.RatingType into g
                        select new shared.PlayerRatingChange
                        {
                            RatingType = g.Key,
                            Count = g.Count(),
                            Sum = MathF.Round(g.Sum(s => s.RatingChange), 2)
                        };

        //var changes = from r in context.Replays
        //          from rpr in r.ReplayRatingInfo.RepPlayerRatings
        //          from rp in r.ReplayPlayers
        //          where r.GameTime > DateTime.Today.AddDays(-30)
        //            && toonIds.Contains(rp.Player.ToonId)
        //          group rpr by rpr.ReplayRatingInfo.RatingType into g
        //          select new PlayerRatingChange
        //          {
        //              RatingType = g.Key,
        //              Count = g.Count(),
        //              Sum = MathF.Round(g.Sum(s => s.RatingChange), 2)
        //          };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        return await plchanges.ToListAsync(token);
    }

    private static IQueryable<Replay> GetRatingReplays(ReplayContext context, RatingType ratingType)
    {
        var gameModes = ratingType switch
        {
            RatingType.Cmdr => new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic },
            RatingType.Std => new List<GameMode>() { GameMode.Standard },
            RatingType.CmdrTE => new List<GameMode>() { GameMode.Commanders },
            RatingType.StdTE => new List<GameMode>() { GameMode.Standard },
            _ => new List<GameMode>()
        };

        bool te = ratingType switch
        {
            RatingType.Cmdr => false,
            RatingType.Std => false,
            _ => true
        };

        var playerCount = 6;

        return context.Replays
        .Where(r => r.Playercount == playerCount
            && r.Duration >= 300
            && r.WinnerTeam > 0
            && r.TournamentEdition == te
            && gameModes.Contains(r.GameMode))
        .AsNoTracking();
    }

    public async Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId, RatingType ratingType)
    {
        if (!Data.IsMaui)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var replaysQuery = from p in context.Players
                               from rp in p.ReplayPlayers
                               orderby rp.Replay.GameTime
                               where p.ToonId == playerId.ToonId
                                && p.RegionId == playerId.RegionId
                                && p.RealmId == playerId.RealmId
                                && rp.Replay.ReplayRatingInfo != null
                                && rp.Replay.ReplayRatingInfo.RatingType == ratingType
                               //group rp by new { rp.Replay.GameTime.Year, rp.Replay.GameTime.Month } into g
                               group rp by new { Year = rp.Replay.GameTime.Year, Week = context.Week(rp.Replay.GameTime) } into g
                               select new ReplayPlayerChartDto()
                               {
                                   Replay = new ReplayChartDto()
                                   {
                                       //GameTime = new DateTime(g.Key.Year, g.Key.Month, 1),
                                       Year = g.Key.Year,
                                       Week = g.Key.Week
                                   },
                                   ReplayPlayerRatingInfo = new RepPlayerRatingChartDto()
                                   {
                                       Rating = MathF.Round(g.Average(a => a.ReplayPlayerRatingInfo.Rating)),
                                       Games = g.Max(m => m.ReplayPlayerRatingInfo.Games)
                                   }
                               };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            return await replaysQuery.ToListAsync();
        } 
        else
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var replaysQuery = from p in context.Players
                               from rp in p.ReplayPlayers
                               orderby rp.Replay.GameTime
                               where p.ToonId == playerId.ToonId
                                && p.RegionId == playerId.RegionId
                                && p.RealmId == playerId.RealmId
                                && rp.Replay.ReplayRatingInfo != null
                                && rp.Replay.ReplayRatingInfo.RatingType == ratingType
                               //group rp by new { rp.Replay.GameTime.Year, rp.Replay.GameTime.Month } into g
                               group rp by new { Year = rp.Replay.GameTime.Year, Week = context.Strftime("'%W'", rp.Replay.GameTime) } into g
                               select new ReplayPlayerChartDto()
                               {
                                   Replay = new ReplayChartDto()
                                   {
                                       //GameTime = new DateTime(g.Key.Year, g.Key.Month, 1),
                                       Year = g.Key.Year,
                                       Week = g.Key.Week
                                   },
                                   ReplayPlayerRatingInfo = new RepPlayerRatingChartDto()
                                   {
                                       Rating = MathF.Round(g.Average(a => a.ReplayPlayerRatingInfo.Rating)),
                                       Games = g.Max(m => m.ReplayPlayerRatingInfo.Games)
                                   }
                               };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            return await replaysQuery.ToListAsync();
        }
    }
}

internal record PlayerTeamResultHelper
{
    public int ToonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Wins { get; set; }
}