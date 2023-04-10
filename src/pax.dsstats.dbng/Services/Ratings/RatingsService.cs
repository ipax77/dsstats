using AutoMapper;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using pax.dsstats.dbng;
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

    private SemaphoreSlim ratingSs;

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
            var request = recalc == false ? await GetCalcRatingRequest() :
                new MmrService.CalcRatingRequest()
                {
                    MmrOptions = new(reCalc: true),
                    MmrIdRatings = await GetMmrIdRatings(new(reCalc: true), null),
                    StartTime = Data.IsMaui ? new DateTime(2018, 1, 1) : new DateTime(2021, 2, 1),
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
                await UpdateSqlitePlayers(request.MmrIdRatings);
            }
            else
            {
                if (request.MmrOptions.ReCalc)
                {
                    RatingsCsvService.CreatePlayerRatingCsv(request.MmrIdRatings);
                    await WriteCsvFilesToDatabase();
                }
                else
                {
                    await UpdatePlayerRatings(request.MmrIdRatings);
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

    private async Task<MmrService.CalcRatingRequest?> GetCalcRatingRequest()
    {
        MmrOptions mmrOptions = new(reCalc: true);

        List<ReplayDsRDto> continueReplays = new();
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

            if (count <= 100)
            {
                var importedReplays = await importedReplaysQuery.ToListAsync();

                var oldestReplay = importedReplays.OrderBy(o => o.GameTime).First();
               
                if (oldestReplay.GameTime > latestRatingsProduced)
                {
                    mmrOptions.ReCalc = false;
                    startTime = latestRatingsProduced;
                    continueReplays = await GetReplayData(startTime, endTime);
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
        }

        // mmrOptions.InjectDic = mmrOptions.ReCalc ? await GetArcadeInjectDic() : new();

        MmrService.CalcRatingRequest request = new()
        {
            CmdrMmrDic = new(),
            MmrIdRatings = await GetMmrIdRatings(mmrOptions, continueReplays),
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
        // continue
        if (request.ReplayDsRDtos.Any())
        {
            MmrService.GeneratePlayerRatings(request);
        }
        // recalc
        else
        {
            var _startTime = request.StartTime;
            var _endTime = request.EndTime;

            while (_startTime < _endTime)
            {
                var chunkEndTime = _startTime.AddYears(1);

                if (chunkEndTime > _endTime)
                {
                    chunkEndTime = _endTime;
                }

                request.ReplayDsRDtos = await GetReplayData(_startTime, chunkEndTime);

                _startTime = _startTime.AddYears(1);

                if (!request.ReplayDsRDtos.Any())
                {
                    continue;
                }

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
            }
        }
    }

    private async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetMmrIdRatings(MmrOptions mmrOptions, List<ReplayDsRDto>? dependentReplays)
    {
        if (mmrOptions.ReCalc || dependentReplays == null)
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
            return await GetCalcRatings(dependentReplays);
        }
    }
    
    private async Task SetPlayerRatingsPos()
    {
        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "CALL SetPlayerRatingPos();";
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
