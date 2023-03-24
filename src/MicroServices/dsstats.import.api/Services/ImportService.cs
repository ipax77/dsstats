using AutoMapper;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System.IO.Compression;
using System.Text.Json;

namespace dsstats.import.api.Services;

public partial class ImportService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<ImportService> logger;

    public ImportService(IServiceProvider serviceProvider, IMapper mapper, ILogger<ImportService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
        SeedImportCache();
    }

    private DbImportCache dbCache = new();

    private void SeedImportCache()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        dbCache.Units = context.Units
            .Select(s => new { s.Name, s.UnitId })
            .ToList().ToDictionary(k => k.Name, v => v.UnitId);
        dbCache.Upgrades = context.Upgrades
            .Select(s => new { s.Name, s.UpgradeId })
            .ToList().ToDictionary(k => k.Name, v => v.UpgradeId);
        dbCache.Players = context.Players
            .Select(s => new { s.ToonId, s.PlayerId })
            .ToList().ToDictionary(k => k.ToonId, v => v.PlayerId);
        dbCache.Uploaders = context.Uploaders
            .Select(s => new { s.AppGuid, s.UploaderId })
            .ToList().ToDictionary(k => k.AppGuid, v => v.UploaderId);
    }

    public async Task Import(ImportRequest request)
    {
        foreach (var blob in request.Replayblobs)
        {
            var blobDir = new DirectoryInfo(Path.GetDirectoryName(blob) ?? "").Name;

            if (!Guid.TryParse(blobDir, out Guid uploaderGuid))
            {
                logger.LogError($"failed determining blob guid from directory name. {blob}");
                continue;
            }

            List<Replay> replays;
            try
            {
                replays = await GetReplaysFromBlobFile(blob);
            }
            catch (Exception ex)
            {
                logger.LogError($"failed getting replays from {blob}: {ex.Message}");
                continue;
            }

            dbCache.Uploaders.TryGetValue(uploaderGuid, out int uploaderId);

            foreach (var replay in replays)
            {
                replay.UploaderId = uploaderId;
                if (!ImportChannel.Writer.TryWrite(replay))
                {
                    logger.LogError($"failed writing replay to import channel {replay.ReplayHash}");
                };
            }
        }
    }

    private async Task<List<Replay>> GetReplaysFromBlobFile(string blob)
    {
        return (await JsonSerializer.DeserializeAsync<List<ReplayDto>>
            (await UnzipAsync(await File.ReadAllTextAsync(blob)), new JsonSerializerOptions() { })
             ?? new())
             .Select(s => mapper.Map<Replay>(s))
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

public record DbImportCache
{
    public Dictionary<string, int> Units { get; set; } = new();
    public Dictionary<string, int> Upgrades { get; set; } = new();
    public Dictionary<int, int> Players { get; set; } = new();
    public Dictionary<Guid, int> Uploaders { get; set; } = new();
}

public record ImportRequest
{
    public List<string> Replayblobs { get; set; } = new();
}

public record ImportResult
{

}

