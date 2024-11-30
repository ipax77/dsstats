using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace dsstats.api.Services;

public partial class UploadService
{
    private readonly string blobBaseDir = "/data/ds/replayblobs";
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<UploadService> logger;
    private SemaphoreSlim blobFileSs = new(1, 1);
    ConcurrentDictionary<string, bool> ReplayBlobsInQueue = new();

    public event EventHandler<BlobImportEventArgs>? BlobImported;
    private void OnBlobImported(BlobImportEventArgs e)
    {
        EventHandler<BlobImportEventArgs>? handler = BlobImported;
        handler?.Invoke(this, e);
    }

    public UploadService(IServiceScopeFactory scopeFactory, ILogger<UploadService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async void ImportInit()
    {
        var blobs = Directory.EnumerateFiles(blobBaseDir, "*base64", SearchOption.AllDirectories);

        if (!blobs.Any())
        {
            return;
        }

        foreach (var blobFile in blobs)
        {
            ReplayBlobsInQueue.AddOrUpdate(blobFile, true, (k, v) => true);
            var blobDir = new DirectoryInfo(Path.GetDirectoryName(blobFile) ?? "").Name;

            if (!Guid.TryParse(blobDir, out Guid uploaderGuid))
            {
                logger.LogError("failed determining blob guid from directory name. {blob}", blobFile);
                continue;
            }

            (var appVersion, var requestNames) = await GetRequestNames(uploaderGuid);

            UploadDto uploadDto = new()
            {
                AppGuid = uploaderGuid,
                AppVersion = appVersion,
                RequestNames = requestNames,
                Base64ReplayBlob = File.ReadAllText(blobFile)
            };

            ProduceUploadJob(uploadDto, blobFile);
        }
    }

    public async Task<(string appVersion, List<RequestNames>)> GetRequestNames(Guid appGuid)
    {
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var result = await context.Players
            .Where(x => x.Uploader != null && x.Uploader.AppGuid == appGuid)
            .Select(s => new RequestNames(s.Name, s.ToonId, s.RegionId, s.RealmId))
            .ToListAsync();

        var appVersion = (await context.Uploaders
            .Where(x => x.AppGuid == appGuid)
            .Select(s => s.AppVersion)
            .FirstOrDefaultAsync()) ?? "unknown";

        return (appVersion, result);
    }

    public async Task UploadPrepV6(UploaderDtoV6 uploaderDtoV6)
    {
        UploadDto uploadDto = new()
        {
            AppGuid = uploaderDtoV6.AppGuid,
            RequestNames = uploaderDtoV6.BattleNetInfos
                .SelectMany(s => s.PlayerUploadDtos)
                .Select(t => new RequestNames(t.Name, t.ToonId, t.RegionId, t.RealmId))
                .ToList(),
        };
        await CreateOrUpdateUploader(uploadDto);
    }

    public async Task<bool> Upload(UploadDto uploadDto)
    {
        if (ReplayBlobsInQueue.Count > 100)
        {
            OnBlobImported(new() { ReplayBlob = "too_many_uploads", Success = false });
            return false;
        }
        var blobFile = await SaveBlob(uploadDto.Base64ReplayBlob, uploadDto.AppGuid);

        if (string.IsNullOrEmpty(blobFile))
        {
            return false;
        }

        ReplayBlobsInQueue.AddOrUpdate(blobFile, true, (k, v) => true);
        return ProduceUploadJob(uploadDto, blobFile);
    }

    private async Task<string> SaveBlob(string gzipbase64String, Guid appGuid)
    {
        await blobFileSs.WaitAsync();
        try
        {
            var appDir = Path.Combine(blobBaseDir, appGuid.ToString());
            if (!Directory.Exists(appDir))
            {
                Directory.CreateDirectory(appDir);
            }
            var blobFilename = Path.Combine(appDir, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".base64");
            var tempBlobFilename = blobFilename + ".temp";

            int fs = 0;
            while (File.Exists(blobFilename) || File.Exists(tempBlobFilename))
            {
                blobFilename = Path.Combine(appDir, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + $"_{fs}.base64");
                tempBlobFilename = blobFilename + ".temp";
                fs++;
                if (fs > 200)
                {
                    throw new FileNotFoundException(blobFilename);
                }
            }

            await File.WriteAllTextAsync(tempBlobFilename, gzipbase64String);
            File.Move(tempBlobFilename, blobFilename);
            return blobFilename;
        }
        catch (Exception ex)
        {
            logger.LogError("failed saving replay blob file: {error}", ex.Message);
        }
        finally
        {
            blobFileSs.Release();
        }
        return "";
    }

    private static async Task<List<ReplayDto>> GetReplaysFromBase64String(string base64String)
    {
        var jsonString = await UnzipAsync(base64String);
        return JsonSerializer.Deserialize<List<ReplayDto>>(jsonString) ?? new();
    }

    private static async Task<string> UnzipAsync(string base64string)
    {
        var bytes = Convert.FromBase64String(base64string);
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                await gs.CopyToAsync(mso);
            }
            return Encoding.UTF8.GetString(mso.ToArray());
        }
    }

    public static async Task<string> ZipAsync(string jsonString)
    {
        var bytes = Encoding.UTF8.GetBytes(jsonString);
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
            {
                await gzipStream.WriteAsync(bytes, 0, bytes.Length);
            }
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
}

public class BlobImportEventArgs : EventArgs
{
    public string ReplayBlob { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int Imported { get; set; }
    public int Duplicates { get; set; }
}
