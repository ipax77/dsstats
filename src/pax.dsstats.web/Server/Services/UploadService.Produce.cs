using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using sc2dsstats.db;
using sc2dsstats.shared;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;

namespace pax.dsstats.web.Server.Services;

public partial class UploadService
{
    private Channel<Replay> ReplayChannel = Channel.CreateUnbounded<Replay>();
    private object lockobject = new();
    private bool insertJobRunning;
    private readonly SemaphoreSlim saveReplaySs = new(1, 1);
    public bool WeHaveNewReplays { get; set; }

    public async Task Produce(string gzipbase64string, Guid appGuid)
    {
        logger.LogWarning($"producing replays for {appGuid}");

        Stopwatch sw = new();
        sw.Start();

        List<Replay> replays = new();
        try
        {
            var replayDtos = JsonSerializer.Deserialize<List<ReplayDto>>(await UnzipAsync(gzipbase64string))?.OrderBy(o => o.GameTime).ToList();

            if (replayDtos == null || !replayDtos.Any())
            {
                return;
            }

            WeHaveNewReplays = true;

            int uploaderId = await SetUploaderLatestReplay(appGuid, replayDtos.Last().GameTime);

            replays = replayDtos.Select(s => mapper.Map<Replay>(s)).ToList();
            replays.ForEach(f => f.UploaderId = uploaderId);
            await MapUpgrades(replays);
            await MapUnits(replays);
            await MapPlayers(replays);
        }
        catch (Exception ex)
        {
            logger.LogError($"failed preparing replay import: {ex.Message}");
            return;
        }
        sw.Stop();
        logger.LogWarning($"prepared replay import in {sw.ElapsedMilliseconds}ms");

        _ = InsertReplays();

        for (int i = 0; i < replays.Count; i++)
        {
            ReplayChannel.Writer.TryWrite(replays[i]);
        }
    }

    private async Task<int> SetUploaderLatestReplay(Guid appGuid, DateTime latestReplayDateTime)
    {
        await saveReplaySs.WaitAsync();
        try
        {
            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var uploader = await context
                .Uploaders.Include(i => i.Players)
                .FirstOrDefaultAsync(f => f.AppGuid == appGuid);

            if (uploader == null)
            {
                return 0;
            }

            uploader.LatestReplay = latestReplayDateTime;
            await context.SaveChangesAsync();
            return uploader.UploaderId;
        }
        catch (Exception ex)
        {
            logger.LogError($"failed setting latestReplay: {ex.Message}");
        }
        finally
        {
            saveReplaySs.Release();
        }
        return 0;
    }

    private async Task InsertReplays()
    {
        lock (lockobject)
        {
            if (insertJobRunning)
            {
                return;
            }
            insertJobRunning = true;
        }

        logger.LogWarning($"start importing replays: {DateTime.UtcNow}");

        while (await ReplayChannel.Reader.WaitToReadAsync())
        {
            if (ReplayChannel.Reader.TryRead(out Replay? replay))
            {
                if (replay == null)
                {
                    continue;
                }

                try
                {
                    await SaveReplay(replay);
                }
                catch (Exception ex)
                {
                    logger.LogError($"failed inserting replay {replay.ReplayHash}: {ex.Message}");
                }
            }
        }
        insertJobRunning = false;
        logger.LogWarning($"importing replays done: {DateTime.UtcNow}");
    }

    private async Task SaveReplay(Replay replay)
    {
        await saveReplaySs.WaitAsync();
        try
        {
            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var dupReplayExists = context.Replays.Any(f => f.ReplayHash == replay.ReplayHash);

            if (!dupReplayExists)
            {
                context.Replays.Add(replay);
                await AddUploader(context, replay);
                await context.SaveChangesAsync();
            }

            else if (await HandleDuplicate(context, replay))
            {
                context.Replays.Add(replay);
                await AddUploader(context, replay);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed saving replay: {ex.Message}");
        }
        finally
        {
            saveReplaySs.Release();
        }
    }

    private async Task AddUploader(ReplayContext context, Replay replay)
    {
        if (replay.UploaderId == 0)
        {
            return;
        }
        var uploader = await context.Uploaders.FirstOrDefaultAsync(f => f.UploaderId == replay.UploaderId);
        if (uploader != null)
        {
            replay.Uploaders.Add(uploader);
        }
    }

    public async Task<bool> HandleDuplicate(ReplayContext context, Replay replay)
    {
        var dupReplay = await context.Replays
            .Include(i => i.Uploaders)
            .Include(i => i.ReplayPlayers)
            .FirstOrDefaultAsync(f => f.ReplayHash == replay.ReplayHash);

        if (dupReplay == null)
        {
            return false;
        }

        if (dupReplay.GameTime - replay.GameTime > TimeSpan.FromDays(1))
        {
            logger.LogWarning($"false positive duplicate? {dupReplay.ReplayHash}");
            return false;
        }

        if (replay.Duration > dupReplay.Duration + 60)
        {
            var delReplay = await context.Replays
                .Include(i => i.Uploaders)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Upgrades)

                .FirstAsync(f => f.ReplayHash == replay.ReplayHash);

            foreach (var uploaderReplayPlayer in delReplay.ReplayPlayers.Where(x => x.IsUploader))
            {
                var uploaderDto = replay.ReplayPlayers.FirstOrDefault(f => f.Name == uploaderReplayPlayer.Name);
                if (uploaderDto == null)
                {
                    logger.LogWarning($"false positive duplicate (dtoPlayer)? {dupReplay.ReplayHash}");
                    return false;
                }
                uploaderDto.IsUploader = true;
            }

            foreach (var uploader in delReplay.Uploaders)
            {
                replay.Uploaders.Add(uploader);
            }

            context.Replays.Remove(delReplay);
            await context.SaveChangesAsync();
            return true;
        }
        else
        {
            foreach (var uploaderReplayPlayer in replay.ReplayPlayers.Where(x => x.IsUploader))
            {
                var dbUploader = dupReplay.ReplayPlayers.FirstOrDefault(f => f.Name == uploaderReplayPlayer.Name);
                if (dbUploader == null)
                {
                    logger.LogWarning($"false positive duplicate (dbPlayer)? {dupReplay.ReplayHash}");
                    return false;
                }
                dbUploader.IsUploader = true;
            }

            if (replay.UploaderId > 0)
            {
                var uploader = await context.Uploaders.FirstOrDefaultAsync(f => f.UploaderId == replay.UploaderId);
                if (uploader != null)
                {
                    dupReplay.Uploaders.Add(uploader);
                }
            }
            await context.SaveChangesAsync();
        }
        return false;
    }
}
