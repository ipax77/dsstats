using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
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

            var uploader = await GetReplayUploder(context, replay);
            if (!await CheckIsDuplicate(context, replay, uploader))
            {
                if (uploader != null)
                {
                    replay.Uploaders.Add(uploader);
                }
                context.Replays.Add(replay);
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


    private static async Task<Uploader?> GetReplayUploder(ReplayContext context, Replay replay)
    {
        if (replay.UploaderId == 0)
        {
            return null;
        }
        return await context.Uploaders.FirstOrDefaultAsync(f => f.UploaderId == replay.UploaderId);
    }

}
