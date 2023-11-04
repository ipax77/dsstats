
using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace dsstats.services;

public partial class PlayerService
{
    public async Task<PlayerDetailSummary> GetPlayerPlayerIdArcadeSummary(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        PlayerDetailSummary summary = new()
        {
            GameModesPlayed = await GetPlayerIdArcadeGameModeCounts(playerId, token),
            Ratings = await GetPlayerIdArcadeRatings(playerId, token),
            Commanders = await GetPlayerIdCommandersPlayed(playerId, ratingType, token),
            ChartDtos = await GetArcadePlayerRatingChartData(playerId, ratingType, token)
        };

        (summary.CmdrPercentileRank, summary.StdPercentileRank) =
            await GetPercentileRank(
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Cmdr)?.Pos ?? 0,
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Std)?.Pos ?? 0);

        return summary;
    }

    public async Task<List<ReplayPlayerChartDto>> GetArcadePlayerRatingChartData(PlayerId playerId,
                                                                       RatingType ratingType,
                                                                       CancellationToken token)
    {
        string sql =
$@"SELECT YEAR(`r0`.`CreatedAt`) AS `Year`, WEEK(`r0`.`CreatedAt`) AS `Week`, ROUND(AVG(`r2`.`Rating`)) AS `Rating`, MAX(`r2`.`Games`) AS `Games`
      FROM `ArcadePlayers` AS `p`
      INNER JOIN `ArcadeReplayPlayers` AS `r` ON `p`.`ArcadePlayerId` = `r`.`ArcadePlayerId`
      INNER JOIN `ArcadeReplays` AS `r0` ON `r`.`ArcadeReplayId` = `r0`.`ArcadeReplayId`
      INNER JOIN `ArcadeReplayRatings` AS `r1` ON `r0`.`ArcadeReplayId` = `r1`.`ArcadeReplayId`
      INNER JOIN `ArcadeReplayPlayerRatings` AS `r2` ON `r`.`ArcadeReplayPlayerId` = `r2`.`ArcadeReplayPlayerId`
      WHERE (((`p`.`ProfileId` = {playerId.ToonId}) AND (`p`.`RegionId` = {playerId.RegionId})) AND (`p`.`RealmId` = {playerId.RealmId})) AND (`r1`.`RatingType` = {(int)ratingType})
      GROUP BY YEAR(`r0`.`CreatedAt`), WEEK(`r0`.`CreatedAt`)
      ORDER BY `Year`, `Week`;
";
        try
        {
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(token);

            var command = new MySqlCommand(sql, connection);

            var reader = await command.ExecuteReaderAsync();

            List<ReplayPlayerChartDto> chartData = new();
            while (await reader.ReadAsync(token))
            {
                int year = reader.GetInt32(0);
                int week = reader.GetInt32(1);
                double rating = reader.GetDouble(2);
                int games = reader.GetInt32(3);
                chartData.Add(new()
                {
                    Replay = new ReplayChartDto()
                    {
                        Year = year,
                        Week = week
                    },
                    ReplayPlayerRatingInfo = new RepPlayerRatingChartDto()
                    {
                        Rating = (float)rating,
                        Games = games
                    }
                });
            }
            return chartData;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting player rating chart data: {error}", ex.Message);
        }
        return new();
    }

    public async Task<List<ReplayPlayerChartDto>> GetArcadePlayerRatingChartData_(PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var replaysQuery = from p in context.ArcadePlayers
                           from rp in p.ArcadeReplayPlayers
                           orderby rp.ArcadeReplay!.CreatedAt
                           where p.ProfileId == playerId.ToonId
                            && p.RegionId == playerId.RegionId
                            && p.RealmId == playerId.RealmId
                            && rp.ArcadeReplay!.ArcadeReplayRating != null
                            && rp.ArcadeReplay.ArcadeReplayRating.RatingType == ratingType
                           group rp by new { Year = rp.ArcadeReplay!.CreatedAt.Year, Week = context.Week(rp.ArcadeReplay.CreatedAt) } into g
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
                                   Rating = Math.Round(g.Average(a => a.ArcadeReplayPlayerRating!.Rating)),
                                   Games = g.Max(m => m.ArcadeReplayPlayerRating!.Games)
                               }
                           };
        return await replaysQuery.ToListAsync(token);
    }

    private async Task<List<PlayerRatingDetailDto>> GetPlayerIdArcadeRatings(PlayerId playerId, CancellationToken token)
    {
        return await context.ArcadePlayerRatings
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
                                GameMode = (GameMode)g.Key.GameMode,
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
            Teammates = await GetPlayerIdArcadePlayerTeammates(context, playerId, ratingType, true, token),
            Opponents = await GetPlayerIdArcadePlayerTeammates(context, playerId, ratingType, false, token),
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
                              && (inTeam ? t != rp : true)
                              && (inTeam ? t.Team == rp.Team : t.Team != rp.Team)
                              && t.ArcadePlayer!.ProfileId > 0
                          select t.ArcadeReplayPlayerRating;

        var avgRating = await teamRatings
            .Select(s => s.Rating)
            .DefaultIfEmpty()
            .AverageAsync(token);
        return Math.Round(avgRating, 2);
    }

    private async Task<List<PlayerTeamResult>> GetPlayerIdArcadePlayerTeammates(ReplayContext context, PlayerId playerId, RatingType ratingType, bool inTeam, CancellationToken token)
    {
        var replays = GetArcadeRatingReplays(context, ratingType);
        var teammateGroup = inTeam ?
                                from r in replays
                                from rp in r.ArcadeReplayPlayers
                                from t in r.ArcadeReplayPlayers
                                where rp.ArcadePlayer!.ProfileId == playerId.ToonId
                                    && rp.ArcadePlayer.RegionId == playerId.RegionId
                                    && rp.ArcadePlayer.RealmId == playerId.RealmId
                                    && t != rp
                                    && t.Team == rp.Team
                                    && t.ArcadePlayer!.ProfileId > 0
                                group t by new { t.ArcadePlayer!.Name, t.ArcadePlayer.ArcadePlayerId, t.ArcadePlayer.ProfileId, t.ArcadePlayer.RealmId, t.ArcadePlayer.RegionId } into g
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
                              where rp.ArcadePlayer!.ProfileId == playerId.ToonId
                                && rp.ArcadePlayer.RegionId == playerId.RegionId
                                && rp.ArcadePlayer.RealmId == playerId.RealmId
                                && t.Team != rp.Team
                                && t.ArcadePlayer!.ProfileId > 0
                              group t by new { t.ArcadePlayer!.Name, t.ArcadePlayer.ArcadePlayerId, t.ArcadePlayer.ProfileId, t.ArcadePlayer.RealmId, t.ArcadePlayer.RegionId } into g
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
        .Where(x => intModes.Contains(x.GameMode))
        .AsNoTracking();
    }
}
