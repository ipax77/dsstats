using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.db;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using Microsoft.Extensions.Options;
using System.Data.SQLite;

namespace dsstats.services;

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
        return await context.ArcadePlayers
            .Where(x => x.ProfileId == playerId.ToonId
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

    public async Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId,
                                                                           RatingType ratingType,
                                                                           CancellationToken token)
    {
        if (IsSqlite)
        {
            return await GetSqlitePlayerRatingChartData(playerId, ratingType, token);
        }

        string sql =
$@"SELECT YEAR(`r0`.`GameTime`) AS `Year`, WEEK(`r0`.`GameTime`) AS `Week`, ROUND(AVG(`r2`.`Rating`)) AS `Rating`, MAX(`r2`.`Games`) AS `Games`
      FROM `Players` AS `p`
      INNER JOIN `ReplayPlayers` AS `r` ON `p`.`PlayerId` = `r`.`PlayerId`
      INNER JOIN `Replays` AS `r0` ON `r`.`ReplayId` = `r0`.`ReplayId`
      INNER JOIN `ReplayRatings` AS `r1` ON `r0`.`ReplayId` = `r1`.`ReplayId`
      INNER JOIN `RepPlayerRatings` AS `r2` ON `r`.`ReplayPlayerId` = `r2`.`ReplayPlayerId`
      WHERE (((`p`.`ToonId` = {playerId.ToonId}) AND (`p`.`RegionId` = {playerId.RegionId})) AND (`p`.`RealmId` = {playerId.RealmId})) AND (`r1`.`RatingType` = {(int)ratingType})
      GROUP BY YEAR(`r0`.`GameTime`), WEEK(`r0`.`GameTime`)
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

    public async Task<List<ReplayPlayerChartDto>> GetSqlitePlayerRatingChartData(PlayerId playerId,
                                                                           RatingType ratingType,
                                                                       CancellationToken token)
    {
        string sql = $@"
        SELECT strftime('%Y', `r0`.`GameTime`) AS `Year`,
               strftime('%W', `r0`.`GameTime`) AS `Week`,
               ROUND(AVG(`r2`.`Rating`)) AS `Rating`,
               MAX(`r2`.`Games`) AS `Games`
        FROM `Players` AS `p`
        INNER JOIN `ReplayPlayers` AS `r` ON `p`.`PlayerId` = `r`.`PlayerId`
        INNER JOIN `Replays` AS `r0` ON `r`.`ReplayId` = `r0`.`ReplayId`
        INNER JOIN `ReplayRatings` AS `r1` ON `r0`.`ReplayId` = `r1`.`ReplayId`
        INNER JOIN `RepPlayerRatings` AS `r2` ON `r`.`ReplayPlayerId` = `r2`.`ReplayPlayerId`
        WHERE (((`p`.`ToonId` = {playerId.ToonId}) AND (`p`.`RegionId` = {playerId.RegionId})) AND (`p`.`RealmId` = {playerId.RealmId})) AND (`r1`.`RatingType` = {(int)ratingType})
        GROUP BY `Year`, `Week`
        ORDER BY `Year`, `Week`;
    ";

        try
        {
            var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync(token);

            var command = new SQLiteCommand(sql, connection);

            var reader = await command.ExecuteReaderAsync();

            List<ReplayPlayerChartDto> chartData = new();
            while (await reader.ReadAsync(token))
            {
                int year = int.Parse(reader.GetString(0));
                int week = int.Parse(reader.GetString(1));
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
}
