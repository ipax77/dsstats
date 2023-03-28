using AutoMapper;
using dsstats.mmr;
using Microsoft.Extensions.Options;
using pax.dsstats.shared;
using pax.dsstats.shared.Ratings;

namespace dsstats.ratings.api.Services;

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

    public async Task ProduceRatings()
    {
        await ratingSs.WaitAsync();

        try
        {
            MmrOptions mmrOptions = new(reCalc: true);
            List<ReplayDsRDto> continueReplays = new();
            int replayRatingAppendId = 0;
            int replayPlayerRatingAppendId = 0;
            DateTime latestReplay = DateTime.MinValue;

            DateTime _startTime = new DateTime(2018, 1, 1);
            DateTime _endTime = DateTime.Today.AddDays(2);

            MmrService.CalcRatingRequest request = new()
            {
                CmdrMmrDic = new(),
                MmrIdRatings = await GetMmrIdRatings(mmrOptions, continueReplays),
                MmrOptions = mmrOptions,
                ReplayRatingAppendId = replayRatingAppendId,
                ReplayPlayerRatingAppendId = replayPlayerRatingAppendId,
            };

            latestReplay = await GeneratePlayerRatings(request, _startTime, _endTime);

            RatingsCsvService.CreatePlayerRatingCsv(request.MmrIdRatings);

            await WriteCsvFilesToDatabase();
        }
        finally
        {
            ratingSs.Release();
        }
    }

    private async Task WriteCsvFilesToDatabase()
    {
        await PlayerRatingsFromCsv2MySql(RatingsCsvService.csvBasePath);
        await ReplayRatingsFromCsv2MySql(RatingsCsvService.csvBasePath);
        await ReplayPlayerRatingsFromCsv2MySql(RatingsCsvService.csvBasePath);
    }

    private async Task<DateTime> GeneratePlayerRatings(MmrService.CalcRatingRequest request,
                                                       DateTime _startTime,
                                                       DateTime _endTime)
    {
        DateTime latestReplay = DateTime.MinValue;
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

            latestReplay = request.ReplayDsRDtos.Last().GameTime;

            var calcResult = MmrService.GeneratePlayerRatings(request);

            request.ReplayRatingAppendId = calcResult.ReplayRatingAppendId;
            request.ReplayPlayerRatingAppendId = calcResult.ReplayPlayerRatingAppendId;
        }
        return latestReplay;
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
}
