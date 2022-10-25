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
    private readonly SemaphoreSlim ss = new(1, 1);

    public UploadService(IServiceProvider serviceProvider, IMapper mapper, ILogger<UploadService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task ImportReplays(string gzipbase64String, Guid appGuid)
    {
        await ss.WaitAsync();
        try
        {
            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var uploader = await context.Uploaders.FirstOrDefaultAsync(f => f.AppGuid == appGuid);
            if (uploader == null)
            {
                return;
            }

            uploader.LatestUpload = DateTime.UtcNow;
            uploader.LatestReplay = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed updating uploader: {ex.Message}");
        }
        finally { ss.Release(); }

        _ = Produce(gzipbase64String, appGuid);
    }

    public async Task<DateTime> CreateOrUpdateUploader(UploaderDto uploader)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var dbUplaoder = await context.Uploaders
            .Include(i => i.Players)
            .Include(i => i.BattleNetInfos)
            .FirstOrDefaultAsync(f => f.AppGuid == uploader.AppGuid);

        if (dbUplaoder == null)
        {
            dbUplaoder = mapper.Map<Uploader>(uploader);
            dbUplaoder.Identifier = dbUplaoder.Players.FirstOrDefault()?.Name ?? "Anonymous";
            await CreateUploaderPlayers(context, dbUplaoder);

            context.Uploaders.Add(dbUplaoder);
            await context.SaveChangesAsync();
        }
        else
        {
            await UpdateUploaderPlayers(context, dbUplaoder, uploader);
            if (dbUplaoder.AppGuid != uploader.AppGuid)
            {
                dbUplaoder.AppGuid = uploader.AppGuid;
            }
            await context.SaveChangesAsync();
        }
        return await GetUploadersLatestReplay(context, dbUplaoder);
    }

    private async Task<DateTime> GetUploadersLatestReplay(ReplayContext context, Uploader uploader)
    {
        if (uploader.LatestReplay == DateTime.MinValue)
        {
            return uploader.LatestReplay;
        }

        return await context.Replays
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
                .ThenInclude(i => i.Uploader)
            .OrderByDescending(o => o.GameTime)
            .Where(x => x.Players.Any(a => a.Player.Uploader != null && a.Player.Uploader.UploaderId == uploader.UploaderId))
            .Select(s => s.GameTime)
            .FirstOrDefaultAsync();
    }

    private static async Task CreateUploaderPlayers(ReplayContext context, Uploader dbUplaoder)
    {
        foreach (var player in dbUplaoder.Players.ToArray())
        {
            var dbPlayer = await context.Players.FirstOrDefaultAsync(f => f.ToonId == player.ToonId);
            if (dbPlayer != null)
            {
                dbPlayer.Uploader = dbUplaoder;
                dbUplaoder.Players.Remove(player);
                dbUplaoder.Players.Add(dbPlayer);
            }
        }
    }

    private async Task UpdateUploaderPlayers(ReplayContext context, Uploader dbUploader, UploaderDto uploader)
    {
        for (int i = 0; i < dbUploader.Players.Count; i++)
        {
            var dbPlayer = dbUploader.Players.ElementAt(i);
            var uploaderPlayer = uploader.Players.FirstOrDefault(f => f.Toonid == dbPlayer.ToonId);
            if (uploaderPlayer == null)
            {
                dbUploader.Players.Remove(dbPlayer);
                dbPlayer.Uploader = null;
            }
            else if (uploaderPlayer.Name != dbPlayer.Name)
            {
                dbPlayer.Name = uploaderPlayer.Name;
            }
        }

        for (int i = 0; i < uploader.Players.Count; i++)
        {
            var dbuploaderPlayer = dbUploader.Players.FirstOrDefault(f => f.ToonId == uploader.Players.ElementAt(i).Toonid);
            if (dbuploaderPlayer == null)
            {
                var dbPlayer = await context.Players.FirstOrDefaultAsync(f => f.ToonId == uploader.Players.ElementAt(i).Toonid);
                if (dbPlayer == null)
                {
                    dbUploader.Players.Add(mapper.Map<Player>(uploader.Players.ElementAt(i)));
                }
                else
                {
                    dbPlayer.Uploader = dbUploader;
                }
            }
        }

        if (dbUploader.BattleNetInfos != null)
        {
            for (int i = 0; i < dbUploader.BattleNetInfos.Count; i++)
            {
                var dbInfo = dbUploader.BattleNetInfos.ElementAt(i);
                var info = uploader.BatteBattleNetInfos?.FirstOrDefault(f => f.BattleNetId == dbInfo.BattleNetId);
                if (info == null)
                {
                    dbUploader.BattleNetInfos.Remove(dbInfo);
                }
            }
        }

        if (uploader.BatteBattleNetInfos != null)
        {
            for (int i = 0; i < uploader.BatteBattleNetInfos.Count; i++)
            {
                var info = uploader.BatteBattleNetInfos.ElementAt(i);
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
