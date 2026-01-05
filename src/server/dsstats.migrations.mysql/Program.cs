
using dsstats.db;
using dsstats.db.Old;
using dsstats.dbServices;
using dsstats.parser;
using dsstats.ratings;
using dsstats.shared;
using dsstats.shared.Arcade;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using s2protocol.NET;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace dsstats.migrations.mysql;

partial class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        string connectionString = "server=localhost;port=9801;database=dsstats10;user=pax;Password=dOdVIs8VHQbgweMu2kMR";
        string importConnectionString = "server=localhost;port=9801;database=dsstats10;user=root;Password=YBEblujvyyj5lREIRhCh";
        // string oldConnectionString = "server=127.0.0.1;port=9101;database=dsstatsorg;user=pax;password=dOdVIs8VHQbgweMu2kMR;SSL Mode=None";

        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var oldConnectionString = config.GetProperty("ProdConnectionString").GetString();

        var dsstatsConfig = json.GetProperty("dsstats");
        // var prodConnectionString = dsstatsConfig.GetProperty("ConnectionString").GetString();
        // connectionString = prodConnectionString ?? "";
        // Console.WriteLine(connectionString);

        var services = new ServiceCollection();
        var serverVersion = new MySqlServerVersion(new Version(8, 4, 6));
        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Information);
            options.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            options.AddConsole();
        });
        services.AddDbContext<DsstatsContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
            //options.EnableDetailedErrors();
            //options.EnableSensitiveDataLogging();
        });
        services.AddDbContext<DsstatsContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
            //options.EnableDetailedErrors();
            //options.EnableSensitiveDataLogging();
        });

        services.AddDbContext<StagingDsstatsContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
            //options.EnableDetailedErrors();
            //options.EnableSensitiveDataLogging();
        });

        services.AddDbContext<OldReplayContext>(options =>
        {
            options.UseMySql(oldConnectionString, new MySqlServerVersion(new Version(5, 7, 44)), o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddOptions<ImportOptions>()
            .Configure(x => x.ConnectionString = importConnectionString);

        services.AddScoped<IReplayRepository, ReplayRepository>();
        services.AddSingleton<IImportService, ImportService>();
        services.AddSingleton<IRatingService, RatingService>();
        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetRequiredService<DsstatsContext>();
        var replaysCount = context.Replays.Count();
        Console.WriteLine($"found {replaysCount} replays");
        var importService = serviceProvider.GetRequiredService<IImportService>();
        var ratingService = serviceProvider.GetRequiredService<IRatingService>();

        var latestImportedArcade = new DateTime(2025, 11, 23, 08, 0, 0);
        var latestImportedDss = new DateTime(2025, 11, 23, 08, 0, 0);

        // ImportTestReplays(importService);
        // ImportManyReplays(importService, 1000).Wait();
        // ratingService.CreateRatings().Wait();
        // ratingService.ContinueRatings().Wait();

        // ImportArcadeReplays(serviceProvider, latestImportedArcade).Wait();
        // ImportV2Replays(serviceProvider, latestImportedDss).Wait();
        //FindArcadeMatches(serviceProvider).Wait();
        // GetV2ReplayCalcDtos(serviceProvider).Wait();
        // FixTe(serviceProvider).Wait();
        // FixMiddleTeam(serviceProvider).Wait();

        // CleanupUnits(serviceProvider).Wait();
        // ImportUnits(serviceProvider).Wait();

        // CheckDups(serviceProvider).Wait();
        // CheckHash(serviceProvider).Wait();
        FixHashes(serviceProvider).Wait();
        // CheckHash2(serviceProvider).Wait();

        // CreateImportJobs(serviceProvider).Wait();

        Console.WriteLine("Replay saved.");
        Console.ReadLine();
    }

    private static void ImportTestReplays(IImportService importService)
    {
        var files = Directory.GetFiles("/data/ds/testreplays");
        List<ReplayDto> replays = [];
        foreach (var file in files)
        {
            var sc2replay = DsstatsParser.GetSc2Replay(file).GetAwaiter().GetResult();
            ArgumentNullException.ThrowIfNull(sc2replay);
            var replayDto = DsstatsParser.ParseReplay(sc2replay);
            replays.Add(replayDto);
        }
        importService.InsertReplays(replays).Wait();
    }

    private static async Task ImportManyReplays(IImportService importService, int count = 100)
    {
        var folder = @"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer";
        var files = Directory.GetFiles(folder, "Direct Strike*").Skip(2500).Take(count);
        Console.WriteLine($"found {files.Count()} replays");
        var decoder = new ReplayDecoder();
        var options = new ReplayDecoderOptions()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            GameEvents = false,
            MessageEvents = true,
            TrackerEvents = true,
            AttributeEvents = false,
        };
        ConcurrentBag<ReplayDto> replays = [];
        await foreach (var result in decoder.DecodeParallelWithErrorReport(files.ToList(), 8, options))
        {
            if (result.Sc2Replay is null)
            {
                Console.WriteLine($"error decoding {result.ReplayPath}: {result.Exception}");
                continue;
            }
            try
            {
                var replay = DsstatsParser.ParseReplay(result.Sc2Replay);
                replays.Add(replay);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"failed parsing file {result.ReplayPath}: {ex.Message}");
            }
        }
        Console.WriteLine($"parsed {replays.Count} replays");
        await importService.InsertReplays(replays.ToList());
    }

    private static async Task FindArcadeMatches(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
        await ratingService.FindSc2ArcadeMatches();
    }

    private static async Task ImportArcadeReplays(ServiceProvider serviceProvider, DateTime latestImported)
    {
        using var scope = serviceProvider.CreateScope();
        var oldContext = scope.ServiceProvider.GetRequiredService<OldReplayContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

        var arcadeReplaysCount = await oldContext.ArcadeReplays.CountAsync();
        Console.WriteLine($"found {arcadeReplaysCount} arcade replays");

        int skip = 0;
        int take = 10000;

        var arcadeReplays = await oldContext.ArcadeReplays
            .Where(x => x.Imported > latestImported)
            .OrderBy(o => o.ArcadeReplayId)
            .Skip(skip)
            .Take(take)
            .Select(s => new ArcadeReplayDto()
            {
                RegionId = s.RegionId,
                BnetBucketId = s.BnetBucketId,
                BnetRecordId = s.BnetRecordId,
                GameMode = s.GameMode,
                CreatedAt = s.CreatedAt,
                Duration = s.Duration,
                PlayerCount = s.PlayerCount,
                WinnerTeam = s.WinnerTeam,
                Players = s.ArcadeReplayDsPlayers.Select(s => new ArcadeReplayPlayerDto()
                {
                    SlotNumber = s.SlotNumber,
                    Team = s.Team,
                    Player = new()
                    {
                        Name = s.Name,
                        ToonId = new()
                        {
                            Region = s.Player!.RegionId,
                            Realm = s.Player!.RealmId,
                            Id = s.Player!.ToonId
                        }
                    }
                }).ToList()
            }).ToListAsync();

        while (arcadeReplays.Count > 0)
        {
            await importService.ImportArcadeReplays(arcadeReplays);
            Console.WriteLine($"{skip}/{arcadeReplaysCount}");
            skip += take;
            arcadeReplays = await oldContext.ArcadeReplays
                .Where(x => x.Imported > latestImported)
            .OrderBy(o => o.ArcadeReplayId)
            .Skip(skip)
            .Take(take)
            .Select(s => new ArcadeReplayDto()
            {
                RegionId = s.RegionId,
                BnetBucketId = s.BnetBucketId,
                BnetRecordId = s.BnetRecordId,
                GameMode = s.GameMode,
                CreatedAt = s.CreatedAt,
                Duration = s.Duration,
                PlayerCount = s.PlayerCount,
                WinnerTeam = s.WinnerTeam,
                Players = s.ArcadeReplayDsPlayers.Select(s => new ArcadeReplayPlayerDto()
                {
                    SlotNumber = s.SlotNumber,
                    Team = s.Team,
                    Player = new()
                    {
                        Name = s.Name,
                        ToonId = new()
                        {
                            Region = s.Player!.RegionId,
                            Realm = s.Player!.RealmId,
                            Id = s.Player!.ToonId
                        }
                    }
                }).ToList()
            }).ToListAsync();
        }
    }

    private static async Task ImportV2Replays(ServiceProvider serviceProvider, DateTime latestImported)
    {
        using var scope = serviceProvider.CreateScope();
        var oldContext = scope.ServiceProvider.GetRequiredService<OldReplayContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
        oldContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));

        int skip = 0;
        int take = 1000;

        while (true)
        {

            var replays = await oldContext.Replays
                .Where(x => x.Imported > latestImported)
                .OrderBy(o => o.ReplayId)
                .Select(s => new ReplayV2Dto()
                {
                    GameTime = s.GameTime,
                    Duration = s.Duration,
                    WinnerTeam = s.WinnerTeam,
                    GameMode = s.GameMode,
                    Bunker = s.Bunker,
                    Cannon = s.Cannon,
                    Maxkillsum = s.Maxkillsum,
                    Playercount = s.Playercount,
                    Middle = s.Middle,
                    TournamentEdition = s.TournamentEdition,
                    CompatHash = s.ReplayHash,
                    ReplayPlayers = s.ReplayPlayers.Select(t => new ReplayPlayerV2Dto()
                    {
                        Name = t.Name,
                        Clan = t.Clan,
                        GamePos = t.GamePos,
                        Team = t.Team,
                        PlayerResult = t.PlayerResult,
                        Duration = t.Duration,
                        Race = t.Race,
                        APM = t.APM,
                        Kills = t.Kills,
                        TierUpgrades = t.TierUpgrades,
                        Refineries = t.Refineries,
                        Player = new PlayerV2Dto()
                        {
                            Name = t.Player.Name,
                            RealmId = t.Player.RealmId,
                            RegionId = t.Player.RegionId,
                            ToonId = t.Player.ToonId,
                        },
                        Upgrades = t.Upgrades.Select(u => new PlayerUpgradeV2Dto()
                        {
                            Gameloop = u.Gameloop,
                            Upgrade = new UpgradeV2Dto()
                            {
                                Name = u.Upgrade!.Name
                            }
                        }).ToList(),
                        Spawns = t.Spawns.Select(v => new SpawnV2Dto()
                        {
                            Gameloop = v.Gameloop,
                            Breakpoint = v.Breakpoint,
                            Income = v.Income,
                            GasCount = v.GasCount,
                            ArmyValue = v.ArmyValue,
                            KilledValue = v.KilledValue,
                            UpgradeSpent = v.UpgradeSpent,
                            Units = v.Units.Select(w => new SpawnUnitV2Dto()
                            {
                                Count = w.Count,
                                Poss = w.Poss,
                                Unit = new UnitV2Dto()
                                {
                                    Name = w.Unit!.Name
                                }
                            }).ToList()
                        }).ToList()
                    }).ToList()
                })
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            if (replays.Count == 0)
            {
                break;
            }

            Console.WriteLine("Importing ...");
            var dtos = replays.Select(s => s.ToV3Dto()).ToList();
            await importService.InsertReplays(dtos);

            Console.WriteLine(skip);
            skip += take;
        }
    }

    private static async Task GetV2ReplayCalcDtos(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var oldContext = scope.ServiceProvider.GetRequiredService<OldReplayContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
        oldContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));

        int skip = 0;
        int take = 10000;

        var fromDate = DateTime.Today.AddDays(-90);

        List<ReplayCalcDto> allreplays = [];

        while (true)
        {
            var replays = await oldContext.Replays
                .Where(x => x.GameTime > fromDate
                    && x.Duration > 300
                    && x.Playercount == 6
                    && x.WinnerTeam > 0)
                .OrderBy(o => o.ReplayId)
                .Select(s => new
                {
                    ReplayId = s.ReplayId,
                    Gametime = s.GameTime,
                    GameMode = s.GameMode,
                    PlayerCount = s.Playercount,
                    WinnerTeam = s.WinnerTeam,
                    TE = s.TournamentEdition,
                    IsArcade = false,
                    Duration = s.Duration,
                    MaxKillSum = s.Maxkillsum,
                    Players = s.ReplayPlayers.Select(t => new
                    {
                        ReplayPlayerId = t.ReplayPlayerId,
                        Duration = t.Duration,
                        Race = t.Race,
                        Team = t.Team,
                        Killsum = t.Kills,
                        PlayerId = t.PlayerId,
                    }).ToList()
                })
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            if (replays.Count == 0)
            {
                break;
            }

            allreplays.AddRange(replays.Select(s => new ReplayCalcDto()
            {
                ReplayId = s.ReplayId,
                Gametime = s.Gametime,
                GameMode = s.GameMode,
                PlayerCount = s.PlayerCount,
                WinnerTeam = s.WinnerTeam,
                TE = s.TE,
                IsArcade = false,
                Players = s.Players.Select(t => new PlayerCalcDto
                {
                    ReplayPlayerId = t.ReplayPlayerId,
                    IsLeaver = t.Duration < s.Duration - 90,
                    IsMvp = t.Killsum == s.MaxKillSum,
                    Race = t.Race,
                    Team = t.Team,
                    PlayerId = t.PlayerId,
                }).ToList()
            }));
            Console.WriteLine(skip);
            skip += take;
        }

        var json = JsonSerializer.Serialize(allreplays);
        File.WriteAllText("/data/ds/calcdtos.json", json);
    }

    public static async Task FixTe(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var oldContext = scope.ServiceProvider.GetRequiredService<OldReplayContext>();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        int skip = 0;
        int take = 2_000;

        while (true)
        {

            var oldReplays = await oldContext.Replays
                .OrderBy(o => o.ReplayId)
                .Select(s => new
                {
                    ReplayHash = s.ReplayHash,
                    TournamentEdition = s.TournamentEdition,
                })
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            if (oldReplays.Count == 0)
            {
                break;
            }

            var hashes = oldReplays.Select(s => s.ReplayHash).ToList();

            var replays = await context.Replays
                .Where(x => hashes.Contains(x.CompatHash))
                .ToListAsync();

            var dict = oldReplays.ToDictionary(s => s.ReplayHash, s => s.TournamentEdition);
            foreach (var replay in replays)
            {
                if (dict.TryGetValue(replay.ReplayHash, out var te))
                {
                    if (replay.TE != te)
                    {
                        replay.TE = te;
                        replay.Title = te ? "Direct Strike TE" : "Direct Strike";
                    }
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine(skip + "/" + replays.Count);
            skip += take;
        }
    }

    private static async Task FixMiddleTeam(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var oldContext = scope.ServiceProvider.GetRequiredService<OldReplayContext>();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        int skip = 0;
        int take = 5_000;

        while (true)
        {
            var oldReplays = await oldContext.Replays
                .Where(x => x.Middle.Length > 0)
                .OrderBy(o => o.ReplayId)
                .Select(s => new
                {
                    ReplayHash = s.ReplayHash,
                    Middle = s.Middle,
                })
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            if (oldReplays.Count == 0)
            {
                break;
            }
            Dictionary<string, int> firstMiddleDict = oldReplays.ToDictionary(k => k.ReplayHash, v =>
                int.Parse(v.Middle.Split('|', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "0"));
            var hashes = firstMiddleDict.Keys.ToHashSet();

            var replays = context.Replays
                .Where(x => hashes.Contains(x.CompatHash));

            foreach (var replay in replays)
            {
                if (firstMiddleDict.TryGetValue(replay.CompatHash, out var firstMiddle)
                    && firstMiddle > 0)
                {
                    replay.MiddleChanges[0] = firstMiddle;
                }
            }
            await context.SaveChangesAsync();

            skip += take;
            Console.WriteLine(skip);
        }
    }

    public static async Task CleanupUnits(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var units = await context.DsUnits
            .Include(i => i.Weapons)
                .ThenInclude(i => i.BonusDamages)
            .Include(i => i.Abilities)
            .Include(i => i.Upgrades)
            .ToListAsync();
        context.DsUnits.RemoveRange(units);
        await context.SaveChangesAsync();
    }

    public static async Task ImportUnits(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var oldContext = scope.ServiceProvider.GetRequiredService<OldReplayContext>();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var oldUnits = await oldContext.DsUnits
            .AsNoTracking()
            .Include(i => i.Weapons)
                .ThenInclude(i => i.BonusDamages)
            .Include(i => i.Abilities)
            .Include(i => i.Upgrades)
            .ToListAsync();

        foreach (var unit in oldUnits)
        {
            unit.DsUnitId = 0;

            foreach (var ability in unit.Abilities)
            {
                ability.DsAbilityId = 0;
            }

            foreach (var weapon in unit.Weapons)
            {
                weapon.DsWeaponId = 0;
                foreach (var bd in weapon.BonusDamages)
                {
                    bd.BonusDamageId = 0;
                }
            }

            foreach (var upgrade in unit.Upgrades)
            {
                upgrade.DsUpgradeId = 0;
            }
        }

        context.DsUnits.AddRange(oldUnits);
        await context.SaveChangesAsync();
    }

    public static async Task CheckDups(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var oldContext = scope.ServiceProvider.GetRequiredService<OldReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // var replayMaui = await context.Replays.FirstAsync(f => f.ReplayHash == "345A65319750A33FB493A149AECE8337BF303917E7FD51040D3319D816A0A1C4");
        // var replayTrans = await context.Replays.FirstAsync(f => f.ReplayHash == "D82971D9E0D17CE59F8B2BB6FCE07045187E072D1E98B1BD6204387E3373CE26");

        var replayMaui = await replayRepository.GetReplayDetails("345A65319750A33FB493A149AECE8337BF303917E7FD51040D3319D816A0A1C4");
        var replayTrans = await replayRepository.GetReplayDetails("D82971D9E0D17CE59F8B2BB6FCE07045187E072D1E98B1BD6204387E3373CE26");
        var replayOld = await GetOldReplay(oldContext, "2776b79874251814920a68fad33358e5");

        var hashMaui = replayMaui?.Replay.ComputeHash();
        var hashTrans = replayTrans?.Replay.ComputeHash();
        var oldHash = replayOld?.ComputeHash();

        Console.WriteLine("Maui: " + hashMaui + '|' + ComputeHash(replayMaui!.Replay));
        Console.WriteLine("Tran: " + hashTrans + '|' + ComputeHash(replayTrans!.Replay));
        Console.WriteLine("Old : " + oldHash + '|' + ComputeHash(replayOld!));

    }

    public static string ComputeHash(ReplayDto replay)
    {
        var sb = new StringBuilder();
        sb.Append(replay.Title);
        sb.Append(replay.Gametime.ToString("o")); // ISO 8601 UTC

        // Add players (sorted by ToonId for determinism)
        foreach (var player in replay.Players.OrderBy(p => p.GamePos)
                                            .ThenBy(p => p.Player?.ToonId.Realm)
                                            .ThenBy(p => p.Player?.ToonId.Id))
        {
            sb.Append(player.Player?.ToonId.Region);
            sb.Append(player.Player?.ToonId.Realm);
            sb.Append(player.Player?.ToonId.Id);
        }
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hashBytes);
    }

    private static async Task<ReplayDto?> GetOldReplay(OldReplayContext oldContext, string replayHash)
    {
        var oldDto = await oldContext.Replays
            .Where(x => x.ReplayHash == replayHash)
            .Select(s => new ReplayV2Dto()
            {
                GameTime = s.GameTime,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                GameMode = s.GameMode,
                Bunker = s.Bunker,
                Cannon = s.Cannon,
                Maxkillsum = s.Maxkillsum,
                Playercount = s.Playercount,
                Middle = s.Middle,
                TournamentEdition = s.TournamentEdition,
                CompatHash = s.ReplayHash,
                ReplayPlayers = s.ReplayPlayers.Select(t => new ReplayPlayerV2Dto()
                {
                    Name = t.Name,
                    Clan = t.Clan,
                    GamePos = t.GamePos,
                    Team = t.Team,
                    PlayerResult = t.PlayerResult,
                    Duration = t.Duration,
                    Race = t.Race,
                    APM = t.APM,
                    Kills = t.Kills,
                    TierUpgrades = t.TierUpgrades,
                    Refineries = t.Refineries,
                    Player = new PlayerV2Dto()
                    {
                        Name = t.Player.Name,
                        RealmId = t.Player.RealmId,
                        RegionId = t.Player.RegionId,
                        ToonId = t.Player.ToonId,
                    },
                    Upgrades = t.Upgrades.Select(u => new PlayerUpgradeV2Dto()
                    {
                        Gameloop = u.Gameloop,
                        Upgrade = new UpgradeV2Dto()
                        {
                            Name = u.Upgrade!.Name
                        }
                    }).ToList(),
                    Spawns = t.Spawns.Select(v => new SpawnV2Dto()
                    {
                        Gameloop = v.Gameloop,
                        Breakpoint = v.Breakpoint,
                        Income = v.Income,
                        GasCount = v.GasCount,
                        ArmyValue = v.ArmyValue,
                        KilledValue = v.KilledValue,
                        UpgradeSpent = v.UpgradeSpent,
                        Units = v.Units.Select(w => new SpawnUnitV2Dto()
                        {
                            Count = w.Count,
                            Poss = w.Poss,
                            Unit = new UnitV2Dto()
                            {
                                Name = w.Unit!.Name
                            }
                        }).ToList()
                    }).ToList()
                }).ToList()
            }).FirstOrDefaultAsync();
        return oldDto?.ToV3Dto();
    }

    private static async Task CheckHash(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        // var dbHash = "296CFCE5497CA9A4CFAD7E4FFAE704F0F7C1EA0CF5E0F0F0E6BA5FE9D86C5048";
        var dbHash = "F2F3DFE6A9D0E2352A416014B91A91766F683646413E5FC79954DC24ABE39B99";
        var details = await replayRepository.GetReplayDetails(dbHash);
        ArgumentNullException.ThrowIfNull(details);
        var replayHash = details.Replay.ComputeHash();

        var minReplay = await GetMinimalReplayDto(dbHash, context);
        ArgumentNullException.ThrowIfNull(minReplay);
        var minHash = minReplay.ComputeHash();
        var equal = dbHash.Equals(minHash);
        Console.WriteLine(replayHash + " <=> " + minHash + " <=> " + dbHash + " " + equal);

    }

    private static async Task CheckHash2(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        // var dbHash = "296CFCE5497CA9A4CFAD7E4FFAE704F0F7C1EA0CF5E0F0F0E6BA5FE9D86C5048";
        List<string> dbHashes = ["29ED835863CEC4A672C44BD29286CE57EA17E8CAC0A5501335AA9609A0CF446C", "BF5CC5D93FB31C29EC301BB026DA2421ABC8D88D299716F5F509FE844BF7AB9B"];
        List<string> calcHashes = [];
        foreach (var dbHash in dbHashes)
        {
            var replay = await GetMinimalReplayDto(dbHash, context);
            if (replay is null)
            {
                continue;
            }
            var calcHash = replay.ComputeHash();
            calcHashes.Add(calcHash);
        }
        Console.WriteLine(string.Join(", ", calcHashes));
    }

    private static async Task FixHashes(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        var replayHashes = await context.Replays.OrderBy(o => o.Gametime)
            .Where(x => x.Imported >= new DateTime(2026, 1, 1))
            .Select(s => s.ReplayHash)
            .ToListAsync();

        int diff = 0;
        int deleted = 0;
        int progress = 0;
        foreach (var replayHash in replayHashes)
        {
            progress++;
            if (progress % 100 == 0)
            {
                Console.WriteLine($"{progress}/{replayHashes.Count} ({diff}/{deleted})");
            }
            var minimalReplay = await GetMinimalReplay(replayHash, context);
            if (minimalReplay is null)
            {
                continue;
            }
            var computedHash = GetMinimalReplayDto(minimalReplay).ComputeHash();
            if (!computedHash.Equals(replayHash))
            {
                diff++;
                var existing = await GetMinimalReplay(computedHash, context);
                if (existing is null)
                {
                    // change hash
                    await context.Replays
                        .Where(x => x.ReplayHash == replayHash)
                        .ExecuteUpdateAsync(e => e.SetProperty(p => p.ReplayHash, computedHash));
                }
                else
                {
                    if ((existing.Duration < minimalReplay.Duration)
                        || (existing.Version == "v2" && minimalReplay.Version != "v2"))
                    {
                        await SetUploader(minimalReplay, existing, context);
                        await DeleteReplay(computedHash, context);
                        await context.Replays
                        .Where(x => x.ReplayHash == replayHash)
                        .ExecuteUpdateAsync(e => e.SetProperty(p => p.ReplayHash, computedHash));
                    }
                    else
                    {
                        await SetUploader(existing, minimalReplay, context);
                        await DeleteReplay(replayHash, context);
                    }
                    deleted++;
                }
            }
        }
        Console.WriteLine($"Hashes fixed: {diff}, Deleted: {deleted}");
    }

    private static async Task SetUploader(Replay keepReplay, Replay deleteReplay, DsstatsContext context)
    {
        foreach (var player in keepReplay.Players.Where(x => !x.IsUploader))
        {
            var deletePlayers = deleteReplay.Players.Where(x => x.IsUploader).ToList();
            if (deletePlayers.Count == 0)
            {
                return;
            }
            var deletePlayer = deletePlayers.FirstOrDefault(f =>
                   f.Player!.ToonId.Id == player.Player!.ToonId.Id
                && f.Player.ToonId.Region == player.Player.ToonId.Region
                && f.Player.ToonId.Realm == player.Player.ToonId.Realm);
            if (deletePlayer != null)
            {
                player.IsUploader = true;
                await context.ReplayPlayers
                    .Where(x => x.ReplayPlayerId == player.ReplayPlayerId)
                    .ExecuteUpdateAsync(e => e.SetProperty(p => p.IsUploader, true));
            }
        }
    }

    private static async Task<ReplayDto?> GetMinimalReplayDto(string replayHash, DsstatsContext context)
    {
        return await context.Replays
            .Where(x => x.ReplayHash == replayHash)
            .Select(s => new ReplayDto()
            {
                Title = s.Title,
                Version = s.Version,
                Gametime = s.Gametime,
                Duration = s.Duration,
                Players = s.Players.Select(t => new ReplayPlayerDto()
                {
                    GamePos = t.GamePos,
                    IsUploader = t.IsUploader,
                    Player = new PlayerDto()
                    {
                        ToonId = new()
                        {
                            Id = t.Player!.ToonId.Id,
                            Realm = t.Player!.ToonId.Realm,
                            Region = t.Player!.ToonId.Region
                        }
                    }
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    private static async Task<Replay?> GetMinimalReplay(string replayHash, DsstatsContext context)
    {
        return await context.Replays
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
            .AsNoTracking()
            .Where(x => x.ReplayHash == replayHash)
            .Select(s => new Replay()
            {
                ReplayId = s.ReplayId,
                Title = s.Title,
                Version = s.Version,
                Gametime = s.Gametime,
                Duration = s.Duration,
                Players = s.Players.Select(t => new ReplayPlayer()
                {
                    ReplayPlayerId = t.ReplayPlayerId,
                    GamePos = t.GamePos,
                    IsUploader = t.IsUploader,
                    Player = new Player()
                    {
                        ToonId = new()
                        {
                            Id = t.Player!.ToonId.Id,
                            Realm = t.Player!.ToonId.Realm,
                            Region = t.Player!.ToonId.Region
                        }
                    }
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    private static ReplayDto GetMinimalReplayDto(Replay replay)
    {
        return new()
        {
            Title = replay.Title,
            Version = replay.Version,
            Gametime = replay.Gametime,
            Duration = replay.Duration,
            Players = replay.Players.Select(t => new ReplayPlayerDto()
            {
                GamePos = t.GamePos,
                IsUploader = t.IsUploader,
                Player = new PlayerDto()
                {
                    ToonId = new()
                    {
                        Id = t.Player!.ToonId.Id,
                        Realm = t.Player!.ToonId.Realm,
                        Region = t.Player!.ToonId.Region
                    }
                }
            }).ToList()
        };
    }

    private static async Task DeleteReplay(string replayHash, DsstatsContext context)
    {
        var replay = await context.Replays
            .Include(i => i.Ratings)
            .Include(i => i.Players)
                .ThenInclude(i => i.Ratings)
            .Include(i => i.Players)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.Players)
                .ThenInclude(i => i.Upgrades)
            .FirstOrDefaultAsync(f => f.ReplayHash == replayHash);
        if (replay is null)
        {
            return;
        }
        context.Replays.Remove(replay);
        await context.SaveChangesAsync();
    }

    public static async Task CreateImportJobs(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var blobDir = "/data/ds/replayblobs";
        var blobFiles = Directory.GetFiles(blobDir, "*.blob", SearchOption.AllDirectories);
        var gzFiles = Directory.GetFiles(blobDir, "*.json.gz", SearchOption.AllDirectories);
        var files = blobFiles.Concat(gzFiles).ToArray();
        Console.WriteLine($"found {files.Length} blob files");
        List<UploadJob> jobs = [];
        foreach (var file in files)
        {
            UploadJob job = new()
            {
                BlobFilePath = file,
                CreatedAt = DateTime.UtcNow,
                FinishedAt = null,
                PlayerIds = []
            };
            jobs.Add(job);
        }
        context.UploadJobs.AddRange(jobs);
        await context.SaveChangesAsync();
    }
}
