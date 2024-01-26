
using AutoMapper;
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace dsstats.db8services;

public partial class BuildService : IBuildService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;
    private readonly IMapper mapper;
    private readonly IDsDataService dsDataService;
    private readonly ILogger<BuildService> logger;
    private readonly string connectionString;
    private readonly bool IsSqlite;

    public BuildService(ReplayContext context,
                        IOptions<DbImportOptions> dbOptions,
                        IMemoryCache memoryCache,
                        IMapper mapper,
                        IDsDataService dsDataService,
                        ILogger<BuildService> logger)
    {
        connectionString = dbOptions.Value.ImportConnectionString;
        IsSqlite = dbOptions.Value.IsSqlite;
        this.context = context;
        this.memoryCache = memoryCache;
        this.mapper = mapper;
        this.dsDataService = dsDataService;
        this.logger = logger;
    }

    public async Task<BuildResponse> GetBuild(BuildRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey();

        if (!memoryCache.TryGetValue(memKey, out BuildResponse? buildResponse)
            || buildResponse is null)
        {
            try
            {
                buildResponse = request.PlayerNames.Count == 0 ?
                    await ProduceBuild(request, token)
                    : await ProducePlayerBuilds(request, token);

                await dsDataService.SetBuildResponseLifeAndCost(buildResponse, request.Interest);

                if (IsSqlite)
                {
                    memoryCache.Set(memKey, buildResponse, new MemoryCacheEntryOptions()
                        .SetPriority(CacheItemPriority.High)
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
                }
                else
                {
                    memoryCache.Set(memKey, buildResponse, new MemoryCacheEntryOptions()
                        .SetPriority(CacheItemPriority.High)
                        .SetAbsoluteExpiration(TimeSpan.FromDays(1)));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError("failed producing build: {error}", ex.Message);
            }
        }
        return buildResponse ?? new();
    }

    public async Task<List<RequestNames>> GetDefaultPlayers()
    {
        var players = new List<RequestNames>()
        {
            new("PAX", 226401, 2, 1),
            new("PAX", 10188255, 1, 1),
            new("Feralan", 1488340, 2, 1),
            new("Feralan", 8497675, 1, 1)
        };

        return await Task.FromResult(players);
    }

    public async Task<List<RequestNames>> GetTopPlayers(RatingType ratingType)
    {
        int minCount = ratingType == RatingType.StdTE || ratingType == RatingType.CmdrTE ? 50 : 1000;
        var minDate = DateTime.Today.AddDays(-90);

        var players = await context.ComboPlayerRatings
            .Where(x => x.RatingType == ratingType
                && x.Player.Uploader!.LatestReplay > minDate
                && x.Games >= minCount)
            .OrderByDescending(o => o.Rating)
            .Take(5)
            .Select(s => new RequestNames()
            {
                Name = s.Player.Name,
                ToonId = s.Player.ToonId,
                RealmId = s.Player.RealmId,
                RegionId = s.Player.RegionId
            }).ToListAsync();

        if (players.Count == 0)
        {
            return await GetDefaultPlayers();
        }
        else
        {
            return players;
        }
    }
}