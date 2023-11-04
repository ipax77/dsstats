
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using dsstats.shared;
using dsstats.db;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data.SQLite;

namespace dsstats.services;

public partial class PlayerService
{
    public async Task<PlayerDetailSummary> GetPlayerPlayerIdSummary(PlayerId playerId,
                                                                    RatingType ratingType,
                                                                    RatingCalcType ratingCalcType,
                                                                    CancellationToken token = default)
    {
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
            ChartDtos = await GetPlayerRatingChartData(playerId, ratingType, token)
        };

        (summary.CmdrPercentileRank, summary.StdPercentileRank) =
            await GetPercentileRank(
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Cmdr)?.Pos ?? 0,
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Std)?.Pos ?? 0);

        return summary;
    }

    public async Task<List<CommanderInfo>> GetPlayerIdCommandersPlayed(PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var sql =
$@"SELECT `r`.`Race` AS `Cmdr`, COUNT(*) AS `Count`
      FROM `Players` AS `p`
      INNER JOIN `ReplayPlayers` AS `r` ON `p`.`PlayerId` = `r`.`PlayerId`
      {(ratingType != RatingType.None ? "INNER JOIN `ReplayRatings` AS `r1` ON `r1`.`ReplayId` = `r`.`ReplayId`" : "")}
      WHERE (((`p`.`ToonId` = {playerId.ToonId}) AND (`p`.`RegionId` = {playerId.RegionId})) AND (`p`.`RealmId` = {playerId.RealmId}) {(ratingType != RatingType.None ? $"AND (`r1`.`RatingType` = {(int)ratingType})" : "")})
      GROUP BY `r`.`Race`
      ORDER BY `Count` DESC;
";
        if (IsSqlite)
        {
            return await GetSqlitePlayerIdCommandersPlayed(sql, token);
        }

        try
        {
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(token);

            var command = new MySqlCommand(sql, connection);

            var reader = await command.ExecuteReaderAsync();

            List<CommanderInfo> cmdrCounts = new();
            while (await reader.ReadAsync(token))
            {
                int cmdr = reader.GetInt32(0);
                int count = reader.GetInt32(1);
                cmdrCounts.Add(new()
                {
                    Cmdr = (Commander)cmdr,
                    Count = count
                });
            }
            return cmdrCounts;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting cmdr count: {error}", ex.Message);
        }
        return new();

    }

    private async Task<List<CommanderInfo>> GetSqlitePlayerIdCommandersPlayed(string sql, CancellationToken token)
    {
        try
        {
            var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync(token);

            var command = new SQLiteCommand(sql, connection);

            var reader = await command.ExecuteReaderAsync();

            List<CommanderInfo> cmdrCounts = new();
            while (await reader.ReadAsync(token))
            {
                int cmdr = reader.GetInt32(0);
                int count = reader.GetInt32(1);
                cmdrCounts.Add(new()
                {
                    Cmdr = (Commander)cmdr,
                    Count = count
                });
            }
            return cmdrCounts;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting cmdr count: {error}", ex.Message);
        }
        return new();

    }

    private async Task<List<PlayerRatingDetailDto>> GetPlayerIdDsstatsRatings(PlayerId playerId, CancellationToken token)
    {
        return await context.PlayerRatings
                .Where(x => x.Player!.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId)
                .ProjectTo<PlayerRatingDetailDto>(mapper.ConfigurationProvider)
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
                                GameMode = (GameMode)g.Key.GameMode,
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
            RatingCalcType.Combo => await GetPlayerIdArcadePlayerRatingDetails(playerId, ratingType, token),
            _ => throw new NotImplementedException()
        };
    }

    private async Task<PlayerRatingDetails> GetPlayerIdDsstatsPlayerRatingDetails(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
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

        var group = from p in context.Players
                    from rp in p.ReplayPlayers
                    where p.ToonId == playerId.ToonId
                        && p.RegionId == playerId.RegionId
                        && p.RealmId == playerId.RealmId
                        && rp.Replay.GameTime > startTime && rp.Replay.GameTime < endTime
                        && rp.Replay.ReplayRating!.RatingType == ratingType
                    group rp by rp.Race into g
                    select new PlayerCmdrAvgGain
                    {
                        Commander = g.Key,
                        AvgGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.RatingChange), 2),
                        Count = g.Count(),
                        Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
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
                                   where p.ToonId == playerId.ToonId
                                       && p.RealmId == playerId.RealmId
                                       && p.RegionId == playerId.RegionId
                                       && rp.Replay.ReplayRating != null
                                       && rp.Replay.ReplayRating.RatingType == ratingType
                                       && t != rp
                                       && t.Team == rp.Team
                                   select t.ReplayPlayerRatingInfo
                                : from p in context.Players
                                  from rp in p.ReplayPlayers
                                  from t in rp.Replay.ReplayPlayers
                                  where p.ToonId == playerId.ToonId
                                       && p.RealmId == playerId.RealmId
                                       && p.RegionId == playerId.RegionId
                                      && rp.Replay.ReplayRating != null
                                      && rp.Replay.ReplayRating.RatingType == ratingType
                                      && t.Team != rp.Team
                                  select t.ReplayPlayerRatingInfo;

        var avgRating = await teamRatings
            .Select(s => s.Rating)
            .DefaultIfEmpty()
            .AverageAsync(token);
        return Math.Round(avgRating, 2);
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
                                    && rp.Replay.ReplayRating != null
                                    && rp.Replay.ReplayRating.RatingType == ratingType
                                where t != rp && t.Team == rp.Team
                                group t by new { t.Player.ToonId, t.Player.RealmId, t.Player.RegionId, t.Player.Name } into g
                                where g.Count() > 10
                                select new PlayerTeamResultHelper()
                                {
                                    PlayerId = new(g.Key.ToonId, g.Key.RealmId, g.Key.RegionId),
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
                                  && rp.Replay.ReplayRating != null
                                  && rp.Replay.ReplayRating.RatingType == ratingType
                              where t.Team != rp.Team
                              group t by new { t.Player.ToonId, t.Player.RealmId, t.Player.RegionId, t.Player.Name } into g
                              where g.Count() > 10
                              select new PlayerTeamResultHelper()
                              {
                                  PlayerId = new(g.Key.ToonId, g.Key.RealmId, g.Key.RegionId),
                                  Name = g.Key.Name,
                                  Count = g.Count(),
                                  Wins = g.Count(c => c.PlayerResult == PlayerResult.Win),
                              };

        var results = await teammateGroup
            .ToListAsync(token);


        return results.Select(s => new PlayerTeamResult()
        {
            Name = s.Name,
            PlayerId = s.PlayerId,
            Count = s.Count,
            Wins = s.Wins,
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

        var replays = context.Replays
            .Where(x => x.GameTime > startDate
                && x.ReplayRating != null
                && x.ReplayRating.LeaverType == LeaverType.None
                && x.ReplayRating.RatingType == request.RatingType);

        if (endDate != DateTime.MinValue && (DateTime.Today - endDate).TotalDays > 2)
        {
            replays = replays.Where(x => x.GameTime < endDate);
        }

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
                    AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.Rating), 2),
                    AvgRatingGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.RatingChange), 2),
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
                    AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.Rating), 2),
                    AvgRatingGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.RatingChange), 2),
                    Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
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
