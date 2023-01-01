
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class CheatDetectService
{
    public async Task DetectNoUpload(bool dry = false)
    {
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
            //var ratio = los * 100.0 / total;

            //if (ratio > 50.0)
            //{
            //    playerIdNoUploads[playerId] = los;
            //}

            noUploadResults.Add(await GetNoUploadResult(playerId, total, los));
        }
        if (!dry)
        {
            // await SetNoUploads(playerIdNoUploads);
            await SetNoUploadResults(noUploadResults);
        }
    }

    private async Task SetNoUploadResults(List<NoUploadResult> noUploadResults)
    {
        foreach (var result in noUploadResults)
        {
            var dbResult = await context.NoUploadResults
                .FirstOrDefaultAsync(f => f.PlayerId == result.PlayerId);

            if (dbResult == null)
            {
                context.NoUploadResults.Add(result);
            }
            else
            {
                mapper.Map(result, dbResult);
            }
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

