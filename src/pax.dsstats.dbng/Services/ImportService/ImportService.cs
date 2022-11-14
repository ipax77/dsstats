
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
    private const string blobBaseDir = "/data/ds/replayblobs";

    public ImportService(IServiceProvider serviceProvider, IMapper mapper, ILogger<ImportService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task<ImportReport> ImportReplayBlobs()
    {
        Stopwatch sw = Stopwatch.StartNew();
        var blobs = GetReplayBlobsFileNames();

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


        sw.Restart();
        int savedReplays = await SaveReplays(replays);
        sw.Stop();

        importReport.SaveDuration = Convert.ToInt32(sw.Elapsed.TotalSeconds);
        importReport.SavedReplays = savedReplays;

        return importReport;
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

    private async Task<int> SaveReplays(List<Replay> replays)
    {
        if (!replays.Any())
        {
            return 0;
        }

        Dictionary<int, Uploader> attachedUploaders = new();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int i = 0;
        foreach (var replay in replays)
        {
            await AttachUploaders(context, replay, attachedUploaders);
            context.Replays.Add(replay);

            i++;
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();
        return i;
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

    private static List<string> GetReplayBlobsFileNames()
    {
        DirectoryInfo info = new(blobBaseDir);
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
        var dirs = Directory.GetDirectories(blobBaseDir).ToList();

        var files = Directory.GetFiles(blobBaseDir, "*.error", SearchOption.AllDirectories).ToList();

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
        var files = Directory.GetFiles(blobBaseDir, "*.done", SearchOption.AllDirectories).ToList();

        files.ForEach(f =>
        {
            string newFile = f.Remove(f.Length - 4);
            File.Move(f, newFile);
        });

        var errorFiles = Directory.GetFiles(blobBaseDir, "*.error", SearchOption.AllDirectories).ToList();

        errorFiles.ForEach(f =>
        {
            string newFile = f.Remove(f.Length - 5);
            File.Move(f, newFile);
        });
    }
}