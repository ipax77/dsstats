
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Diagnostics;

namespace pax.dsstats.dbng.Services;

public partial class CheatDetectService
{
    private const int maxNoUploadInfoKeepCount = 40;

    public async Task DetectNoUpload(bool dry = false)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var uploaderPlayerIds = await context.Players.Where(x => x.UploaderId != null)
                    .Select(s => s.PlayerId)
                    .ToListAsync();

        Dictionary<int, int> playerIdNoUploads = new();
        List<NoUploadResult> noUploadResults = new List<NoUploadResult>();

        foreach (var playerId in uploaderPlayerIds)
        {
            var results = await context.ReplayPlayers
                .Include(i => i.Replay)
                .Where(x => x.PlayerId == playerId
                    && !x.IsUploader)
                .GroupBy(g => g.PlayerResult)
                .Select(s => new
                {
                    Result = s.Key,
                    Count = s.Count()
                })
                .ToListAsync();

            int los = results.FirstOrDefault(f => f.Result == PlayerResult.Los)?.Count ?? 0;

            if (los == 0)
            {
                continue;
            }

            int total = results.Sum(s => s.Count);
            noUploadResults.Add(await GetNoUploadResult(playerId, total, los));
        }
        if (!dry)
        {
            await SetNoUploadResults(noUploadResults);
            await CleanupNoUploadResults();
        }
        sw.Stop();
        logger.LogWarning($"noupload results created in {sw.ElapsedMilliseconds}");
    }

    private async Task SetNoUploadResults(List<NoUploadResult> noUploadResults)
    {
        DateTime created = DateTime.UtcNow;
        foreach (var result in noUploadResults)
        {
            result.Created = created;
            context.NoUploadResults.Add(result);
        }
        await context.SaveChangesAsync();
    }

    private async Task<NoUploadResult> GetNoUploadResult(int playerId, int noUploadCount, int noUploadDefeats)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        return new NoUploadResult()
        {
            PlayerId = playerId,
            TotalReplays = await context.ReplayPlayers
                .Where(x => x.Player.PlayerId == playerId)
                .CountAsync(),
            LatestReplay = await context.ReplayPlayers
                .Where(x => x.Player.PlayerId == playerId)
                .OrderByDescending(o => o.Replay.GameTime)
                .Select(s => s.Replay.GameTime)
                .FirstOrDefaultAsync(),
            NoUploadTotal = noUploadCount,
            NoUploadDefeats = noUploadDefeats,
            LatestNoUpload = await context.ReplayPlayers
                .Where(x => x.PlayerId == playerId
                    && !x.IsUploader)
                .OrderByDescending(o => o.Replay.GameTime)
                .Select(s => s.Replay.GameTime)
                .FirstOrDefaultAsync(),
            LatestUpload = await context.Players
                .Where(x => x.PlayerId == playerId)
                .Select(s => s.Uploader.LatestUpload)
                .FirstOrDefaultAsync()
        };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    private async Task CleanupNoUploadResults()
    {
        DateTime minCreated = DateTime.UtcNow.AddDays(maxNoUploadInfoKeepCount * -1);

        var removeResults = await context.NoUploadResults
            .Where(x => x.Created < minCreated)
            .ToListAsync();

        if (!removeResults.Any())
        {
            return;
        }

        context.NoUploadResults.RemoveRange(removeResults);
        await context.SaveChangesAsync();
    }

    private async Task SetNoUploads(Dictionary<int, int> playerIdNoUploads)
    {
        int i = 0;
        foreach (var ent in playerIdNoUploads)
        {
            var player = await context.Players
                .FirstOrDefaultAsync(f => f.PlayerId == ent.Key);

            if (player == null)
            {
                continue;
            }
            player.NotUploadCount = ent.Value;

            i++;
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();
    }
}

