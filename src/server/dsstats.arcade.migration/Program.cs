using dsstats.db;
using dsstats.dbServices;
using dsstats.ratings;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg =>
    {
        // Load the same out-of-repo prod config file used by dsstats.api in production
        cfg.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);
    })
    .ConfigureServices((ctx, services) =>
    {
        // Prod connection string: from /data/localserverconfig.json ("dsstats:ConnectionString"),
        // falling back to appsettings.json ("ProdConnectionString") for manual override.
        var prodCs = ctx.Configuration["dsstats:ConnectionString"]
            ?? ctx.Configuration["ProdConnectionString"]
            ?? throw new InvalidOperationException(
                "Prod connection string not found. Set 'dsstats:ConnectionString' in /data/localserverconfig.json " +
                "or 'ProdConnectionString' in appsettings.json.");
        var prodImportCs = ctx.Configuration["dsstats:ImportConnectionString"]
            ?? throw new InvalidOperationException(
                "Prod connection string not found. Set 'dsstats:ImportConnectionString' in /data/localserverconfig.json ");

        var serverVersion = new MySqlServerVersion(new Version(8, 4, 7));

        services.AddDbContext<DsstatsContext>(opt =>
            opt.UseMySql(prodCs, serverVersion, o =>
            {
                o.MigrationsAssembly("dsstats.migrations.mysql");
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            }));

        services.AddSingleton<IImportService, ImportService>();
        services.AddSingleton<IRatingService, RatingService>();
        services.Configure<ImportOptions>(opt => opt.ConnectionString = prodImportCs);
    })
    .Build();

var config = host.Services.GetRequiredService<IConfiguration>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var importService = host.Services.GetRequiredService<IImportService>();
var ratingService = host.Services.GetRequiredService<IRatingService>();

var devCs = config["DevConnectionString"]
    ?? throw new InvalidOperationException("DevConnectionString is not configured in appsettings.json.");

var serverVersion = new MySqlServerVersion(new Version(8, 4, 7));
var devOptions = new DbContextOptionsBuilder<DsstatsContext>()
    .UseMySql(devCs, serverVersion, o =>
        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
    .Options;

var scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();

// Connection test: print replay counts from both databases before migrating
using (var devContext = new DsstatsContext(devOptions))
using (var scope = scopeFactory.CreateScope())
{
    var prodContext = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
    var devCount = await devContext.ArcadeReplays.CountAsync();
    var prodCount = await prodContext.ArcadeReplays.CountAsync();
    logger.LogInformation("Dev  ArcadeReplays: {devCount}", devCount);
    logger.LogInformation("Prod ArcadeReplays: {prodCount}", prodCount);
}

const int batchSize = 2000;
int page = 0;
int totalImported = 0;

var imported = new DateTime(2026, 4, 4);
logger.LogInformation("Starting arcade replay migration... after imported {imported}", imported.ToShortDateString());

while (true)
{
    List<ArcadeReplay> batch;

    using (var devContext = new DsstatsContext(devOptions))
    {
        batch = await devContext.ArcadeReplays
            .Where(x => x.Imported > imported)
            .Include(r => r.Players)
                .ThenInclude(p => p.Player)
            .OrderBy(r => r.ArcadeReplayId)
            .Skip(page * batchSize)
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync();
    }

    if (batch.Count == 0)
        break;

    var dtos = batch.Select(r => r.ToArcadeDto()).ToList();
    await importService.ImportArcadeReplays(dtos);

    totalImported += batch.Count;
    page++;

    logger.LogInformation("Progress: {total} replays migrated (batch {page}, {count} in batch)",
        totalImported, page, batch.Count);

    if (batch.Count < batchSize)
        break;
}

logger.LogInformation("Migration complete. Total replays processed: {total}", totalImported);

// Populate CombinedReplays table with the newly imported arcade replays
logger.LogInformation("Calling BatchImportCombinedReplays stored procedure...");
using (var scope = scopeFactory.CreateScope())
{
    var prodContext = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
    prodContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(20));
    await prodContext.Database.ExecuteSqlRawAsync("CALL BatchImportCombinedReplays();");
}
logger.LogInformation("BatchImportCombinedReplays complete.");

// Match the backfilled arcade replays against dsstats replays (full historical scan)
logger.LogInformation("Running FindSc2ArcadeMatches to match arcade backfill with dsstats replays...");
// await ratingService.FindSc2ArcadeMatches();
await ratingService.MatchWithNewArcadeReplays(imported);
logger.LogInformation("FindSc2ArcadeMatches complete.");
