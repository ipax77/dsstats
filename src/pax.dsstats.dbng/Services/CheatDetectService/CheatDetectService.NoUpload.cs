
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

        double maxRatio = 0;

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
            var ratio = los * 100.0 / total;

            if (ratio > maxRatio)
            {
                maxRatio = ratio;
            }

            if (ratio > 50.0)
            {
                playerIdNoUploads[playerId] = los;
            }
        }
        logger.LogWarning($"maxNoLosUploadRatio: {maxRatio.ToString("N2")}");

        if (!dry)
        {
            await SetNoUploads(playerIdNoUploads);
        }
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