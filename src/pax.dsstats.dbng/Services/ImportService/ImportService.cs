
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        Dictionary<Guid, int> uploaderDic = new();
        foreach (var blob in blobs)
        {
            var blobDir = new DirectoryInfo(Path.GetDirectoryName(blob) ?? "").Name;
            if (Guid.TryParse(blobDir, out Guid uploaderGuid))
            {
                int uploaderId = 0;
                if (uploaderDic.ContainsKey(uploaderGuid))
                {
                    uploaderId = uploaderDic[uploaderGuid];
                }
                else
                {
                    uploaderId = await GetUploaderId(uploaderGuid);
                    uploaderDic[uploaderGuid] = uploaderId;
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
        logger.LogInformation($"prepared blobs in {sw.ElapsedMilliseconds}");
        sw.Restart();
        int newPlayers = await CreateAndMapPlayers(replays);
        sw.Stop();
        logger.LogInformation($"mapped players in {sw.ElapsedMilliseconds}");
        sw.Restart();
        int newUnits = await CreateAndMapUnits(replays);
        sw.Stop();
        logger.LogInformation($"mapped units in {sw.ElapsedMilliseconds}");
        sw.Restart();
        int newUpgrades = await CreateAndMapUpgrades(replays);
        sw.Stop();
        logger.LogInformation($"mapped upgrades in {sw.ElapsedMilliseconds}");

        logger.LogInformation($"got {replays.Count} replays - new: Players {newPlayers}, Units: {newUnits}, Upgrades: {newUpgrades}");
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
        var replays = await JsonSerializer.DeserializeAsync<List<ReplayDto>>
            (await UnzipAsync(await File.ReadAllTextAsync(blob)), new JsonSerializerOptions() { })
             ?? new();
        replays.ForEach(f => f.UploaderGuid = uploaderGuid);
        return replays;
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
}