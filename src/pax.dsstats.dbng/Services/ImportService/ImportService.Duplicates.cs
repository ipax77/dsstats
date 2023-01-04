
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Extensions;

namespace pax.dsstats.dbng.Services;

public partial class ImportService
{
    public async Task<(Dictionary<string, int>, Dictionary<string, int>)> CollectDbDuplicates(List<Replay> replays)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replayHashes = replays.Select(s => s.ReplayHash).Distinct().ToList();

        var dupReplays = (await context.Replays
            .Where(x => replayHashes.Contains(x.ReplayHash))
            .Select(s => new DbDupHelper()
            {
                ReplayId = s.ReplayId,
                ReplayHash = s.ReplayHash,
            })
            .ToListAsync())
            .Distinct()
            .ToDictionary(k => k.ReplayId, v => v);

        var replaysLastSpawnHashes = replays.SelectMany(s => s.ReplayPlayers)
            .Where(x => x.LastSpawnHash != null)
            .Select(s => s.LastSpawnHash)
            .Distinct()
            .ToList();

        Dictionary<int, DbDupHelper> lsDupReplays = new();
        HashSet<int> lsDupreplayIds = new();
        if (replaysLastSpawnHashes.Any())
        {
            var lsDupHelpers = await context.ReplayPlayers
                .Where(x => x.LastSpawnHash != null && replaysLastSpawnHashes.Contains(x.LastSpawnHash))
                .Select(s => new DbDupHelper()
                {
                    ReplayId = s.ReplayId,
                    LastSpawnHash = s.LastSpawnHash
                })
                .ToListAsync();

            lsDupreplayIds = lsDupHelpers.Select(s => s.ReplayId).ToHashSet();

            foreach (int id in lsDupreplayIds)
            {
                lsDupReplays[id] = new DbDupHelper()
                {
                    ReplayId = id,
                    LastSpawnHashes = lsDupHelpers.Where(x => x.ReplayId == id).Select(s => s.LastSpawnHash ?? "").ToList()
                };
            }
        }

        var dupreplayIds = dupReplays.Values.Select(s => s.ReplayId).ToHashSet();
        dupreplayIds.UnionWith(lsDupreplayIds);


        Dictionary<string, int> replayHashMap = new();
        Dictionary<string, int> lastSpawnHashMap = new();

        foreach (var id in dupreplayIds)
        {
            if (dupReplays.ContainsKey(id))
            {
                replayHashMap[dupReplays[id].ReplayHash ?? ""] = id;
            }
            if (lsDupReplays.ContainsKey(id))
            {
                foreach (var lastSpawnhash in lsDupReplays[id].LastSpawnHashes)
                {
                    lastSpawnHashMap[lastSpawnhash] = id;
                }
            }
        }
        return (replayHashMap, lastSpawnHashMap);
    }

    public async Task<(int, List<Replay>)> HandleDbDuplicates(List<Replay> replays,
                                         Dictionary<string, int> replayHashMap,
                                         Dictionary<string, int> lastSpawnHashMap)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Dictionary<int, Uploader> fakeUploaderDic = new();
        HashSet<int> delReplayIds = new();
        List<Replay> removeReplays = new();

        int dupCount = 0;
        foreach (var replay in replays)
        {
            var dupReplayIds = GetDuplicateReplayIds(replay, replayHashMap, lastSpawnHashMap);
            if (!dupReplayIds.Any())
            {
                continue;
            }

            Uploader? replayFakeUploader = null;
            if (replay.UploaderId > 0)
            {
                if (!fakeUploaderDic.ContainsKey(replay.UploaderId))
                {
                    replayFakeUploader = new Uploader() { UploaderId = replay.UploaderId };
                    // fakeUploaderDic[replay.UploaderId] = replayFakeUploader;
                }
                else
                {
                    replayFakeUploader = fakeUploaderDic[replay.UploaderId];
                }
            }

            var keepReplay = replay;
            foreach (var dupReplayId in dupReplayIds)
            {
                var dupReplay = await context.Replays
                    .Include(i => i.Uploaders)
                    .Include(i => i.ReplayPlayers)
                        .ThenInclude(i => i.Player)
                    .FirstAsync(f => f.ReplayId == dupReplayId);

                if (!DuplicateIsPlausible(replay, dupReplay))
                {
                    logger.LogWarning($"duplicate not plausible: {replay.GameTime} <=> {dupReplay.GameTime} - {replay.ReplayHash}");
                    // continue;
                }

                if (dupReplay.Duration >= keepReplay.Duration)
                {
                    if (keepReplay.ReplayId == 0) // keepReplay == replay
                    {
                        removeReplays.Add(keepReplay);
                    }
                    else
                    {
                        delReplayIds.Add(keepReplay.ReplayId);
                    }
                    keepReplay = dupReplay;
                    keepReplay.ReplayPlayers = SyncReplayPlayers(keepReplay.ReplayPlayers.ToList(), replay.ReplayPlayers.ToList());
                    SyncUploaders(keepReplay, replay, replayFakeUploader);
                }
                else
                {
                    delReplayIds.Add(dupReplay.ReplayId);
                    keepReplay.ReplayPlayers = SyncReplayPlayers(keepReplay.ReplayPlayers.ToList(), dupReplay.ReplayPlayers.ToList());
                    SyncUploaders(keepReplay, dupReplay, replayFakeUploader);
                }
                dupCount++;
                await AttachUploaders(context, keepReplay, fakeUploaderDic);
            }
        }

        await context.SaveChangesAsync();
        await DeleteReplays(context, delReplayIds);

        return (delReplayIds.Count, replays.Except(removeReplays).ToList());
    }

    private static void SyncUploaders(Replay keepReplay, Replay dupReplay, Uploader? replayFakeUploader)
    {
        if (replayFakeUploader != null && !keepReplay.Uploaders.Any(a => a.UploaderId == replayFakeUploader.UploaderId))
        {
            keepReplay.Uploaders.Add(replayFakeUploader);
        }

        foreach (var dupUploader in dupReplay.Uploaders)
        {
            var keepUploader = keepReplay.Uploaders.FirstOrDefault(f => f.UploaderId == dupUploader.UploaderId);
            if (keepUploader == null)
            {
                keepReplay.Uploaders.Add(dupUploader);
            }
        }
    }

    private static HashSet<int> GetDuplicateReplayIds(Replay replay,
                                         Dictionary<string, int> replayHashMap,
                                         Dictionary<string, int> lastSpawnHashMap)
    {
        HashSet<int> dupReplayIds = new();

        if (replayHashMap.ContainsKey(replay.ReplayHash))
        {
            dupReplayIds.Add(replayHashMap[replay.ReplayHash]);
        }

        foreach (var lsHash in replay.ReplayPlayers.Where(x => x.LastSpawnHash != null).Select(s => s.LastSpawnHash ?? ""))
        {
            if (lastSpawnHashMap.ContainsKey(lsHash))
            {
                dupReplayIds.Add(lastSpawnHashMap[lsHash]);
            }
        }
        return dupReplayIds;
    }

    private static async Task DeleteReplays(ReplayContext context, HashSet<int> delReplayIds)
    {
        if (!delReplayIds.Any())
        {
            return;
        }
        foreach (var delReplayId in delReplayIds)
        {
            var delReplay = await context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Upgrades)
                .FirstAsync(f => f.ReplayId == delReplayId);
            context.Replays.Remove(delReplay);
        }
        await context.SaveChangesAsync();
    }

    private static List<ReplayPlayer> SyncReplayPlayers(List<ReplayPlayer> keepReplayPlayers, List<ReplayPlayer> replayPlayers)
    {
        List<ReplayPlayer> syncedReplayPlayers = new();

        foreach (var keepReplayPlayer in keepReplayPlayers)
        {
            var replayPlayer = replayPlayers.FirstOrDefault(f => f.PlayerId == keepReplayPlayer.PlayerId);
            if (replayPlayer == null)
            {
                return new();
            }

            bool isUploader = replayPlayer.IsUploader || keepReplayPlayer.IsUploader;

            if (replayPlayer.Duration > keepReplayPlayer.Duration)
            {
                replayPlayer.IsUploader = isUploader;
                syncedReplayPlayers.Add(replayPlayer);
            }
            else
            {
                keepReplayPlayer.IsUploader = isUploader;
                syncedReplayPlayers.Add(keepReplayPlayer);
            }
        }
        return syncedReplayPlayers;
    }

    private bool DuplicateIsPlausible(Replay replay, Replay dupReplay)
    {
        if ((dupReplay.GameTime - replay.GameTime).Duration() > TimeSpan.FromDays(3))
        {
            logger.LogWarning($"false positive duplicate? {replay.GameTime} <=> {dupReplay.GameTime} : {dupReplay.ReplayHash}");
            return false;
        }
        return true;
    }

    private static void AdjustImportValues(Replay replay)
    {
        if (replay.Middle.Length > 4000)
        {
            replay.Middle = replay.Middle[..3999];
            var middles = replay.Middle.Split('|', StringSplitOptions.RemoveEmptyEntries).SkipLast(1);
            replay.Middle = string.Join('|', middles);
        }

        bool isComputerGame = false;
        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            replayPlayer.ReplayPlayerId = 0;
            replayPlayer.LastSpawnHash = replayPlayer.Spawns
                .FirstOrDefault(f => f.Breakpoint == shared.Breakpoint.All)?
                .GenHash(replay);

            foreach (var spawnUnit in replayPlayer.Spawns.SelectMany(s => s.Units))
            {
                if (spawnUnit.Poss.Length > 3999)
                {
                    spawnUnit.Poss = spawnUnit.Poss[..3999];
                    var poss = spawnUnit.Poss.Split(',', StringSplitOptions.RemoveEmptyEntries).SkipLast(1);
                    if (poss.Count() % 2 != 0)
                    {
                        poss = poss.SkipLast(1);
                    }
                    spawnUnit.Poss = string.Join(',', poss);
                }
            }

            if (replayPlayer.Name.StartsWith("Computer "))
            {
                isComputerGame = true;
            }
        }

        if (isComputerGame)
        {
            replay.GameMode = shared.GameMode.Tutorial;
        }
    }
}

internal record DbDupHelper
{
    public int ReplayId { get; set; }
    public string? ReplayHash { get; set; }
    public string? LastSpawnHash { get; set; }
    public List<string> LastSpawnHashes { get; set; } = new();
}