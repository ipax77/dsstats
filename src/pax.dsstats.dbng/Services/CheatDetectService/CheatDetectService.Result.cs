using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace pax.dsstats.dbng.Services;

public partial class CheatDetectService
{
    public async Task<CheatDetectResult> GetCheatDetectResult()
    {
        CheatDetectResult cheatDetectResult = new();

        var uploaderPlayerIds = await context.Players.Where(x => x.UploaderId != null)
            .Select(s => s.PlayerId)
            .ToListAsync();

        foreach (var playerId in uploaderPlayerIds)
        {
            var noUploadResults = await context.NoUploadResults
                .Where(x => x.PlayerId == playerId)
                .OrderBy(o => o.Created)
                .ToListAsync();

            if (noUploadResults.Count < 10)
            {
                continue;
            }

            double cheatRating = 0;

            var ratio = noUploadResults[0].NoUploadDefeats * 100.0 / noUploadResults[0].NoUploadTotal;

            if (ratio > 50)
            {
                cheatRating++;
            }

            var daysSinceNoUpload = Convert.ToInt32((noUploadResults[0].LatestUpload - noUploadResults[0].LatestNoUpload).TotalDays);

            if (daysSinceNoUpload < 30)
            {
                cheatRating++;
            }

            if (daysSinceNoUpload < 30)
            {
                cheatRating++;
            }

            var latestUpload = noUploadResults[0].LatestUpload;

            for (int i = 1; i < noUploadResults.Count; i++)
            {
                var result = noUploadResults[i];

                if (result.LatestNoUpload > result.LatestUpload)
                {
                    continue;
                }

                var stepRatio = result.NoUploadDefeats * 100.0 / result.NoUploadTotal;
                var stepLatestUplaod = result.LatestUpload;

                var cheatFactor = 1.0;

                if (stepLatestUplaod == latestUpload)
                {
                    cheatFactor = 0.1;
                }

                if (stepRatio - ratio > 0)
                {
                    cheatRating += cheatFactor;
                }

                var stepDaysSinceNoUpload = Convert.ToInt32((result.LatestUpload - result.LatestNoUpload).TotalDays);

                if (stepDaysSinceNoUpload - daysSinceNoUpload > 0)
                {
                    cheatRating += cheatFactor;
                }
            }

            cheatDetectResult.PlayerResults.Add(new()
            {
                PlayerId = playerId,
                CheatRating = cheatRating,
            });
        }

        StringBuilder sb = new();
        foreach (var result in cheatDetectResult.PlayerResults.OrderByDescending(o => o.CheatRating))
        {
            sb.Append($"{result.PlayerId} => {result.CheatRating}");
            sb.Append(Environment.NewLine);
        }
        logger.LogWarning(sb.ToString());
        return cheatDetectResult;
    }
}

public record CheatDetectResult
{
    public List<CheatDetectPlayerResult> PlayerResults { get; set; } = new();
}

public record CheatDetectPlayerResult
{
    public int PlayerId { get; init; }
    public double CheatRating { get; init; }
}