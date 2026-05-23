
using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices;

public partial class ImportService
{
    private static async Task<DuplicateResult> HandleDuplicates(
        List<Replay> replays,
        DsstatsContext context,
        IReadOnlyDictionary<string, SpawnPlaybackEncodedSidecar> sidecarsByReplayHash)
    {
        var dedupedReplays = replays
            .GroupBy(r => r.ReplayHash)
            .Select(g =>
            {
                var keeper = g.OrderByDescending(r => r.Duration).First();
                // Merge IsUploader info from all duplicates in this batch
                foreach (var duplicate in g.Where(x => x != keeper))
                {
                    SetUploaders(keeper, duplicate);
                }
                return keeper;
            })
            .ToList();

        var importReplayHashes = dedupedReplays.Select(s => s.ReplayHash).ToHashSet();
        var dbReplays = await context.Replays
            .Include(i => i.Players)
            .Where(x => importReplayHashes.Contains(x.ReplayHash))
            .ToDictionaryAsync(k => k.ReplayHash, v => v);
        var dbReplayHashesWithSidecars = await context.ReplaySpawnPlaybacks
            .Where(x => x.Replay != null && importReplayHashes.Contains(x.Replay.ReplayHash))
            .Select(x => x.Replay!.ReplayHash)
            .ToHashSetAsync();

        DuplicateResult result = new();

        foreach (var replay in dedupedReplays)
        {
            sidecarsByReplayHash.TryGetValue(replay.ReplayHash, out var incomingSidecar);
            if (dbReplays.TryGetValue(replay.ReplayHash, out var dbReplay))
            {
                if (replay.Duration > dbReplay.Duration)
                {
                    var sidecarToSave = incomingSidecar;
                    if (sidecarToSave is null && dbReplayHashesWithSidecars.Contains(dbReplay.ReplayHash))
                    {
                        sidecarToSave = await GetSpawnPlaybackSidecar(dbReplay.ReplayId, context);
                    }

                    SetUploaders(replay, dbReplay);
                    await DeleteReplay(dbReplay.ReplayHash, context);
                    result.ReplaysToImport.Add(replay);
                    if (sidecarToSave is not null)
                    {
                        result.SidecarsToSave.Add(new(replay, null, sidecarToSave, ReplaceExisting: true));
                    }
                    result.Replaced++;
                }
                else
                {
                    SetUploaders(dbReplay, replay);
                    if (incomingSidecar is not null && !dbReplayHashesWithSidecars.Contains(dbReplay.ReplayHash))
                    {
                        result.SidecarsToSave.Add(new(null, dbReplay.ReplayId, incomingSidecar, ReplaceExisting: false));
                        dbReplayHashesWithSidecars.Add(dbReplay.ReplayHash);
                    }
                    result.Duplicates++;
                }
            }
            else
            {
                result.ReplaysToImport.Add(replay);
                if (incomingSidecar is not null)
                {
                    result.SidecarsToSave.Add(new(replay, null, incomingSidecar, ReplaceExisting: true));
                }
            }
        }

        return result;
    }

    private static void SetUploaders(Replay keepReplay, Replay dupReplay)
    {
        var dupPlayers = dupReplay.Players.Where(x => x.IsUploader).ToList();
        if (dupPlayers.Count == 0)
        {
            return;
        }

        foreach (var replayPlayer in keepReplay.Players.Where(x => !x.IsUploader))
        {
            var dupPlayer = dupPlayers.FirstOrDefault(f => f.PlayerId == replayPlayer.PlayerId);
            if (dupPlayer is not null)
            {
                replayPlayer.IsUploader = true;
            }
        }
    }

    private static async Task DeleteReplay(string replayHash, DsstatsContext context)
    {
        try
        {
            var replay = await context.Replays
                .Include(i => i.Players)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.Ratings)
                    .ThenInclude(i => i.ReplayPlayerRatings)
                .Include(i => i.Players)
                    .ThenInclude(i => i.Ratings)
                .Include(i => i.Players)
                    .ThenInclude(i => i.Upgrades)
                .FirstOrDefaultAsync(f => f.ReplayHash == replayHash);

            if (replay is not null)
            {
                context.Replays.Remove(replay);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static async Task<SpawnPlaybackEncodedSidecar?> GetSpawnPlaybackSidecar(int replayId, DsstatsContext context)
    {
        return await context.ReplaySpawnPlaybacks
            .AsNoTracking()
            .Where(x => x.ReplayId == replayId)
            .Select(x => new SpawnPlaybackEncodedSidecar(
                x.Payload,
                x.CompressedLength,
                x.UncompressedLength,
                x.UnitCount,
                x.FormatVersion,
                x.Compression))
            .FirstOrDefaultAsync();
    }

    private static async Task SaveSpawnPlaybackSidecars(List<SpawnPlaybackSidecarSave> sidecars, DsstatsContext context)
    {
        var replayIds = sidecars
            .Select(sidecar => sidecar.Replay?.ReplayId ?? sidecar.ReplayId.GetValueOrDefault())
            .Where(replayId => replayId > 0)
            .Distinct()
            .ToList();
        if (replayIds.Count == 0)
        {
            return;
        }

        var existingByReplayId = await context.ReplaySpawnPlaybacks
            .Where(x => replayIds.Contains(x.ReplayId))
            .ToDictionaryAsync(k => k.ReplayId);

        bool changed = false;
        foreach (var sidecar in sidecars)
        {
            int replayId = sidecar.Replay?.ReplayId ?? sidecar.ReplayId.GetValueOrDefault();
            if (replayId == 0)
            {
                continue;
            }

            if (existingByReplayId.TryGetValue(replayId, out var existing))
            {
                if (!sidecar.ReplaceExisting)
                {
                    continue;
                }

                ApplySpawnPlayback(existing, sidecar.SpawnPlayback);
                changed = true;
            }
            else
            {
                var entity = new ReplaySpawnPlayback
                {
                    ReplayId = replayId,
                    CreatedAt = DateTime.UtcNow
                };
                ApplySpawnPlayback(entity, sidecar.SpawnPlayback);
                context.ReplaySpawnPlaybacks.Add(entity);
                existingByReplayId[replayId] = entity;
                changed = true;
            }
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }
    }

    private static void ApplySpawnPlayback(ReplaySpawnPlayback entity, SpawnPlaybackEncodedSidecar sidecar)
    {
        entity.FormatVersion = sidecar.FormatVersion;
        entity.Compression = sidecar.Compression;
        entity.CompressedLength = sidecar.CompressedLength;
        entity.UncompressedLength = sidecar.UncompressedLength;
        entity.UnitCount = sidecar.UnitCount;
        entity.Payload = sidecar.Payload;
        entity.CreatedAt = DateTime.UtcNow;
    }
}

internal sealed class DuplicateResult
{
    public int Duplicates { get; set; }
    public int Replaced { get; set; }
    public List<Replay> ReplaysToImport { get; set; } = [];
    public List<SpawnPlaybackSidecarSave> SidecarsToSave { get; } = [];
}

internal sealed record SpawnPlaybackSidecarSave(
    Replay? Replay,
    int? ReplayId,
    SpawnPlaybackEncodedSidecar SpawnPlayback,
    bool ReplaceExisting);
