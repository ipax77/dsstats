using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace dsstats.dbServices;

public sealed class ReplayUserRatingOptions
{
    public string IpHashSalt { get; set; } = "dsstats-dev-replay-user-rating-salt";
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan ProcessedRetention { get; set; } = TimeSpan.FromDays(7);
    public int CollectorBatchSize { get; set; } = 1_000;
}

public enum ReplayUserRatingSubmitStatus
{
    Accepted,
    InvalidScore,
    ReplayNotFound,
    CooldownActive
}

public sealed record ReplayUserRatingSubmitResult(
    ReplayUserRatingSubmitStatus Status,
    ReplayUserRatingDto? Rating,
    DateTime? NextAllowedVoteAt);

public sealed class ReplayUserRatingService(
    IDbContextFactory<DsstatsContext> contextFactory,
    IOptions<ReplayUserRatingOptions> options,
    ILogger<ReplayUserRatingService> logger)
{
    private const int SubmitLockStripes = 64;
    private readonly ConcurrentDictionary<int, PendingReplayUserRating> pendingByReplayId = [];
    private readonly SemaphoreSlim pendingLoadLock = new(1, 1);
    private readonly SemaphoreSlim[] submitLocks = Enumerable.Range(0, SubmitLockStripes)
        .Select(_ => new SemaphoreSlim(1, 1))
        .ToArray();
    private volatile bool pendingLoaded;

    public string GetIpHash(IPAddress? ipAddress)
    {
        var normalized = NormalizeIpAddress(ipAddress);
        var payload = $"{options.Value.IpHashSalt}\n{normalized}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    public async Task<ReplayUserRatingDto?> GetRatingAsync(
        string replayHash,
        string ipHash,
        CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var replayId = await context.Replays
            .AsNoTracking()
            .Where(x => x.ReplayHash == replayHash)
            .Select(x => (int?)x.ReplayId)
            .FirstOrDefaultAsync(token);

        if (replayId is null)
        {
            return null;
        }

        return await BuildRatingDtoAsync(context, replayId.Value, ipHash, DateTime.UtcNow, token);
    }

    public async Task<ReplayUserRatingSubmitResult> SubmitRatingAsync(
        string replayHash,
        string ipHash,
        int score,
        CancellationToken token = default)
    {
        if (score is < 1 or > 5)
        {
            return new(ReplayUserRatingSubmitStatus.InvalidScore, null, null);
        }

        await using var context = await contextFactory.CreateDbContextAsync(token);
        var replayId = await context.Replays
            .AsNoTracking()
            .Where(x => x.ReplayHash == replayHash)
            .Select(x => (int?)x.ReplayId)
            .FirstOrDefaultAsync(token);

        if (replayId is null)
        {
            return new(ReplayUserRatingSubmitStatus.ReplayNotFound, null, null);
        }

        var submitLock = GetSubmitLock(replayId.Value, ipHash);
        await submitLock.WaitAsync(token);
        try
        {
            var now = DateTime.UtcNow;
            var cutoff = now - options.Value.Cooldown;
            var latestVote = await context.ReplayUserRatingCollects
                .AsNoTracking()
                .Where(x => x.ReplayId == replayId.Value
                    && x.IpHash == ipHash
                    && x.CreatedAt > cutoff)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.CreatedAt })
                .FirstOrDefaultAsync(token);

            if (latestVote is not null)
            {
                var nextAllowed = latestVote.CreatedAt + options.Value.Cooldown;
                var rating = await BuildRatingDtoAsync(context, replayId.Value, ipHash, now, token);
                return new(ReplayUserRatingSubmitStatus.CooldownActive, rating, nextAllowed);
            }

            context.ReplayUserRatingCollects.Add(new()
            {
                ReplayId = replayId.Value,
                IpHash = ipHash,
                Score = score,
                CreatedAt = now
            });

            await context.SaveChangesAsync(token);
            if (pendingLoaded)
            {
                AddPending(replayId.Value, score);
            }

            var acceptedRating = await BuildRatingDtoAsync(context, replayId.Value, ipHash, now, token);
            return new(ReplayUserRatingSubmitStatus.Accepted, acceptedRating, null);
        }
        finally
        {
            submitLock.Release();
        }
    }

    public async Task<int> CollectPendingVotesAsync(CancellationToken token = default)
    {
        await EnsurePendingLoadedAsync(token);

        await using var context = await contextFactory.CreateDbContextAsync(token);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var now = DateTime.UtcNow;
        var batchSize = Math.Max(1, options.Value.CollectorBatchSize);

        var batch = await context.ReplayUserRatingCollects
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.ReplayUserRatingCollectId)
            .Take(batchSize)
            .ToListAsync(token);

        if (batch.Count == 0)
        {
            await DeleteExpiredProcessedRowsAsync(context, now, token);
            await transaction.CommitAsync(token);
            return 0;
        }

        var grouped = batch
            .GroupBy(x => x.ReplayId)
            .Select(x => new PendingReplayUserRating(x.Key, x.Count(), x.Sum(v => v.Score)))
            .ToList();
        var replayIds = grouped.Select(x => x.ReplayId).ToList();

        var summaries = await context.ReplayUserRatingSummaries
            .Where(x => replayIds.Contains(x.ReplayId))
            .ToDictionaryAsync(x => x.ReplayId, token);

        foreach (var group in grouped)
        {
            if (summaries.TryGetValue(group.ReplayId, out var summary))
            {
                summary.VoteCount += group.Count;
                summary.ScoreSum += group.ScoreSum;
                summary.UpdatedAt = now;
            }
            else
            {
                context.ReplayUserRatingSummaries.Add(new()
                {
                    ReplayId = group.ReplayId,
                    VoteCount = group.Count,
                    ScoreSum = group.ScoreSum,
                    UpdatedAt = now
                });
            }
        }

        foreach (var vote in batch)
        {
            vote.ProcessedAt = now;
        }

        await context.SaveChangesAsync(token);
        await DeleteExpiredProcessedRowsAsync(context, now, token);
        await transaction.CommitAsync(token);

        foreach (var group in grouped)
        {
            SubtractPending(group.ReplayId, group.Count, group.ScoreSum);
        }

        return batch.Count;
    }

    public async Task RebuildPendingOverlayAsync(CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var pending = await context.ReplayUserRatingCollects
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null)
            .GroupBy(x => x.ReplayId)
            .Select(x => new PendingReplayUserRating(x.Key, x.Count(), x.Sum(v => v.Score)))
            .ToListAsync(token);

        pendingByReplayId.Clear();
        foreach (var row in pending)
        {
            pendingByReplayId[row.ReplayId] = row;
        }

        pendingLoaded = true;
    }

    private async Task<ReplayUserRatingDto> BuildRatingDtoAsync(
        DsstatsContext context,
        int replayId,
        string ipHash,
        DateTime now,
        CancellationToken token)
    {
        await EnsurePendingLoadedAsync(token);

        var summary = await context.ReplayUserRatingSummaries
            .AsNoTracking()
            .Where(x => x.ReplayId == replayId)
            .Select(x => new { x.VoteCount, x.ScoreSum })
            .FirstOrDefaultAsync(token);

        var latestVote = await context.ReplayUserRatingCollects
            .AsNoTracking()
            .Where(x => x.ReplayId == replayId && x.IpHash == ipHash)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Score, x.CreatedAt })
            .FirstOrDefaultAsync(token);

        var voteCount = summary?.VoteCount ?? 0;
        var scoreSum = summary?.ScoreSum ?? 0;
        if (pendingByReplayId.TryGetValue(replayId, out var pending))
        {
            voteCount += pending.Count;
            scoreSum += pending.ScoreSum;
        }

        DateTime? nextAllowed = null;
        if (latestVote is not null && latestVote.CreatedAt > now - options.Value.Cooldown)
        {
            nextAllowed = latestVote.CreatedAt + options.Value.Cooldown;
        }

        return new()
        {
            Average = voteCount == 0 ? 0 : Math.Round(scoreSum / (double)voteCount, 2),
            VoteCount = voteCount,
            CurrentVote = latestVote?.Score,
            NextAllowedVoteAt = nextAllowed
        };
    }

    private async Task EnsurePendingLoadedAsync(CancellationToken token)
    {
        if (pendingLoaded)
        {
            return;
        }

        await pendingLoadLock.WaitAsync(token);
        try
        {
            if (!pendingLoaded)
            {
                await RebuildPendingOverlayAsync(token);
            }
        }
        finally
        {
            pendingLoadLock.Release();
        }
    }

    private void AddPending(int replayId, int score)
    {
        pendingByReplayId.AddOrUpdate(
            replayId,
            static (key, score) => new PendingReplayUserRating(key, 1, score),
            static (_, current, score) => current with
            {
                Count = current.Count + 1,
                ScoreSum = current.ScoreSum + score
            },
            score);
    }

    private void SubtractPending(int replayId, int count, int scoreSum)
    {
        pendingByReplayId.AddOrUpdate(
            replayId,
            static (_, _) => new PendingReplayUserRating(0, 0, 0),
            static (_, current, state) =>
            {
                var nextCount = current.Count - state.Count;
                var nextSum = current.ScoreSum - state.ScoreSum;
                return nextCount <= 0 || nextSum <= 0
                    ? new PendingReplayUserRating(current.ReplayId, 0, 0)
                    : current with { Count = nextCount, ScoreSum = nextSum };
            },
            new PendingReplayUserRating(replayId, count, scoreSum));

        if (pendingByReplayId.TryGetValue(replayId, out var current) && current.Count == 0)
        {
            pendingByReplayId.TryRemove(replayId, out _);
        }
    }

    private SemaphoreSlim GetSubmitLock(int replayId, string ipHash)
    {
        var hash = HashCode.Combine(replayId, StringComparer.Ordinal.GetHashCode(ipHash));
        return submitLocks[(hash & int.MaxValue) % submitLocks.Length];
    }

    private async Task DeleteExpiredProcessedRowsAsync(DsstatsContext context, DateTime now, CancellationToken token)
    {
        var deleteBefore = now - options.Value.ProcessedRetention;
        var deleted = await context.ReplayUserRatingCollects
            .Where(x => x.ProcessedAt != null && x.ProcessedAt < deleteBefore)
            .ExecuteDeleteAsync(token);

        if (deleted > 0)
        {
            logger.LogInformation("Deleted {Count} processed replay user rating collect rows.", deleted);
        }
    }

    private static string NormalizeIpAddress(IPAddress? ipAddress)
    {
        if (ipAddress is null)
        {
            return "unknown";
        }

        return ipAddress.IsIPv4MappedToIPv6
            ? ipAddress.MapToIPv4().ToString()
            : ipAddress.ToString();
    }

    private readonly record struct PendingReplayUserRating(int ReplayId, int Count, int ScoreSum);
}
