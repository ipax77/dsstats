using dsstats.db;
using dsstats.dbServices;
using dsstats.parser;
using dsstats.ratings;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using s2protocol.NET;
using System.Diagnostics;
using System.IO.Compression;

if (args.Length > 0 && string.Equals(args[0], "import-te-sidecars", StringComparison.OrdinalIgnoreCase))
{
    await RunImportTeSidecars(args[1..]);
    return;
}

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

static async Task RunImportTeSidecars(string[] args)
{
    var options = ImportTeOptions.Parse(args);
    if (string.IsNullOrWhiteSpace(options.Path) || !Directory.Exists(options.Path))
    {
        Console.WriteLine("Replay folder not found. Pass --path \"<Multiplayer folder>\".");
        return;
    }

    var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(cfg =>
        {
            cfg.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: false);
        })
        .ConfigureServices((ctx, services) =>
        {
            var connectionString = ctx.Configuration["DevConnectionString"]
                ?? throw new InvalidOperationException("DevConnectionString is not configured in appsettings.json.");

            var serverVersion = new MySqlServerVersion(new Version(8, 4, 7));
            services.AddDbContextFactory<DsstatsContext>(opt =>
                opt.UseMySql(connectionString, serverVersion, o =>
                {
                    o.MigrationsAssembly("dsstats.migrations.mysql");
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                }));
            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<DsstatsContext>>().CreateDbContext());
            services.AddSingleton<IRatingService, NoOpRatingService>();
            services.AddSingleton<IImportService, ImportService>();
        })
        .Build();

    await using (var context = await host.Services
        .GetRequiredService<IDbContextFactory<DsstatsContext>>()
        .CreateDbContextAsync())
    {
        await context.Database.MigrateAsync();
        if (options.ResetSidecars && !options.DryRun)
        {
            int deleted = await context.ReplaySpawnPlaybacks.ExecuteDeleteAsync();
            Console.WriteLine($"Deleted existing ReplaySpawnPlaybacks rows: {deleted}");
        }
    }

    var files = new DirectoryInfo(options.Path)
        .GetFiles("Direct Strike TE*.SC2Replay", SearchOption.TopDirectoryOnly)
        .OrderByDescending(file => file.LastWriteTime)
        .Take(options.Take)
        .ToList();

    Console.WriteLine($"Selected TE replay files: {files.Count}");
    Console.WriteLine($"Dry run: {options.DryRun}");
    Console.WriteLine($"Brotli level: {options.CompressionLevel}");
    Console.WriteLine();

    ReplayDecoder replayDecoder = new();
    ReplayDecoderOptions decoderOptions = new()
    {
        Initdata = true,
        Details = true,
        Metadata = true,
        GameEvents = false,
        MessageEvents = false,
        TrackerEvents = true,
        AttributeEvents = false,
    };

    List<ReplayImportDto> imports = [];
    List<ReplayImportMetric> metrics = [];
    List<string> errors = [];
    Stopwatch totalSw = Stopwatch.StartNew();
    TimeSpan decodeElapsed = TimeSpan.Zero;
    TimeSpan encodeElapsed = TimeSpan.Zero;

    foreach (var file in files)
    {
        try
        {
            Stopwatch decodeSw = Stopwatch.StartNew();
            var sc2Replay = await replayDecoder.DecodeAsync(file.FullName, decoderOptions);
            decodeSw.Stop();
            decodeElapsed += decodeSw.Elapsed;
            if (sc2Replay is null)
            {
                errors.Add($"{file.Name}: decode returned null");
                Console.WriteLine($"{file.Name}: skipped, decode returned null");
                continue;
            }

            var directStrikeReplay = DsstatsParser.ParseDirectStrikeReplay(sc2Replay);
            var replay = DsstatsParser.ParseReplay(sc2Replay);
            if (!replay.Title.EndsWith("TE", StringComparison.Ordinal))
            {
                errors.Add($"{file.Name}: decoded title is '{replay.Title}', not TE");
                Console.WriteLine($"{file.Name}: skipped, decoded title is '{replay.Title}'");
                continue;
            }

            Stopwatch encodeSw = Stopwatch.StartNew();
            var sidecar = SpawnPlaybackSidecarFactory.Create(sc2Replay, directStrikeReplay);
            var encoded = SpawnPlaybackSidecarCodec.EncodeWithMetadata(sidecar, options.CompressionLevel);
            encodeSw.Stop();
            encodeElapsed += encodeSw.Elapsed;

            replay.SpawnPlayback = new()
            {
                Available = true,
                FormatVersion = encoded.FormatVersion,
                CompressedLength = encoded.CompressedLength,
                UncompressedLength = encoded.UncompressedLength,
                UnitCount = encoded.UnitCount
            };

            imports.Add(new(replay, encoded));
            var codecStats = encoded.CodecStats ?? new(0, 0, 0, 0, 0);
            metrics.Add(new(
                file.Name,
                replay.ComputeHash(),
                file.Length,
                encoded.UnitCount,
                encoded.CompressedLength,
                encoded.UncompressedLength,
                codecStats.AbsolutePlayerCount,
                codecStats.RepeatPlayerCount,
                codecStats.RawSpawnPositionCount,
                codecStats.ChangedSpawnPositionCount,
                codecStats.ReusedSpawnPositionCount));

            Console.WriteLine(
                $"{file.Name}: units={encoded.UnitCount:N0}, compressed={ToKiB(encoded.CompressedLength):N1} KiB, ratio={GetRatio(encoded.CompressedLength, encoded.UncompressedLength):P1}, modes={codecStats.AbsolutePlayerCount:N0}/{codecStats.RepeatPlayerCount:N0}, reuse={codecStats.ReusedSpawnPositionCount:N0}");
        }
        catch (Exception ex)
        {
            errors.Add($"{file.Name}: {ex.Message}");
            Console.WriteLine($"{file.Name}: skipped, {ex.Message}");
        }
    }

    TimeSpan importElapsed = TimeSpan.Zero;
    if (!options.DryRun && imports.Count > 0)
    {
        Stopwatch importSw = Stopwatch.StartNew();
        var importService = host.Services.GetRequiredService<IImportService>();
        await importService.InsertReplayImports(imports);
        importSw.Stop();
        importElapsed = importSw.Elapsed;
    }

    totalSw.Stop();
    PrintSummary(metrics, errors, decodeElapsed, encodeElapsed, importElapsed, totalSw.Elapsed);

    if (!options.DryRun && metrics.Count > 0)
    {
        await PrintDbVerification(host.Services, metrics);
    }
}

static void PrintSummary(
    IReadOnlyList<ReplayImportMetric> metrics,
    IReadOnlyList<string> errors,
    TimeSpan decodeElapsed,
    TimeSpan encodeElapsed,
    TimeSpan importElapsed,
    TimeSpan totalElapsed)
{
    long totalFileBytes = metrics.Sum(x => x.FileBytes);
    int totalUnits = metrics.Sum(x => x.UnitCount);
    int totalCompressed = metrics.Sum(x => x.CompressedLength);
    int totalUncompressed = metrics.Sum(x => x.UncompressedLength);
    int totalAbsolutePlayers = metrics.Sum(x => x.AbsolutePlayerCount);
    int totalRepeatPlayers = metrics.Sum(x => x.RepeatPlayerCount);
    int totalRawSpawns = metrics.Sum(x => x.RawSpawnPositionCount);
    int totalChangedSpawns = metrics.Sum(x => x.ChangedSpawnPositionCount);
    int totalReusedSpawns = metrics.Sum(x => x.ReusedSpawnPositionCount);

    Console.WriteLine();
    Console.WriteLine("Summary");
    Console.WriteLine($"Decoded TE replays: {metrics.Count}");
    Console.WriteLine($"Skipped/errors: {errors.Count}");
    Console.WriteLine($"Replay file bytes: total={ToKiB(totalFileBytes):N1} KiB, avg={ToKiB(GetAverage(totalFileBytes, metrics.Count)):N1} KiB");
    Console.WriteLine($"Units: total={totalUnits:N0}, avg={GetAverage(totalUnits, metrics.Count):N1}");
    Console.WriteLine($"Compressed sidecar: total={ToKiB(totalCompressed):N1} KiB, avg={ToKiB(GetAverage(totalCompressed, metrics.Count)):N1} KiB");
    Console.WriteLine($"Uncompressed sidecar: total={ToKiB(totalUncompressed):N1} KiB, avg={ToKiB(GetAverage(totalUncompressed, metrics.Count)):N1} KiB");
    Console.WriteLine($"Compressed min/max: {ToKiB(metrics.Select(x => x.CompressedLength).DefaultIfEmpty().Min()):N1}/{ToKiB(metrics.Select(x => x.CompressedLength).DefaultIfEmpty().Max()):N1} KiB");
    Console.WriteLine($"Bytes per unit: compressed={GetAverage(totalCompressed, totalUnits):N2}, uncompressed={GetAverage(totalUncompressed, totalUnits):N2}");
    Console.WriteLine($"Compression ratio: {GetRatio(totalCompressed, totalUncompressed):P1}");
    Console.WriteLine($"Player modes: absolute={totalAbsolutePlayers:N0}, repeat={totalRepeatPlayers:N0}");
    Console.WriteLine($"Spawn positions: raw={totalRawSpawns:N0}, changed={totalChangedSpawns:N0}, reused={totalReusedSpawns:N0}");
    Console.WriteLine($"Timings: decode={decodeElapsed}, encode={encodeElapsed}, import={importElapsed}, total={totalElapsed}");

    if (errors.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Skipped/Error Files");
        foreach (var error in errors)
        {
            Console.WriteLine(error);
        }
    }
}

static async Task PrintDbVerification(IServiceProvider services, IReadOnlyList<ReplayImportMetric> metrics)
{
    var hashes = metrics.Select(x => x.ReplayHash).ToHashSet(StringComparer.Ordinal);
    await using var context = await services
        .GetRequiredService<IDbContextFactory<DsstatsContext>>()
        .CreateDbContextAsync();

    var rows = await context.ReplaySpawnPlaybacks
        .AsNoTracking()
        .Where(x => x.Replay != null && hashes.Contains(x.Replay.ReplayHash))
        .Select(x => new
        {
            x.Replay!.ReplayHash,
            x.CompressedLength,
            x.UncompressedLength,
            x.UnitCount,
            x.FormatVersion
        })
        .ToListAsync();

    Console.WriteLine();
    Console.WriteLine("DB Verification");
    Console.WriteLine($"Sidecars matched by replay hash: {rows.Count}/{metrics.Count}");
    Console.WriteLine($"DB format versions: {string.Join(", ", rows.GroupBy(x => x.FormatVersion).OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Count()}"))}");
    Console.WriteLine($"DB avg compressed sidecar: {ToKiB(GetAverage(rows.Sum(x => x.CompressedLength), rows.Count)):N1} KiB");
    Console.WriteLine($"DB avg uncompressed sidecar: {ToKiB(GetAverage(rows.Sum(x => x.UncompressedLength), rows.Count)):N1} KiB");
    Console.WriteLine($"DB avg units: {GetAverage(rows.Sum(x => x.UnitCount), rows.Count):N1}");
}

static double ToKiB(double bytes) => bytes / 1024.0;

static double GetAverage(double total, int count) => count == 0 ? 0 : total / count;

static double GetRatio(double compressed, double uncompressed) => uncompressed <= 0 ? 0 : compressed / uncompressed;

sealed record ReplayImportMetric(
    string FileName,
    string ReplayHash,
    long FileBytes,
    int UnitCount,
    int CompressedLength,
    int UncompressedLength,
    int AbsolutePlayerCount,
    int RepeatPlayerCount,
    int RawSpawnPositionCount,
    int ChangedSpawnPositionCount,
    int ReusedSpawnPositionCount);

sealed class ImportTeOptions
{
    public string Path { get; private init; } = string.Empty;
    public int Take { get; private init; } = 100;
    public bool DryRun { get; private init; }
    public bool ResetSidecars { get; private init; }
    public CompressionLevel CompressionLevel { get; private init; } = CompressionLevel.Optimal;

    public static ImportTeOptions Parse(string[] args)
    {
        string path = string.Empty;
        int take = 100;
        bool dryRun = false;
        bool resetSidecars = false;
        CompressionLevel compressionLevel = CompressionLevel.Optimal;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--path" when i + 1 < args.Length:
                    path = args[++i];
                    break;
                case "--take" when i + 1 < args.Length && int.TryParse(args[i + 1], out var value):
                    take = Math.Max(1, value);
                    i++;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--reset-sidecars":
                    resetSidecars = true;
                    break;
                case "--compression" when i + 1 < args.Length:
                    compressionLevel = string.Equals(args[++i], "fastest", StringComparison.OrdinalIgnoreCase)
                        ? CompressionLevel.Fastest
                        : CompressionLevel.Optimal;
                    break;
            }
        }

        return new()
        {
            Path = path,
            Take = take,
            DryRun = dryRun,
            ResetSidecars = resetSidecars,
            CompressionLevel = compressionLevel
        };
    }
}

sealed class NoOpRatingService : IRatingService
{
    public Task ContinueFindSc2ArcadeMatches(DateTime? lastCheckTime = null) => Task.CompletedTask;

    public Task ContinueRatings() => Task.CompletedTask;

    public Task CreateRatings() => Task.CompletedTask;

    public Task FindSc2ArcadeMatches() => Task.CompletedTask;

    public Task MatchNewDsstatsReplays(DateTime? dsstatsImportedAfter = null) => Task.CompletedTask;

    public Task MatchWithNewArcadeReplays(DateTime? arcadeImportedAfter = null) => Task.CompletedTask;

    public Task PreRatings(List<int> replayIds) => Task.CompletedTask;

    public Task PreRatings(List<ReplayCalcDto> replays) => Task.CompletedTask;
}
