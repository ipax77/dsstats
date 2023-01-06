using AutoMapper;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System.IO.Compression;
using System.Text;

namespace pax.dsstats.web.Server.Services;

public partial class UploadService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<UploadService> logger;
    private const string blobBaseDir = "/data/ds/replayblobs";

    public UploadService(IServiceProvider serviceProvider, IMapper mapper, ILogger<UploadService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task<bool> ImportReplays(string gzipbase64String, Guid appGuid, DateTime latestReplay, bool dry = false)
    {
        try
        {

            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var uploader = await context.Uploaders.FirstOrDefaultAsync(f => f.AppGuid == appGuid);
            if (uploader == null)
            {
                return false;
            }

            if (uploader.IsDeleted || uploader.UploadIsDisabled)
            {
                return false;
            }

            if (!dry)
            {
                await SaveBlob(gzipbase64String, appGuid);
            }

            uploader.LatestUpload = DateTime.UtcNow;
            uploader.LatestReplay = latestReplay;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed updating uploader: {ex.Message}");
            return false;
        }

        // _ = Produce(gzipbase64String, appGuid);

        return true;
    }

    private async Task SaveBlob(string gzipbase64String, Guid appGuid)
    {
        var appDir = Path.Combine(blobBaseDir, appGuid.ToString());
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }
        var blobFilename = Path.Combine(appDir, DateTime.UtcNow.ToString(@"yyyyMMdd-HHmmss") + ".base64");

        int fs = 0;
        while (File.Exists(blobFilename))
        {
            await Task.Delay(1000);
            blobFilename = Path.Combine(appDir, DateTime.UtcNow.ToString(@"yyyyMMdd-HHmmss") + ".base64");
            fs++;
            if (fs > 5)
            {
                logger.LogError($"can't find filename while saving replayblob {blobFilename}");
                return;
            }
        }

        var tempBlobFilename = blobFilename + ".temp";
        await File.WriteAllTextAsync(tempBlobFilename, gzipbase64String);
        File.Move(tempBlobFilename, blobFilename);
    }

    public async Task<DateTime?> CreateOrUpdateUploader(UploaderDto uploader)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var dbUploader = await context.Uploaders
            .Include(i => i.Players)
            .Include(i => i.BattleNetInfos)
            .FirstOrDefaultAsync(f => f.AppGuid == uploader.AppGuid);

        if (dbUploader == null)
        {
            dbUploader = await FindDuplicateUploader(context, uploader);
        }

        if (dbUploader == null)
        {
            dbUploader = mapper.Map<Uploader>(uploader);
            dbUploader.Identifier = uploader.BattleNetInfos.SelectMany(s => s.PlayerUploadDtos).FirstOrDefault()?.Name ?? "Anonymous";
            context.Uploaders.Add(dbUploader);
            await CreateUploaderPlayers(context, dbUploader, uploader.BattleNetInfos.SelectMany(s => s.PlayerUploadDtos).ToList());
        }
        else
        {
            if (dbUploader.IsDeleted || dbUploader.UploadIsDisabled)
            {
                if ((DateTime.UtcNow - dbUploader.UploadLastDisabled) < TimeSpan.FromDays(7))
                {
                    return null;
                }
            }

            await UpdateUploaderPlayers(context, dbUploader, uploader);
            dbUploader.AppGuid = uploader.AppGuid;
            dbUploader.Identifier = dbUploader.Players.FirstOrDefault()?.Name ?? "Anonymous";
            dbUploader.AppVersion = uploader.AppVersion;

            dbUploader.UploadIsDisabled = false;
            dbUploader.IsDeleted = false;

            await context.SaveChangesAsync();
        }
        return await GetUploadersLatestReplay(context, dbUploader);
    }

    private async Task<DateTime> GetUploadersLatestReplay(ReplayContext context, Uploader uploader)
    {
        return await Task.FromResult(uploader.LatestReplay);
        //return await context.Uploaders
        //    .Include(i => i.Replays)
        //    .Where(x => x.UploaderId == uploader.UploaderId)
        //    .SelectMany(s => s.Replays)
        //    .OrderByDescending(o => o.GameTime)
        //    .Select(s => s.GameTime)
        //    .FirstOrDefaultAsync();
    }

    private async Task CreateUploaderPlayers(ReplayContext context, Uploader dbUploader, List<PlayerUploadDto> playerUploadDtos)
    {
        foreach (var player in playerUploadDtos)
        {
            var dbPlayer = await context.Players.FirstOrDefaultAsync(f => f.ToonId == player.ToonId);
            if (dbPlayer == null)
            {
                dbPlayer = mapper.Map<Player>(player);
                dbPlayer.Uploader = dbUploader;
                context.Players.Add(dbPlayer);
            }
            else
            {
                dbPlayer.Uploader = dbUploader;
                dbUploader.Players.Add(dbPlayer);
            }
        }
        await context.SaveChangesAsync();
    }

    private async Task UpdateUploaderPlayers(ReplayContext context, Uploader dbUploader, UploaderDto uploader)
    {
        for (int i = 0; i < dbUploader.Players.Count; i++)
        {
            var dbPlayer = dbUploader.Players.ElementAt(i);
            var uploaderPlayer = uploader.BattleNetInfos?
                .SelectMany(s => s.PlayerUploadDtos)
                .FirstOrDefault(f => f.ToonId == dbPlayer.ToonId);
            if (uploaderPlayer == null)
            {
                dbUploader.Players.Remove(dbPlayer);
                dbPlayer.Uploader = null;
            }
        }

        var uploaderPlayers = uploader.BattleNetInfos?.SelectMany(s => s.PlayerUploadDtos).ToList();

        if (uploaderPlayers != null && uploaderPlayers.Any())
        {

            for (int i = 0; i < uploaderPlayers.Count; i++)
            {
                var dbuploaderPlayer = dbUploader.Players.FirstOrDefault(f => f.ToonId == uploaderPlayers.ElementAt(i).ToonId);
                if (dbuploaderPlayer == null)
                {
                    var dbPlayer = await context.Players.FirstOrDefaultAsync(f => f.ToonId == uploaderPlayers.ElementAt(i).ToonId);
                    if (dbPlayer == null)
                    {
                        var dbAddPlayer = mapper.Map<Player>(uploaderPlayers.ElementAt(i));
                        dbAddPlayer.Uploader = dbUploader;
                        dbUploader.Players.Add(dbAddPlayer);
                    }
                    else
                    {
                        dbPlayer.Uploader = dbUploader;
                    }
                }
            }
        }

        if (dbUploader.BattleNetInfos != null)
        {
            for (int i = 0; i < dbUploader.BattleNetInfos.Count; i++)
            {
                var dbInfo = dbUploader.BattleNetInfos.ElementAt(i);
                var info = uploader.BattleNetInfos?.FirstOrDefault(f => f.BattleNetId == dbInfo.BattleNetId);
                if (info == null)
                {
                    dbUploader.BattleNetInfos.Remove(dbInfo);
                }
            }
        }

        if (uploader.BattleNetInfos != null)
        {
            for (int i = 0; i < uploader.BattleNetInfos.Count; i++)
            {
                var info = uploader.BattleNetInfos.ElementAt(i);
                var dbInfo = dbUploader.BattleNetInfos?.FirstOrDefault(f => f.BattleNetId == info.BattleNetId);
                if (dbInfo == null)
                {
                    if (dbUploader.BattleNetInfos == null)
                    {
                        dbUploader.BattleNetInfos = new List<BattleNetInfo>() { mapper.Map<BattleNetInfo>(info) };
                    }
                    else
                    {
                        dbUploader.BattleNetInfos.Add(mapper.Map<BattleNetInfo>(info));
                    }
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task<Uploader?> FindDuplicateUploader(ReplayContext context, UploaderDto uploader)
    {
        var ids = uploader.BattleNetInfos.Select(s => s.BattleNetId).ToList();

        if (!ids.Any())
        {
            return null;
        }

        return await context.BattleNetInfos
            .Include(i => i.Uploader)
            .Where(x => ids.Contains(x.BattleNetId))
            .Select(s => s.Uploader)
            .Distinct()
            .FirstOrDefaultAsync();
    }

    public async Task<DateTime> DisableUploader(Guid appGuid)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var uploader = await context.Uploaders.FirstOrDefaultAsync(f => f.AppGuid == appGuid);

        if (uploader == null)
        {
            return DateTime.MinValue;
        }

        if (uploader.UploadIsDisabled)
        {
            return uploader.UploadLastDisabled.AddDays(7);
        }

        uploader.UploadDisabledCount++;
        uploader.UploadLastDisabled = DateTime.UtcNow;
        uploader.UploadIsDisabled = true;

        await context.SaveChangesAsync();
        return DateTime.Today.AddDays(7);
    }

    public async Task<bool> DeleteUploader(Guid appGuid)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var uploader = await context.Uploaders
            .FirstOrDefaultAsync(f => f.AppGuid == appGuid);

        if (uploader == null || uploader.IsDeleted)
        {
            return true;
        }

        uploader.UploadDisabledCount++;
        uploader.UploadLastDisabled = DateTime.UtcNow;
        uploader.UploadIsDisabled = true;
        uploader.IsDeleted = true;

        await context.SaveChangesAsync();
        return true;
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
}
