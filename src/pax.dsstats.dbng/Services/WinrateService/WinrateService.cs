
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using System.Globalization;

namespace pax.dsstats.dbng.Services;

public class WinrateService : IWinrateService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;
    private readonly IOptions<DbImportOptions> dbOptions;
    private readonly ILogger<WinrateService> logger;

    public WinrateService(ReplayContext context, IMemoryCache memoryCache, IOptions<DbImportOptions> dbOptions, ILogger<WinrateService> logger)
    {
        this.context = context;
        this.memoryCache = memoryCache;
        this.dbOptions = dbOptions;
        this.logger = logger;
    }

    public async Task<WinrateResponse> GetWinrate(WinrateRequest request, CancellationToken token)
    {
        var memKey = request.GenMemKey();

        if (!memoryCache.TryGetValue(memKey, out WinrateResponse response)) 
        { 
            response = await ProduceWinrate(request, token);
            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }

        return response;
    }

    private async Task<WinrateResponse> ProduceWinrate(WinrateRequest request, CancellationToken token)
    {
        var data = await GetDataFromRaw(request, token);

        if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            data = data.Where(x => x.Commander != Commander.None && (int)x.Commander <= 3).ToList();
        }
        else
        {
            data = data.Where(x => (int)x.Commander > 3).ToList();
        }

        return new()
        {
            Interest = request.Interest,
            WinrateEnts = data,
        };
    }

    private async Task<List<WinrateEnt>> GetData(WinrateRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        var group = request.Interest == Commander.None ?
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.GameTime > fromDate
                        && toDate < DateTime.Now.AddDays(-2) ? r.GameTime < toDate : true
                        && r.ReplayRatingInfo!.RatingType == request.RatingType
                        && rp.Duration >= 300
                    group rp by rp.Race into g
                    select new WinrateEnt
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        Wins = g.Count(s => s.PlayerResult == PlayerResult.Win),
                        AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.RatingChange), 2),
                    } :
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.GameTime > fromDate
                        && toDate < DateTime.Now.AddDays(-2) ? r.GameTime < toDate : true
                        && r.ReplayRatingInfo!.RatingType == request.RatingType
                        && rp.Duration >= 300
                        && rp.Race == request.Interest
                    group rp by rp.OppRace into g
                    select new WinrateEnt
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        Wins = g.Count(s => s.PlayerResult == PlayerResult.Win),
                        AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.RatingChange), 2),
                    };

        return await group.ToListAsync(token);
    }

    private async Task<List<WinrateEnt>> GetDataFromRaw(WinrateRequest request, CancellationToken token)
    {
        if (!Data.IsMaui && request.Exp2WinOffset > 0 && (request.FromRating > 0 || request.ToRating > 0))
        {
            return await GetDataFromVeryRawWithExp2Win(request, token);
        }

        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        var sql = request.Interest == Commander.None ?
            $@"
                SELECT 
                    rp.Race as commander,
	                count(*) as count,
                    round(avg(rpr.Rating), 2) as avgrating,
                    round(avg(rpr.RatingChange), 2) as avggain,
                    sum(CASE WHEN rp.PlayerResult = 1 THEN 1 ELSE 0 END) as wins
                FROM Replays as r
                INNER JOIN ReplayRatings as rr on rr.ReplayId = r.ReplayId
                INNER JOIN ReplayPlayers AS rp on rp.ReplayId = r.ReplayId
                INNER JOIN RepPlayerRatings AS rpr on rpr.ReplayPlayerId = rp.ReplayPlayerId
                WHERE rr.RatingType = {(int)request.RatingType}
                 AND r.GameTime > '{fromDate.ToString("yyyy-MM-dd")}'
                 {(toDate < DateTime.Today.AddDays(-2) ? $"AND r.GameTime < '{toDate.ToString("yyyy-MM-dd")}'" : "")}
                 AND rp.Duration > 300
                 {(request.FromRating > Data.MinBuildRating ? $"AND rpr.Rating >= {request.FromRating}" : "")}
                 {(request.ToRating != 0 && request.ToRating < Data.MaxBuildRating ? $"AND rpr.Rating <= {request.ToRating}" : "")}
                 {(request.Exp2WinOffset != 0 ? $"AND rr.ExpectationToWin >= {((50 - request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)} AND rr.ExpectationToWin <= {((50 + request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)}" : "")}
                GROUP BY rp.Race;
            "
            : 
            $@"
                SELECT 
                    rp.OppRace as commander,
	                count(*) as count,
                    round(avg(rpr.Rating), 2) as avgrating,
                    round(avg(rpr.RatingChange), 2) as avggain,
                    sum(CASE WHEN rp.PlayerResult = 1 THEN 1 ELSE 0 END) as wins
                FROM Replays as r
                INNER JOIN ReplayRatings as rr on rr.ReplayId = r.ReplayId
                INNER JOIN ReplayPlayers AS rp on rp.ReplayId = r.ReplayId
                INNER JOIN RepPlayerRatings AS rpr on rpr.ReplayPlayerId = rp.ReplayPlayerId
                WHERE rr.RatingType = {(int)request.RatingType}
                 AND r.GameTime > '{fromDate.ToString("yyyy-MM-dd")}'
                 {(toDate < DateTime.Today.AddDays(-2) ? $"AND r.GameTime < '{toDate.ToString("yyyy-MM-dd")}'" : "")}
                 AND rp.Duration > 300
                 AND rp.Race = {(int)request.Interest}
                 {(request.FromRating > Data.MinBuildRating ? $"AND rpr.Rating >= {request.FromRating}" : "")}
                 {(request.ToRating != 0 && request.ToRating < Data.MaxBuildRating ? $"AND rpr.Rating <= {request.ToRating}" : "")}
                 {(request.Exp2WinOffset != 0 ? $"AND rr.ExpectationToWin >= {((50 - request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)} AND rr.ExpectationToWin <= {((50 + request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)}" : "")}
                GROUP BY rp.OppRace;
            ";

        var result = await context.WinrateEnts
            .FromSqlRaw(sql)
            .ToListAsync(token);

        return result;
    }

    private async Task<List<WinrateEnt>> GetDataFromRawWithExp2Win(WinrateRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        var fromRating = request.FromRating <= Data.MinBuildRating ? 0 : request.FromRating;
        var toRating = request.ToRating >= Data.MaxBuildRating ? 10000 : request.ToRating;
        var tempTableName = Guid.NewGuid().ToString();

        await context.Database.ExecuteSqlRawAsync($@"
            CREATE TEMPORARY TABLE `{tempTableName}` AS
            SELECT rp.ReplayId, MIN(rpr.Rating) AS minRating, MAX(rpr.Rating) AS maxRating
            FROM ReplayPlayers AS rp
            INNER JOIN RepPlayerRatings AS rpr ON rp.ReplayPlayerId = rpr.ReplayPlayerId
            INNER JOIN Replays AS r ON r.ReplayId = rp.ReplayId
            WHERE r.GameTime > '{fromDate.ToString("yyyy-MM-dd")}'
            {(toDate < DateTime.Today.AddDays(-2) ? $"AND r.GameTime < '{toDate.ToString("yyyy-MM-dd")}'" : "")}
            GROUP BY rp.ReplayId;
        ");

        var sql = request.Interest == Commander.None ?
            $@"
                SELECT
                  rp.Race AS commander,
                  COUNT(*) AS count,
                  ROUND(AVG(rpr.Rating), 2) AS avgrating,
                  ROUND(AVG(rpr.RatingChange), 2) AS avggain,
                  SUM(CASE WHEN rp.PlayerResult = 1 THEN 1 ELSE 0 END) AS wins
                FROM Replays AS r
                INNER JOIN ReplayRatings AS rr ON rr.ReplayId = r.ReplayId
                INNER JOIN ReplayPlayers AS rp ON rp.ReplayId = r.ReplayId
                INNER JOIN RepPlayerRatings AS rpr ON rpr.ReplayPlayerId = rp.ReplayPlayerId
                INNER JOIN `{tempTableName}` AS tpr ON r.ReplayId = tpr.ReplayId
                WHERE rr.RatingType = {(int)request.RatingType}
                  AND rp.Duration > 300
                  AND rr.ExpectationToWin >= {((50 - request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)} AND rr.ExpectationToWin <= {((50 + request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)}
                  AND tpr.minRating >= {fromRating} AND tpr.maxRating <= {toRating}
                GROUP BY rp.Race;
            "
            :
            $@"
                SELECT
                  rp.OppRace AS commander,
                  COUNT(*) AS count,
                  ROUND(AVG(rpr.Rating), 2) AS avgrating,
                  ROUND(AVG(rpr.RatingChange), 2) AS avggain,
                  SUM(CASE WHEN rp.PlayerResult = 1 THEN 1 ELSE 0 END) AS wins
                FROM Replays AS r
                INNER JOIN ReplayRatings AS rr ON rr.ReplayId = r.ReplayId
                INNER JOIN ReplayPlayers AS rp ON rp.ReplayId = r.ReplayId
                INNER JOIN RepPlayerRatings AS rpr ON rpr.ReplayPlayerId = rp.ReplayPlayerId
                INNER JOIN `{tempTableName}` AS tpr ON r.ReplayId = tpr.ReplayId
                WHERE rr.RatingType = {(int)request.RatingType}
                  AND rp.Duration > 300
                  AND rr.ExpectationToWin >= {((50 - request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)} AND rr.ExpectationToWin <= {((50 + request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)}
                  AND tpr.minRating >= {fromRating} AND tpr.maxRating <= {toRating}
                GROUP BY rp.OppRace;
            ";

        var result = await context.WinrateEnts
            .FromSqlRaw(sql)
            .ToListAsync(token);

        await context.Database.ExecuteSqlRawAsync($"DROP TEMPORARY TABLE `{tempTableName}`;");

        return result;
    }

    private async Task<List<WinrateEnt>> GetDataFromVeryRawWithExp2Win(WinrateRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        var fromRating = request.FromRating <= Data.MinBuildRating ? 0 : request.FromRating;
        var toRating = request.ToRating >= Data.MaxBuildRating ? 10000 : request.ToRating;
        var tempTableName = Guid.NewGuid().ToString();

        using var connection = new MySqlConnection(dbOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var createSql = $@"
            CREATE TEMPORARY TABLE `{tempTableName}` AS
            SELECT rp.ReplayId, MIN(rpr.Rating) AS minRating, MAX(rpr.Rating) AS maxRating
            FROM ReplayPlayers AS rp
            INNER JOIN RepPlayerRatings AS rpr ON rp.ReplayPlayerId = rpr.ReplayPlayerId
            INNER JOIN Replays AS r ON r.ReplayId = rp.ReplayId
            WHERE r.GameTime > @FromDate
            {(toDate < DateTime.Today.AddDays(-2) ? "AND r.GameTime < @ToDate" : "")}
            GROUP BY rp.ReplayId;
        ";

        using var createCommand = new MySqlCommand(createSql, connection);

        createCommand.Parameters.AddWithValue("@FromDate", fromDate.ToString("yyyy-MM-dd"));
        if (toDate < DateTime.Today.AddDays(-2))
        {
            createCommand.Parameters.AddWithValue("@ToDate", toDate.ToString("yyyy-MM-dd"));
        }

        await createCommand.ExecuteNonQueryAsync();

        logger.LogInformation("Creating temp table: {sql}", createSql);

        var sql = request.Interest == Commander.None ?
            $@"
                SELECT
                  rp.Race AS commander,
                  COUNT(*) AS count,
                  ROUND(AVG(rpr.Rating), 2) AS avgrating,
                  ROUND(AVG(rpr.RatingChange), 2) AS avggain,
                  SUM(CASE WHEN rp.PlayerResult = 1 THEN 1 ELSE 0 END) AS wins
                FROM Replays AS r
                INNER JOIN ReplayRatings AS rr ON rr.ReplayId = r.ReplayId
                INNER JOIN ReplayPlayers AS rp ON rp.ReplayId = r.ReplayId
                INNER JOIN RepPlayerRatings AS rpr ON rpr.ReplayPlayerId = rp.ReplayPlayerId
                INNER JOIN `{tempTableName}` AS tpr ON r.ReplayId = tpr.ReplayId
                WHERE rr.RatingType = @RatingType
                  AND rp.Duration > 300
                  AND rr.ExpectationToWin >= @ExpectationMin AND rr.ExpectationToWin <= @ExpectationMax
                  AND tpr.minRating >= @FromRating AND tpr.maxRating <= @ToRating
                GROUP BY rp.Race;
            "
            :
            $@"
                SELECT
                  rp.OppRace AS commander,
                  COUNT(*) AS count,
                  ROUND(AVG(rpr.Rating), 2) AS avgrating,
                  ROUND(AVG(rpr.RatingChange), 2) AS avggain,
                  SUM(CASE WHEN rp.PlayerResult = 1 THEN 1 ELSE 0 END) AS wins
                FROM Replays AS r
                INNER JOIN ReplayRatings AS rr ON rr.ReplayId = r.ReplayId
                INNER JOIN ReplayPlayers AS rp ON rp.ReplayId = r.ReplayId
                INNER JOIN RepPlayerRatings AS rpr ON rpr.ReplayPlayerId = rp.ReplayPlayerId
                INNER JOIN `{tempTableName}` AS tpr ON r.ReplayId = tpr.ReplayId
                WHERE rr.RatingType = @RatingType
                  AND rp.Duration > 300
                  AND rr.ExpectationToWin >= @ExpectationMin AND rr.ExpectationToWin <= @ExpectationMax
                  AND tpr.minRating >= @FromRating AND tpr.maxRating <= @ToRating
                GROUP BY rp.OppRace;
            ";

        using var mainQueryCommand = new MySqlCommand(sql, connection);
        mainQueryCommand.Parameters.AddWithValue("@RatingType", (int)request.RatingType);
        mainQueryCommand.Parameters.AddWithValue("@ExpectationMin", (50 - request.Exp2WinOffset) / 100.0);
        mainQueryCommand.Parameters.AddWithValue("@ExpectationMax", (50 + request.Exp2WinOffset) / 100.0);
        mainQueryCommand.Parameters.AddWithValue("@FromRating", fromRating);
        mainQueryCommand.Parameters.AddWithValue("@ToRating", toRating);

        List<WinrateEnt> results = new();

        using (var reader = await mainQueryCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                int cmdr = reader.GetInt32(0);
                int count = reader.GetInt32(1);
                double avgRating = reader.GetDouble(2);
                double avgGain = reader.GetDouble(3);
                int wins = reader.GetInt32(4);
                results.Add(new()
                {
                    Commander = (Commander)cmdr,
                    Count = count,
                    AvgRating = avgRating,
                    AvgGain = avgGain,
                    Wins = wins
                });
            }
        }

        logger.LogInformation("Got data with: {sql}", sql);

        using var cleanupCommand = new MySqlCommand($"DROP TEMPORARY TABLE `{tempTableName}`;", connection);
        await cleanupCommand.ExecuteNonQueryAsync();

        logger.LogInformation("drop table: {sql}", $"DROP TEMPORARY TABLE `{tempTableName}`;");

        return results;
    }
}

