
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace pax.dsstats.dbng.Services;

public partial class ImportService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<ImportService> logger;
    private const string defaultBlobBaseDir = "/data/ds/replayblobs";
    private const int continueMaxCount = 100;
    private readonly string baseDir = defaultBlobBaseDir;

    public ImportService(IServiceProvider serviceProvider, IMapper mapper, ILogger<ImportService> logger, string baseDir = defaultBlobBaseDir)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
        this.baseDir = baseDir;
    }

    public async Task<ImportReport> ImportReplayBlobs()
    {
        Stopwatch sw = Stopwatch.StartNew();
        var blobs = GetReplayBlobsFileNames(baseDir);

        if (!blobs.Any())
        {
            return new();
        }

        Dictionary<Guid, Uploader> fakeUploaderDic = new();
        var replays = await GetReplaysFromBlobs(blobs, fakeUploaderDic);

        sw.Stop();

        ImportReport importReport = new()
        {
            BlobFiles = blobs.Count,
            ReplaysFromBlobs = replays.Count,
            BlobPreparationDuration = Convert.ToInt32(sw.Elapsed.TotalSeconds)
        };

        try
        {
            importReport = await ImportReplays(replays, importReport);
        }
        catch (Exception ex)
        {
            foreach (var blob in blobs)
            {
                File.Move(blob, blob + ".error");
            }
            logger.LogError($"failed importing replays: {ex.Message}");
            importReport.Error = ex.Message;
            return importReport;
        }

        foreach (var blob in blobs)
        {
            File.Move(blob, blob + ".done");
        }

        importReport.Success = true;
        return importReport;
    }

    public async Task<ImportReport> ImportReplays(List<Replay> replays, ImportReport importReport)
    {
        Stopwatch sw = Stopwatch.StartNew();
        int newPlayers = await CreateAndMapPlayers(replays);
        int newUnits = await CreateAndMapUnits(replays);
        int newUpgrades = await CreateAndMapUpgrades(replays);
        sw.Stop();

        importReport.NewPlayers = newPlayers;
        importReport.NewUnits = newUnits;
        importReport.NewUpgrades = newUpgrades;
        importReport.MappingDuration = Convert.ToInt32(sw.Elapsed.TotalSeconds);

        sw.Restart();

        replays.ForEach(f => AdjustImportValues(f));

        int countBefore = replays.Count;
        replays = HandleLocalDuplicates(replays);
        sw.Stop();

        importReport.LocalDupsHandled = countBefore - replays.Count;
        importReport.LocalDupsDuration = Convert.ToInt32(sw.Elapsed.TotalSeconds);

        sw.Restart();

        countBefore = replays.Count;
        (var replayHashMap, var lastSpawnHashMap) = await CollectDbDuplicates(replays);
        (int delIds, replays) = await HandleDbDuplicates(replays, replayHashMap, lastSpawnHashMap);
        sw.Stop();

        importReport.DbDupsHandled = countBefore - replays.Count;
        importReport.DbDupsDeleted = delIds;
        importReport.DbDupsDuration = Convert.ToInt32(sw.Elapsed.TotalSeconds);

        if (replays.Count <= continueMaxCount)
        {
            importReport.LatestReplay = await GetLatestRepelayDate();
        }

        sw.Restart();
        (int savedReplays, var continueReplays) = await SaveReplays(replays);
        sw.Stop();

        importReport.SaveDuration = Convert.ToInt32(sw.Elapsed.TotalSeconds);
        importReport.SavedReplays = savedReplays;
        importReport.ContinueReplays = continueReplays.Select(s => mapper.Map<ReplayDsRDto>(s)).ToList();

        return importReport;
    }

    private async Task<DateTime> GetLatestRepelayDate()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Replays
            .OrderByDescending(o => o.GameTime)
            .Select(s => s.GameTime)
            .FirstOrDefaultAsync();
    }

    private async Task<List<Replay>> GetReplaysFromBlobs(List<string> blobs, Dictionary<Guid, Uploader> fakeUploaderDic)
    {
        List<Replay> replays = new();

        foreach (var blob in blobs)
        {
            var blobDir = new DirectoryInfo(Path.GetDirectoryName(blob) ?? "").Name;
            if (Guid.TryParse(blobDir, out Guid uploaderGuid))
            {
                int uploaderId = 0;
                if (fakeUploaderDic.ContainsKey(uploaderGuid))
                {
                    uploaderId = fakeUploaderDic[uploaderGuid].UploaderId;
                }
                else
                {
                    uploaderId = await GetUploaderId(uploaderGuid);
                    fakeUploaderDic[uploaderGuid] = new Uploader() { UploaderId = uploaderId };
                }
                var uploaderReplayDtos = await GetReplaysFromBlobFile(blob, uploaderGuid);
                var uploaderReplays = uploaderReplayDtos.Select(s => mapper.Map<Replay>(s)).ToList();
                uploaderReplays.ForEach(f => f.UploaderId = uploaderId);
                replays.AddRange(uploaderReplays);
            }
            if (replays.Count > 10000)
            {
                logger.LogWarning($"skipping blobs due to max replay count {replays.Count}");
                break;
            }
        }
        return replays;
    }

    private async Task<(int, List<Replay>)> SaveReplays(List<Replay> replays)
    {
        if (!replays.Any())
        {
            return (0, new());
        }

        Dictionary<int, Uploader> attachedUploaders = new();
        List<Replay> continueReplays = new();
        bool potentialContinue = replays.Count <= continueMaxCount;
        CheatResult cheatResult = new();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int i = 0;
        foreach (var replay in replays)
        {
            await AttachUploaders(context, replay, attachedUploaders);
            context.Replays.Add(replay);
            await CheatDetectService.AdjustReplay(context, replay, cheatResult);

            i++;
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
            if (potentialContinue)
            {
                continueReplays.Add(replay);
            }
        }
        await context.SaveChangesAsync();

        if (continueReplays.Any())
        {
            var playerIds = continueReplays
                .SelectMany(s => s.ReplayPlayers)
                .Select(s => s.PlayerId)
                .Distinct().ToList();

            await context.Players
                .Where(x => playerIds.Contains(x.PlayerId))
                .LoadAsync();

            var uploaderIds = continueReplays
                .SelectMany(s => s.ReplayPlayers)
                .Select(s => s.Player.UploaderId)
                .Distinct().ToList();

#pragma warning disable CS8629 // Nullable value type may be null.
            List<int> uploadersIdsNoNull = uploaderIds
                .Where(x => x != null)
                .Select(s => s.Value)
                .ToList();
#pragma warning restore CS8629 // Nullable value type may be null.

            await context.Uploaders
                .Where(x => uploadersIdsNoNull.Contains(x.UploaderId))
                .LoadAsync();
        }

        if (cheatResult.DcGames > 0 || cheatResult.RqGames > 0)
        {
            logger.LogWarning($"AdjustedReplays: {cheatResult}");
        }

        return (i, continueReplays);
    }

    private static async Task AttachUploaders(ReplayContext context, Replay replay, Dictionary<int, Uploader> attachedUploaders)
    {
        List<Uploader> uploaders = new();
        foreach (var uploader in replay.Uploaders)
        {
            if (attachedUploaders.ContainsKey(uploader.UploaderId))
            {
                uploaders.Add(attachedUploaders[uploader.UploaderId]);
            }
            else
            {
                var dbUploader = await context.Uploaders.FirstOrDefaultAsync(f => f.UploaderId == uploader.UploaderId);
                if (dbUploader != null)
                {
                    attachedUploaders[dbUploader.UploaderId] = dbUploader;
                    uploaders.Add(dbUploader);
                }
            }
        }
        replay.Uploaders = uploaders;
    }

    private async Task<int> GetUploaderId(Guid uploaderGuid)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Uploaders
            .Where(x => x.AppGuid == uploaderGuid)
            .Select(s => s.UploaderId)
            .FirstOrDefaultAsync();
    }

    private static async Task<List<ReplayDto>> GetReplaysFromBlobFile(string blob, Guid uploaderGuid)
    {
        return await JsonSerializer.DeserializeAsync<List<ReplayDto>>
            (await UnzipAsync(await File.ReadAllTextAsync(blob)), new JsonSerializerOptions() { })
             ?? new();
    }

    private static List<string> GetReplayBlobsFileNames(string baseDir)
    {
        DirectoryInfo info = new(baseDir);
        return info.GetFiles("*.base64", SearchOption.AllDirectories)
                .OrderBy(p => p.CreationTime)
                .Select(s => s.FullName)
                .ToList();
    }

    public static async Task<MemoryStream> UnzipAsync(string base64string)
    {
        var bytes = Convert.FromBase64String(base64string);
        using var msi = new MemoryStream(bytes);
        var mso = new MemoryStream();
        using (var gs = new GZipStream(msi, CompressionMode.Decompress))
        {
            await gs.CopyToAsync(mso);
        }
        mso.Position = 0;
        return mso;
    }

    public void DEBUGSeedUploaders()
    {
        var dirs = Directory.GetDirectories(baseDir).ToList();

        var files = Directory.GetFiles(baseDir, "*.error", SearchOption.AllDirectories).ToList();

        files.ForEach(f =>
        {
            string newFile = f.Remove(f.Length - 5);
            File.Move(f, newFile);
        });

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();


        List<Uploader> uploaders = new();
        foreach (var dir in dirs)
        {
            if (Guid.TryParse(new DirectoryInfo(dir).Name, out Guid guid))
            {
                if (context.Uploaders.FirstOrDefault(f => f.AppGuid == guid) == null)
                {
                    uploaders.Add(new()
                    {
                        AppGuid = guid
                    });
                }
            }
        }

        context.Uploaders.AddRange(uploaders);
        context.SaveChanges();
    }

    public void DEBUGResetBlobs()
    {
        var files = Directory.GetFiles(baseDir, "*.done", SearchOption.AllDirectories).ToList();

        files.ForEach(f =>
        {
            string newFile = f.Remove(f.Length - 4);
            File.Move(f, newFile);
        });

        var errorFiles = Directory.GetFiles(baseDir, "*.error", SearchOption.AllDirectories).ToList();

        errorFiles.ForEach(f =>
        {
            string newFile = f.Remove(f.Length - 5);
            File.Move(f, newFile);
        });
    }

    public void DEBUGFixComputerGames()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var computerReplays = context.Replays
            .Where(x => x.ReplayPlayers.Any(a => a.Player.ToonId == 0))
            .ToList();

        if (!computerReplays.Any())
        {
            return;
        }

        computerReplays.ForEach(f => f.GameMode = GameMode.Tutorial);
        context.SaveChanges();
    }

    public void DEBUGCreateUnitUpgradesJson()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var units = context.Units
            .AsNoTracking()
            .ToList();

        var upgrades = context.Upgrades
            .AsNoTracking()
            .ToList();

        File.WriteAllText("/data/ds/units.json", JsonSerializer.Serialize(units));       
        File.WriteAllText("/data/ds/upgrades.json", JsonSerializer.Serialize(upgrades));
    }

    public void DEBUGSeedUnitsUpgradesFromJson()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<Unit> units = JsonSerializer.Deserialize<List<Unit>>(File.ReadAllText("/data/ds/units.json")) ?? new();
        context.Units.AddRange(units);
        context.SaveChanges();

        List<Upgrade> upgrades = JsonSerializer.Deserialize<List<Upgrade>>(File.ReadAllText("/data/ds/upgrades.json")) ?? new();
        context.Upgrades.AddRange(upgrades);
        context.SaveChanges();
    }   
}