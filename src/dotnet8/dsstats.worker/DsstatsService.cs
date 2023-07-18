
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;

using pax.dsstats.shared.Arcade;
using s2protocol.NET;

namespace dsstats.worker;

public partial class DsstatsService
{
    private readonly string connectionString;
    private readonly string configFile;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<DsstatsService> logger;

    private ReplayDecoderOptions decoderOptions;
    private ReplayDecoder? decoder;
    private readonly SemaphoreSlim ssDecode = new(1, 1);
    private readonly SemaphoreSlim ssSave = new(1, 1);

    public DsstatsService(IServiceScopeFactory scopeFactory, ILogger<DsstatsService> logger)
    {
        var appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dsstats.worker");

        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

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
        this.logger = logger;

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.Migrate();
    }

    private HashSet<Unit> Units = new();
    private HashSet<Upgrade> Upgrades = new();

    private HashSet<PlayerId> PlayerIds = new();

    public async Task StartJob(CancellationToken token = default)
    {
        UpdateConfig();
        var newReplays = await GetNewReplays();
        logger.LogInformation("New replays: {newReplays}", newReplays.Count);

        if (newReplays.Count > 0)
        {
            try
            {
                int decoded = await Decode(newReplays, token);
                logger.LogWarning("replays decoded: {decoded}", decoded);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);
            }
        }
    }
}

