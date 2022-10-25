using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
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

            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var uploader = await context
                .Uploaders.Include(i => i.Players)
                .FirstOrDefaultAsync(f => f.AppGuid == appGuid);

            if (uploader == null)
            {
                return;
            }

            uploader.LatestReplay = replayDtos.Last().GameTime;
            await context.SaveChangesAsync();

            replays = replayDtos.Select(s => mapper.Map<Replay>(s)).ToList();
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

    public async Task InsertReplays()
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
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var dupReplayExists = context.Replays.Any(f => f.ReplayHash == replay.ReplayHash);

        if (!dupReplayExists)
        {
            context.Replays.Add(replay);
            await context.SaveChangesAsync();
        }

        else if (await HandleDuplicate(context, replay))
        {
            context.Replays.Add(replay);
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> HandleDuplicate(ReplayContext context, Replay replay)
    {
        var dupReplay = await context.Replays
            .Include(i => i.Players)
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
                .Include(i => i.Players)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.Players)
                    .ThenInclude(i => i.Upgrades)

                .FirstAsync(f => f.ReplayHash == replay.ReplayHash);

            foreach (var uploader in delReplay.Players.Where(x => x.IsUploader))
            {
                var uploaderDto = replay.Players.FirstOrDefault(f => f.Name == uploader.Name);
                if (uploaderDto == null)
                {
                    logger.LogWarning($"false positive duplicate (dtoPlayer)? {dupReplay.ReplayHash}");
                    return false;
                }
                uploaderDto.IsUploader = true;
            }

            context.Replays.Remove(delReplay);
            await context.SaveChangesAsync();
            return true;
        }
        else
        {
            foreach (var uploader in replay.Players.Where(x => x.IsUploader))
            {
                var dbUploader = dupReplay.Players.FirstOrDefault(f => f.Name == uploader.Name);
                if (dbUploader == null)
                {
                    logger.LogWarning($"false positive duplicate (dbPlayer)? {dupReplay.ReplayHash}");
                    return false;
                }
                dbUploader.IsUploader = true;
            }
            await context.SaveChangesAsync();
        }
        return false;
    }
}
