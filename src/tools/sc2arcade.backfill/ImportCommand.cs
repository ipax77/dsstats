using System.Text.Json;
using dsstats.db;
using dsstats.ratings;
using dsstats.shared;
using dsstats.shared.Arcade;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sc2arcade.crawler;

namespace sc2arcade.backfill;

public static class ImportCommand
{
    public static async Task<int> RunAsync(string directory, string configPath, bool execute, bool finalize,
        CancellationToken token)
    {
        using var archive = Archive.Open(directory, write: true);
        var state = BackfillState.Read(archive);
        if (state.Active is not null) throw new InvalidOperationException("Archive collection is incomplete.");
        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath, token));
        var section = config.RootElement.GetProperty("dsstats");
        string connection = section.GetProperty("ConnectionString").GetString()
            ?? throw new InvalidOperationException("Missing dsstats:ConnectionString.");
        var cs = new MySqlConnector.MySqlConnectionStringBuilder(connection);
        // Never print credentials or the complete connection string.
        Console.WriteLine($"Target: {cs.Server}:{cs.Port}/{cs.Database}; mode={(execute ? "EXECUTE" : "READ-ONLY PREVIEW")}");
        var services = new ServiceCollection();
        services.AddLogging();
        var version = new MySqlServerVersion(section.TryGetProperty("ServerVersion", out var v)
            ? Version.Parse(v.GetString()!) : new Version(8, 4, 0));
        services.AddDbContextFactory<DsstatsContext>(o => o.UseMySql(connection, version));
        services.AddDbContextFactory<StagingDsstatsContext>(o => o.UseMySql(connection, version));
        services.Configure<ImportOptions>(o => o.ConnectionString = connection);
        services.AddSingleton<IRatingService, RatingService>();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<DsstatsContext>>();
        await using (var context = await factory.CreateDbContextAsync(token))
        {
            await context.Database.OpenConnectionAsync(token);
            Console.WriteLine($"Connected. Archive eligible replays: {state.Regions.Sum(r => r.EligibleReplays):N0}.");
        }
        string receiptDirectory = Path.Combine(directory, "imports", Archive.Hash($"{cs.Server}:{cs.Port}/{cs.Database}"));
        string receiptPath = Path.Combine(receiptDirectory, "session.json");
        ImportReceipt receipt = File.Exists(receiptPath)
            ? JsonSerializer.Deserialize<ImportReceipt>(File.ReadAllText(receiptPath), Archive.Json)!
            : new(DateTime.UtcNow.AddSeconds(-1), archive.Configuration.StartedAt, false);
        if (receipt.ArchiveStartedAt != archive.Configuration.StartedAt)
            throw new InvalidDataException("Import receipt belongs to a different archive.");
        if (execute)
        {
            Directory.CreateDirectory(receiptDirectory);
            SaveReceipt(receiptPath, receipt);
        }
        var importer = new ArcadeArchiveImporter(factory);
        List<ArcadeReplayDto> batch = [];
        int existing = 0, inserted = 0, missing = 0, newPlayers = 0, batches = 0;
        foreach (var record in archive.Records.Where(r => r.Kind == "Response" && r.Outcome == "Page"))
        {
            token.ThrowIfCancellationRequested();
            foreach (var lobby in BackfillState.ParsePage(record.Body!, record.RegionId).Results)
            {
                if (lobby.CreatedAt < archive.Configuration.Cutoff.UtcDateTime) continue;
                if (ArcadeReplayConverter.Convert(lobby).Replay is { } replay) batch.Add(replay);
                if (batch.Count >= 500) await Flush();
            }
        }
        if (batch.Count > 0) await Flush();
        Console.WriteLine($"Import summary: existing={existing}, inserted={inserted}, missing={missing}, newPlayers={newPlayers}.");
        if (execute && finalize)
        {
            Console.WriteLine("Matching imported archive replays against existing dsstats replays...");
            await provider.GetRequiredService<IRatingService>().MatchWithNewArcadeReplays(receipt.ImportedAfter);
            await using var context = await factory.CreateDbContextAsync(token);
            context.Database.SetCommandTimeout(TimeSpan.FromMinutes(20));
            Console.WriteLine("Updating CombinedReplays using the production stored procedure...");
            await context.Database.ExecuteSqlRawAsync("CALL BatchImportCombinedReplays();", token);
            SaveReceipt(receiptPath, receipt with { Finalized = true });
            Console.WriteLine("Matching and CombinedReplays complete. The production full rating job must rebuild historical ratings.");
        }
        return 0;

        async Task Flush()
        {
            var result = await importer.ImportAsync(batch, execute, token);
            existing += result.Existing; inserted += result.Inserted; missing += result.Missing; newPlayers += result.NewPlayers;
            batch.Clear();
            if (++batches % 10 == 0) Console.WriteLine($"Batches={batches}, existing={existing}, inserted={inserted}, missing={missing}, newPlayers={newPlayers}");
        }
    }

    private static void SaveReceipt(string path, ImportReceipt receipt)
    {
        using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, receipt, Archive.Json);
            stream.Flush(true);
        }
        File.Move(path + ".tmp", path, true);
    }

    public record ImportReceipt(DateTime ImportedAfter, DateTimeOffset ArchiveStartedAt, bool Finalized);
}
