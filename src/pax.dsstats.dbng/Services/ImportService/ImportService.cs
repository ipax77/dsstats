
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

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

    public async Task ImportReplayBlobs()
    {
        Stopwatch sw = Stopwatch.StartNew();
        var blobs = GetReplayBlobsFileNames();

        sw.Stop();
        logger.LogInformation($"got {blobs.Count} blobs {sw.ElapsedMilliseconds} ms");

        sw.Restart();

        List<Replay> replays = new();
        Dictionary<Guid, Uploader> fakeUploaderDic = new();
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
        sw.Stop();
        logger.LogInformation($"prepared blobs in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        int newPlayers = await CreateAndMapPlayers(replays);
        sw.Stop();
        logger.LogInformation($"mapped players in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        int newUnits = await CreateAndMapUnits(replays);
        sw.Stop();
        logger.LogInformation($"mapped units in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        int newUpgrades = await CreateAndMapUpgrades(replays);
        sw.Stop();
        logger.LogInformation($"mapped upgrades in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        replays.ForEach(f => SetReplayPlayerLastSpawnHashes(f));
        sw.Stop();
        logger.LogInformation($"player spawnHashes generated in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        int countBefore = replays.Count;
        replays = HandleLocalDuplicates(replays);
        sw.Stop();
        logger.LogInformation($"handled local duplicates ({countBefore} => {replays.Count}) in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        var newReplays = await SaveReplays(replays, fakeUploaderDic.Values.ToDictionary(k => k.UploaderId, v => v));
        sw.Stop();
        logger.LogInformation($"replays imported {newReplays} in {sw.ElapsedMilliseconds} ms");
        logger.LogInformation($"got {replays.Count} replays - new: Players {newPlayers}, Units: {newUnits}, Upgrades: {newUpgrades}");
    }

    private async Task<int> SaveReplays(List<Replay> replays, Dictionary<int, Uploader> fakeUploaderDic)
    {
        int i = 0;
        foreach (var replay in replays)
        {
            if (await SaveReplay(replay, fakeUploaderDic))
            {
                i++;
            }
        }
        return i;
    }

    private async Task<bool> SaveReplay(Replay replay, Dictionary<int, Uploader> fakeUploaderDic)
    {
        // logger.LogError($"saving replay {replay.ReplayHash}");
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if (!await IsDbDuplicate(context, replay, fakeUploaderDic))
        {
            if (replay.UploaderId > 0)
            {
                var fakeUploader = fakeUploaderDic[replay.UploaderId];
                context.Attach(fakeUploader);
                replay.Uploaders.Add(fakeUploader);
            }
            context.Replays.Add(replay);
            await context.SaveChangesAsync();
            return true;
        }
        else
        {
            return false;
        }
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
        return info.GetFiles("*", SearchOption.AllDirectories)
                .OrderBy(p => p.CreationTime)
                .Select(s => s.FullName)
                .ToList();
    }

    private static async Task<MemoryStream> UnzipAsync(string base64string)
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

    public async Task DEBUGSeedUploaders()
    {
        var dirs = Directory.GetDirectories(blobBaseDir);

        List<Uploader> uploaders = new();
        foreach (var dir in dirs)
        {
            if (Guid.TryParse(new DirectoryInfo(dir).Name, out Guid guid))
            {
                uploaders.Add(new()
                {
                    AppGuid = guid
                });
            }
        }

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        context.Uploaders.AddRange(uploaders);
        await context.SaveChangesAsync();
    }
}