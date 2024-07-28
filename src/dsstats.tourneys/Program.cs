using System.Security.Cryptography;
using System.Text.Json;
using AutoMapper;
using dsstats.db8;
using dsstats.db8.AutoMapper;
using dsstats.db8services;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.tourneys;

class Program
{
    public static readonly string tourneysDir = "/data/ds/Tourneys";
    public static readonly string decodeDir = "/data/ds/decode/done";

    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var importConnectionString = config.GetProperty("ImportConnectionString").GetString() ?? "";
        var mySqlConnectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var decodeUrl = config.GetProperty("DecodeUrl").GetString() ?? "";

        services.AddOptions<DbImportOptions>()
            .Configure(x =>
                {
                    x.ImportConnectionString = importConnectionString;
                    x.IsSqlite = false;
                });

        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Information);
            options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            options.AddConsole();
        });

        services.AddDbContext<ReplayContext>(options =>
            options.UseMySql(mySqlConnectionString, ServerVersion.AutoDetect(mySqlConnectionString), p =>
            {
                p.CommandTimeout(600);
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
        );

        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddHttpClient("decode")
            .ConfigureHttpClient(options =>
            {
                options.BaseAddress = new Uri(decodeUrl);
            });

        services.AddScoped<IReplayRepository, ReplayRepository>();

        var serviceProvider = services.BuildServiceProvider();

        if (args.Length == 0)
        {
            Console.WriteLine("Need a trouney name as parameter");
            return;
        }
        var tourneyDir = Path.Combine(tourneysDir, args[0]);
        if (!Directory.Exists(tourneyDir))
        {
            Console.WriteLine($"tourneyDir {tourneyDir} not found.");
            return;
        }

        AddTourneyReplays(serviceProvider, tourneyDir).Wait();

        Console.WriteLine("job done.");
    }

    static async Task AddTourneyReplays(ServiceProvider serviceProvider, string tourneyDir)
    {
        var replays = Directory.GetFiles(tourneyDir, "*.SC2Replay", SearchOption.AllDirectories)
            .ToHashSet();
        var existingJsons = replays.Where(x => File.Exists(Path.ChangeExtension(x, "json"))).ToList();
        replays.ExceptWith(existingJsons);

        if (replays.Count == 0)
        {
            Console.Write("not new tourney replays found.");
            return;
        }
        Guid guid = Guid.NewGuid();
        var fileHashes = await SaveReplays(serviceProvider, replays, guid);
        await Task.Delay(30000);

        SetReplayHashes(guid, fileHashes);
        var stateChanges = await SaveTourneyInfo(serviceProvider, tourneyDir, fileHashes);
        Console.WriteLine($"replay events generated: {stateChanges}");
    }

    static async Task<int> SaveTourneyInfo(ServiceProvider serviceProvider,
                                      string tourneyDir,
                                      Dictionary<string, ReplayFileInfo> replayHashes)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var tourney = await context.Events.FirstOrDefaultAsync(f => f.Name == Path.GetFileName(tourneyDir));

        if (tourney is null)
        {
            logger.LogWarning("Tourney not found {dir}", tourneyDir);
            return 0;
        }

        foreach (var replayInfo in replayHashes.Values)
        {
            if (string.IsNullOrEmpty(replayInfo.ReplayHash))
            {
                logger.LogWarning("no replay hash found: {replay}", replayInfo.TourneyPath);
                continue;
            }

            var replay = await context.Replays.FirstOrDefaultAsync(f => f.ReplayHash == replayInfo.ReplayHash);

            if (replay is null)
            {
                logger.LogWarning("replay not found: {hash}", replayInfo.ReplayHash);
                continue;
            }

            // 1v1
            string winnerTeam = replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam).FirstOrDefault()?.Name ?? "";
            string runnerTeam = replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam).FirstOrDefault()?.Name ?? "";
            var groupName = Path.GetFileName(Path.GetDirectoryName(replayInfo.DecodePath));
            ReplayEvent replayEvent = new()
            {
                WinnerTeam = winnerTeam,
                RunnerTeam = runnerTeam,
                Round = groupName ?? "tbd",
                Event = tourney
            };
            replay.ReplayEvent = replayEvent;
            var fakeJson = JsonSerializer.Serialize(mapper.Map<ReplayDto>(replay));
            File.WriteAllText(Path.ChangeExtension(replayInfo.TourneyPath, "json"), fakeJson);
        }
        return await context.SaveChangesAsync();
    }

    static async Task<Dictionary<string, ReplayFileInfo>> SaveReplays(ServiceProvider serviceProvider,
                                                                      ICollection<string> replays,
                                                                      Guid guid)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("decode");
        Dictionary<string, ReplayFileInfo> fileHashes = [];
        using var sha256 = SHA256.Create();
        try
        {
            var formData = new MultipartFormDataContent();

            foreach (var replay in replays)
            {
                var stream = new MemoryStream(File.ReadAllBytes(replay));
                var hashBytes = sha256.ComputeHash(stream);
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                fileHashes[hashString] = new ReplayFileInfo() { TourneyPath = replay };
                stream.Position = 0;
                var fileContent = new StreamContent(stream);
                formData.Add(fileContent, "files", Path.GetFileName(replay));
            }

            var result = await httpClient.PostAsync($"/api/v1/decode/upload/{guid}", formData);
            result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError("failed saving replays: {error}", ex.Message);
        }
        return fileHashes;
    }

    static void SetReplayHashes(Guid guid, Dictionary<string, ReplayFileInfo> fileHashes)
    {
        var replays = Directory.GetFiles(decodeDir, "*.SC2Replay", SearchOption.AllDirectories)
            .Where(x => Path.GetFileName(x).StartsWith(guid.ToString()))
            .ToList();
        using var sha256 = SHA256.Create();
        foreach (var replay in replays)
        {
            using var stream = new MemoryStream(File.ReadAllBytes(replay));
            var hashBytes = sha256.ComputeHash(stream);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            if (fileHashes.TryGetValue(hashString, out var replayFileInfo)
                && replayFileInfo is not null)
            {
                replayFileInfo.DecodePath = replay;
                replayFileInfo.ReplayHash = Path.GetFileNameWithoutExtension(replay)[37..];
            }
        }
    }
}

internal record ReplayFileInfo
{
    public string TourneyPath { get; set; } = string.Empty;
    public string DecodePath { get; set; } = string.Empty;
    public string ReplayHash { get; set; } = string.Empty;
}