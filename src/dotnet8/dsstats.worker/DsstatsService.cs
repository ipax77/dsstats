
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;

using pax.dsstats.shared.Arcade;
using s2protocol.NET;

namespace dsstats.worker;

public partial class DsstatsService
{
    private readonly string appFolder;
    private readonly string connectionString;
    private readonly string configFile;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IMapper mapper;
    private readonly ILogger<DsstatsService> logger;

    private ReplayDecoderOptions decoderOptions;
    private ReplayDecoder? decoder;
    private readonly SemaphoreSlim ssDecode = new(1, 1);
    private readonly SemaphoreSlim ssSave = new(1, 1);
    private readonly SemaphoreSlim ssUpload = new(1, 1);

    public DsstatsService(IServiceScopeFactory scopeFactory,
                          IHttpClientFactory httpClientFactory,
                          IMapper mapper,
                          ILogger<DsstatsService> logger)
    {
        appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dsstats.worker");

        configFile = Path.Combine(appFolder, "workerconfig.json");
        connectionString = $"Data Source={Path.Combine(appFolder, "dsstats.db")}";
        decoderOptions = new()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            MessageEvents = false,
            TrackerEvents = true,
            GameEvents = false,
            AttributeEvents = false
        };

        this.scopeFactory = scopeFactory;
        this.httpClientFactory = httpClientFactory;
        this.mapper = mapper;
        this.logger = logger;

        EnsurePrerequisites();
    }

    private HashSet<Unit> Units = new();
    private HashSet<Upgrade> Upgrades = new();

    private HashSet<PlayerId> PlayerIds = new();

    public async Task StartJob(CancellationToken token = default)
    {
        EnsurePrerequisites();
        UpdateConfig();
        var newReplays = await GetNewReplays();
        logger.LogInformation("New replays: {newReplays}", newReplays.Count);

        if (newReplays.Count > 0)
        {
            try
            {
                int decoded = await Decode(newReplays, token);
                await Upload();
                logger.LogWarning("replays decoded: {decoded}", decoded);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);
            }
        }
    }

    private void EnsurePrerequisites()
    {
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.Migrate();
    }
}

