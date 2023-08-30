using AutoMapper;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using pax.dsstats.shared;
using pax.dsstats.shared.Ratings;
using System.Diagnostics;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class RatingsService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly IOptions<DbImportOptions> dbImportOptions;
    private readonly ILogger<RatingsService> logger;

    public SemaphoreSlim ratingSs;

    public RatingsService(IServiceProvider serviceProvider,
                          IMapper mapper,
                          IOptions<DbImportOptions> dbImportOptions,
                          ILogger<RatingsService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.dbImportOptions = dbImportOptions;
        this.logger = logger;

        ratingSs = new(1, 1);
    }

    private const int ratingsCount = 10;
    private Queue<RatingsReport> ratingsResults = new Queue<RatingsReport>(ratingsCount);

    public async Task ProduceRatings(bool recalc = false)
    {
        await ratingSs.WaitAsync();

        Stopwatch sw = Stopwatch.StartNew();
        int recalcCount = 0;
        try
        {
            await CleanupPreRatings();

            var request = recalc == false ? await GetCalcRatingRequest() :
                new MmrService.CalcRatingRequest()
                {
                    MmrOptions = new(reCalc: true),
                    MmrIdRatings = await GetMmrIdRatings(new(reCalc: true), null),
                    // StartTime = Data.IsMaui ? new DateTime(2018, 1, 1) : new DateTime(2021, 2, 1),
                    StartTime = new DateTime(2018, 1, 1),
                    EndTime = DateTime.Today.AddDays(2)
                };

            if (request == null)
            {
                // nothing to do
                logger.LogWarning("nothing to do2");
                return;
            }

            recalc = request.MmrOptions.ReCalc;
            recalcCount = request.ReplayDsRDtos.Count;

            await GeneratePlayerRatings(request);

            if (Data.IsMaui)
            {
                await UpdateSqlitePlayers(request.MmrIdRatings, request.MmrOptions.ReCalc);
            }
            else
            {
                var playerArcadeNoUploads = await GetPlayerArcadeNoUploads();
                if (request.MmrOptions.ReCalc)
                {
                    RatingsCsvService.CreatePlayerRatingCsv(request.MmrIdRatings, playerArcadeNoUploads);
                    await WriteCsvFilesToDatabase();
                }
                else
                {
                    await UpdatePlayerRatings(request.MmrIdRatings, playerArcadeNoUploads);
                    await ContinueReplayPlayerRatingsFromCsv2MySql(RatingsCsvService.csvBasePath);
                    await ContinueReplayRatingsFromCsv2MySql(RatingsCsvService.csvBasePath);
                }
                await SetPlayerRatingsPos();
                await SetRatingChange();
            }
        }
        finally
        {
            sw.Stop();
            logger.LogWarning($"{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm:ss")}: ratings produced in {sw.ElapsedMilliseconds} ms {recalc}/{recalcCount}");

            if (ratingsResults.Count >= ratingsCount)
            {
                ratingsResults.Dequeue();
            }

            ratingsResults.Enqueue(new()
            {
                Produced = DateTime.UtcNow,
                ElapsedMs = (int)sw.ElapsedMilliseconds,
                Recalc = recalc,
                RecalcCount = recalcCount
            });

            ratingSs.Release();
        }
    }

    private async Task CleanupPreRatings()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var preRatings = await context.ReplayRatings
            .Include(i => i.RepPlayerRatings)
            .Where(x => x.IsPreRating)
            .ToListAsync();

        if (preRatings.Count == 0)
        {
            return;
        }

        context.ReplayRatings.RemoveRange(preRatings);
        await context.SaveChangesAsync();
    }

    private async Task<MmrService.CalcRatingRequest?> GetCalcRatingRequest()
    {
        MmrOptions mmrOptions = new(reCalc: true);

        int replayRatingAppendId = 0;
        int replayPlayerRatingAppendId = 0;

        DateTime startTime = new DateTime(2018, 1, 1);
        // DateTime startTime = new DateTime(2021, 2, 1);
        DateTime endTime = DateTime.Today.AddDays(2);

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var latestRatingsProduced = await context.Replays
            .Where(x => x.ReplayRatingInfo != null)
            .OrderByDescending(o => o.GameTime)
            .Select(s => s.GameTime)
            .FirstOrDefaultAsync();


        if (latestRatingsProduced > DateTime.MinValue)
        {
            var importedReplaysQuery = context.Replays
                .Where(x => x.Imported != null
                    && x.Imported >= latestRatingsProduced
                    && x.ReplayRatingInfo == null)
                .Select(s => new { s.Imported, s.GameTime });

            var count = await importedReplaysQuery.CountAsync();

            if (count == 0)
            {
                return null;
            }

            var oldestReplay = importedReplaysQuery.OrderBy(o => o.GameTime).First();
            if (oldestReplay.GameTime > latestRatingsProduced)
            {
                mmrOptions.ReCalc = false;
                startTime = latestRatingsProduced;
                replayPlayerRatingAppendId = await context.RepPlayerRatings
                    .OrderByDescending(o => o.RepPlayerRatingId)
                    .Select(s => s.RepPlayerRatingId)
                    .FirstOrDefaultAsync();
                replayRatingAppendId = await context.ReplayRatings
                    .OrderByDescending(o => o.ReplayRatingId)
                    .Select(s => s.ReplayRatingId)
                    .FirstOrDefaultAsync();
            }
        }

        // mmrOptions.InjectDic = mmrOptions.ReCalc ? await GetArcadeInjectDic() : new();

        MmrService.CalcRatingRequest request = new()
        {
            CmdrMmrDic = new(),
            MmrIdRatings = await GetMmrIdRatings(mmrOptions, mmrOptions.ReCalc ? null : startTime),
            MmrOptions = mmrOptions,
            ReplayRatingAppendId = replayRatingAppendId,
            ReplayPlayerRatingAppendId = replayPlayerRatingAppendId,
            StartTime = startTime,
            EndTime = endTime,
        };

        return request;
    }

    private async Task WriteCsvFilesToDatabase()
    {
        await PlayerRatingsFromCsv2MySql(RatingsCsvService.csvBasePath);
        await ReplayRatingsFromCsv2MySql(RatingsCsvService.csvBasePath);
        await ReplayPlayerRatingsFromCsv2MySql(RatingsCsvService.csvBasePath);
    }

    private async Task GeneratePlayerRatings(MmrService.CalcRatingRequest request)
    {
        int skip = 0;
        int take = 100000;

        request.ReplayDsRDtos = await GetReplayData(request.StartTime, skip, take, request.MmrOptions.ReCalc);

        while (request.ReplayDsRDtos.Any())
        {
            var calcResult = MmrService.GeneratePlayerRatings(request, Data.IsMaui);

            if (Data.IsMaui)
            {
                (request.ReplayRatingAppendId, request.ReplayPlayerRatingAppendId) =
                    await UpdateSqliteMmrChanges(calcResult.replayRatingDtos, request.ReplayRatingAppendId, request.ReplayPlayerRatingAppendId);
            }
            else
            {
                request.ReplayRatingAppendId = calcResult.ReplayRatingAppendId;
                request.ReplayPlayerRatingAppendId = calcResult.ReplayPlayerRatingAppendId;
            }

            if (request.ReplayDsRDtos.Count < take)
            {
                break;
            }

            skip += take;
            request.ReplayDsRDtos = await GetReplayData(request.StartTime, skip, take, request.MmrOptions.ReCalc);
        }
    }

    private async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetMmrIdRatings(MmrOptions mmrOptions, DateTime? startTime)
    {
        if (mmrOptions.ReCalc || startTime == null)
        {
            Dictionary<RatingType, Dictionary<int, CalcRating>> calcRatings = new();

            foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
            {
                if (ratingType == RatingType.None)
                {
                    continue;
                }
                calcRatings[ratingType] = new();
            }
            return calcRatings;
        }
        else
        {
            return await GetCalcRatings(startTime.Value);
        }
    }

    private async Task SetPlayerRatingsPos()
    {
        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        // command.CommandText = "CALL SetPlayerRatingPos();";
        command.CommandText = "CALL SetPlayerRatingPosWithDefeats();";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }

    private async Task SetRatingChange()
    {
        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "CALL SetRatingChange();";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }
}
