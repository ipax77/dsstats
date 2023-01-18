
using Microsoft.Extensions.Logging;

namespace pax.dsstats.dbng.Services;

public partial class ImportService
{
    private List<Replay> HandleLocalDuplicates(List<Replay> replays)
    {
        HashSet<string> skipReplayHashes = new();
        foreach (var replay in replays.ToArray())
        {
            if (skipReplayHashes.Contains(replay.ReplayHash))
            {
                continue;
            }
            var dupReplays = GetLocalDuplicateReplays(replay, replays);
            if (dupReplays.Any())
            {
                (replays, var delHashes) = HandleLocalDuplicate(replay, replays, dupReplays);
                skipReplayHashes.UnionWith(delHashes);
            }
        }
        return replays;
    }


    private static List<Replay> GetLocalDuplicateReplays(Replay replay, List<Replay> replays)
    {
        var dupReplays = replays
            .Where(x => x.ReplayHash == replay.ReplayHash)
            .ToList();

        List<string> replayLastSpawnHashes = replay.ReplayPlayers.Select(s => s.LastSpawnHash ?? "")
            .Where(x => !String.IsNullOrEmpty(x))
            .ToList();

        var spawnDupReplays = replayLastSpawnHashes.Any() ?
            replays.Where(x => x.ReplayPlayers.Any(a => a.LastSpawnHash != null
                && replayLastSpawnHashes.Contains(a.LastSpawnHash))).ToList()
            : new List<Replay>();

        if (spawnDupReplays.Count > 0)
        {
            dupReplays.AddRange(spawnDupReplays);
        }

        return dupReplays.Where(x => x != replay).Distinct().ToList();
    }

    private (List<Replay>, List<string>) HandleLocalDuplicate(Replay replay, List<Replay> replays, List<Replay> dupReplays)
    {
        List<ReplayPlayer> replayPlayers = replay.ReplayPlayers.ToList();
        HashSet<Replay> delReplays = new();
        Replay keepReplay = replay;
        HashSet<Uploader> uploaders = new();
        if (replay.UploaderId > 0)
        {
            uploaders.Add(new Uploader() { UploaderId = replay.UploaderId });
        }

        foreach (var dupReplay in dupReplays)
        {
            if (!DuplicateIsPlausible(replay, dupReplay))
            {
                logger.LogWarning($"false positive duplicate (gametime)? {dupReplay.ReplayHash}");
                replay.ReplayPlayers.ToList().ForEach(f => f.LastSpawnHash = null);
                continue;
            }

            var syncedReplayPlayers = SyncReplayPlayers(replayPlayers, dupReplay.ReplayPlayers.ToList());
            if (!syncedReplayPlayers.Any())
            {
                replay.ReplayPlayers.ToList().ForEach(f => f.LastSpawnHash = null);
                logger.LogWarning($"false positive duplicate (dtoPlayer)? {dupReplay.ReplayHash}");
                continue;
            }
            replayPlayers = syncedReplayPlayers;
            uploaders = uploaders.Union(dupReplay.Uploaders).ToHashSet();
            if (dupReplay.UploaderId > 0 && !uploaders.Any(a => a.UploaderId == dupReplay.UploaderId))
            {
                uploaders.Add(new Uploader() { UploaderId = dupReplay.UploaderId });
            }

            if (dupReplay.Duration > keepReplay.Duration)
            {
                delReplays.Add(keepReplay);
                keepReplay = dupReplay;
            }
            else
            {
                delReplays.Add(dupReplay);
            }
        }

        replayPlayers.ForEach(f => { f.Replay = keepReplay; f.ReplayId = keepReplay.ReplayId; });
        keepReplay.ReplayPlayers = replayPlayers;
        keepReplay.Uploaders = uploaders;

        replays = DeleteLocalReplays(replays, delReplays);
        return (replays, delReplays.Select(s => s.ReplayHash).ToList());
    }

    private static List<Replay> DeleteLocalReplays(List<Replay> replays, HashSet<Replay> delReplays)
    {
        if (!delReplays.Any())
        {
            return replays;
        }
        return replays.Except(delReplays).ToList();
    }
}