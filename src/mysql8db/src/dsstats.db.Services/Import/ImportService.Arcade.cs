using System.Diagnostics;
using dsstats.db.Services.Ratings;
using dsstats.shared;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.db.Services.Import;

public partial class ImportService
{
    public async Task ImportArcadeReplays(List<ArcadeReplayDto> replayDtos)
    {
        if (!IsInit)
        {
            await Init();
        }

        Dictionary<PlayerId, string> playerInfos = [];

        for (int i = 0; i < replayDtos.Count; i++)
        {
            foreach (var rp in replayDtos[i].ArcadeReplayDsPlayers)
            {
                var playerId = new PlayerId(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId);
                if (!playerInfos.ContainsKey(playerId))
                {
                    playerInfos[playerId] = rp.Name;
                }
            }
        }

        var playerIds = await GetPlayerIds(playerInfos
            .Select(s => new RequestNames(s.Value, s.Key.ToonId, s.Key.RegionId, s.Key.RealmId))
            .ToList());

        var importTime = DateTime.UtcNow;
        var replays = replayDtos.Select(s => new ArcadeReplay()
        {
            RegionId = s.RegionId,
            BnetBucketId = s.BnetBucketId,
            BnetRecordId = s.BnetRecordId,
            GameMode = s.GameMode,
            CreatedAt = s.CreatedAt,
            Duration = s.Duration,
            PlayerCount = 6,
            WinnerTeam = s.WinnerTeam,
            Imported = importTime,
            ArcadeReplayPlayers = s.ArcadeReplayDsPlayers.Select(t => new ArcadeReplayPlayer()
            {
                Name = t.Name,
                SlotNumber = t.SlotNumber,
                Team = t.Team,
                Discriminator = t.Discriminator,
                PlayerResult = t.PlayerResult,
                Player = null,
                PlayerId = playerIds[new PlayerId(t.Player.ToonId, t.Player.RealmId, t.Player.RegionId)]
            }).ToList()
        });

        using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        await context.BulkInsertAsync(replays, new BulkConfig
        {
            IncludeGraph = true,
            SetOutputIdentity = true,
        });
        // await context.ArcadeReplays.AddRangeAsync(replays);
        // await context.SaveChangesAsync();
    }

    public async Task CombineDsstatsSc2ArcadeReplays(bool add = true)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var data = new ArcadeCombineData();
        HashSet<int> matchedArcadeIds = [];
        data.MatchesInfo = await GetProcessedReplayIds();

        int matches = 0;
        List<ReplayArcadeMatch> replayMatches = [];
        var fromDate = data.MatchesInfo.LatestUpdate == DateTime.MinValue ? new DateTime(2021, 2, 1)
            : data.MatchesInfo.LatestUpdate.AddDays(-2);
        int skip = 0;
        int take = 5000;

        using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();


        while (true)
        {
            var dsstatsReplays = await context.Replays
                .Where(x => x.PlayerCount == 6 && x.Duration >= 300 && x.WinnerTeam > 0)
                .Where(x => x.GameTime >= fromDate)
                .OrderBy(r => r.GameTime)
                    .ThenBy(t => t.ReplayId)
                .Select(s => new ReplayCalcDto()
                {
                    ReplayId = s.ReplayId,
                    GameTime = s.GameTime,
                    GameMode = s.GameMode,
                    WinnerTeam = s.WinnerTeam,
                    Duration = s.Duration,
                    IsTE = s.IsTE,
                    ReplayPlayers = s.ReplayPlayers.Select(t => new ReplayPlayerCalcDto()
                    {
                        ReplayPlayerId = t.ReplayPlayerId,
                        GamePos = t.GamePos,
                        PlayerResult = t.PlayerResult,
                        IsLeaver = t.Duration < s.Duration - 90,
                        IsMvp = t.Kills == s.Maxkillsum,
                        Team = t.Team,
                        Race = t.Race,
                        PlayerId = t.PlayerId,
                        IsUploader = t.IsUploader,
                    }).ToList()
                })
                .Skip(skip)
                .Take(take)
                .AsSplitQuery()
                .ToListAsync();

            if (dsstatsReplays.Count == 0)
            {
                break;
            }

            dsstatsReplays = dsstatsReplays.Where(x => !data.MatchesInfo.ReplayDict.ContainsKey(x.ReplayId))
                .ToList();

            await InitArcadeRep(dsstatsReplays, data, context);

            foreach (var dsstatsReplay in dsstatsReplays)
            {
                var arcadeReplay = await FindSc2ArcadeReplay(dsstatsReplay, matchedArcadeIds, data, context);
                if (arcadeReplay is not null)
                {
                    data.CurrentArcadeCalcDtos.Remove(arcadeReplay);
                    matchedArcadeIds.Add(arcadeReplay.ReplayId);
                    replayMatches.Add(new()
                    {
                        ReplayId = dsstatsReplay.ReplayId,
                        ArcadeReplayId = arcadeReplay.ReplayId,
                        MatchTime = DateTime.UtcNow
                    });
                    matches++;
                }
            }
            skip += take;

            logger.LogInformation("Porecessed: {count}/{matches}", skip, replayMatches.Count);
        }
        await StoreReplayMatches(replayMatches);
    }

    private async Task StoreReplayMatches(List<ReplayArcadeMatch> replayMatches)
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        if (replayMatches.Count == 0)
        {
            return;
        }

        context.ReplayArcadeMatches.AddRange(replayMatches);
        await context.SaveChangesAsync();
    }

    private async Task<ReplayCalcDto?> FindSc2ArcadeReplay(ReplayCalcDto dsstatsReplay,
                                                           HashSet<int> matchedArcadeIds,
                                                           ArcadeCombineData data,
                                                           DsstatsContext context)
    {
        var reasonableReplays = await GetReasonableReplays(dsstatsReplay, matchedArcadeIds, data, context);

        if (reasonableReplays.Count == 0)
        {
            return null;
        }

        var dsstatsPlayerIds = dsstatsReplay.ReplayPlayers.Select(s => s.PlayerId).OrderBy(o => o).ToList();
        ReplayCalcDto? bestMatch = null;
        int minBestScore = dsstatsPlayerIds.Count;
        int bestMatchScore = 0;

        foreach (var arcadeReplay in reasonableReplays)
        {
            var arcadePlayerIds = arcadeReplay.ReplayPlayers.Select(s => s.PlayerId).OrderBy(o => o).ToList();
            var key = new ReplayKey(arcadeReplay.ReplayId, arcadeReplay.IsArcade);
            int lcsLength = CalculateLCSLength(dsstatsPlayerIds, arcadePlayerIds);

            if (lcsLength > bestMatchScore)
            {
                bestMatchScore = lcsLength;
                bestMatch = arcadeReplay;
            }
        }
        return bestMatchScore >= minBestScore ? bestMatch : null;
    }

    private static int CalculateLCSLength(List<int> a, List<int> b)
    {
        int[,] dp = new int[a.Count + 1, b.Count + 1];

        for (int i = 1; i <= a.Count; i++)
        {
            for (int j = 1; j <= b.Count; j++)
            {
                if (a[i - 1] == b[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }

        return dp[a.Count, b.Count];
    }

    private async Task<List<ReplayCalcDto>> GetReasonableReplays(ReplayCalcDto dsstatsReplay,
                                                                 HashSet<int> matchedArcadeIds,
                                                                 ArcadeCombineData data,
                                                                 DsstatsContext context)
    {
        await UpdateArcadeReplays(dsstatsReplay, data, context);
        return data.CurrentArcadeCalcDtos
            .Where(x => x.GameTime > dsstatsReplay.GameTime.AddDays(-dateOverflow)
                && x.GameTime < dsstatsReplay.GameTime.AddDays(dateOverflow)
                && x.GameMode == dsstatsReplay.GameMode
                && !matchedArcadeIds.Contains(x.ReplayId)
            ).ToList();
    }

    private async Task UpdateArcadeReplays(ReplayCalcDto dsstasReplay, ArcadeCombineData data, DsstatsContext context)
    {
        var currentChunkInfo = data.ChunkInfos[data.CurrentChunkInfoIndex];
        if (dsstasReplay.GameTime > currentChunkInfo.EndTime.AddDays(-0.5))
        {
            if (data.ChunkInfos.Count > data.CurrentChunkInfoIndex + 1)
            {
                data.CurrentChunkInfoIndex++;
                currentChunkInfo = data.ChunkInfos[data.CurrentChunkInfoIndex];
                await LoadCurrentChunkInfoArcadeReplays(data, context);
            }
        }
    }

    private async Task<ReplayMatchInfo> GetProcessedReplayIds()
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var matches = await context.ReplayArcadeMatches
            .ToListAsync();

        return new()
        {
            ReplayDict = matches.ToDictionary(k => k.ReplayId, v => true),
            ArcadeDict = matches.ToDictionary(k => k.ArcadeReplayId, v => true),
            LatestUpdate = matches.OrderByDescending(o => o.MatchTime).FirstOrDefault()?.MatchTime ?? DateTime.MinValue,
        };
    }
}

internal record ReplayMatchInfo
{
    public Dictionary<int, bool> ReplayDict { get; set; } = [];
    public Dictionary<int, bool> ArcadeDict { get; set; } = [];
    public DateTime LatestUpdate { get; set; }
}

internal record struct ReplayKey(int ReplayId, bool IsArcade);
