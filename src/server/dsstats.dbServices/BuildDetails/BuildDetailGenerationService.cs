using dsstats.db;
using dsstats.shared;
using dsstats.shared.DetailBuild;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices.BuildDetails;

public sealed class BuildDetailGenerationService(
    IDbContextFactory<DsstatsContext> contextFactory,
    ILogger<BuildDetailGenerationService> logger)
{
    public const int CurrentDetectionVersion = 1;
    public const int DefaultBatchSize = 500;

    public async Task<BuildDetailGenerationResult> ProcessPendingBatchAsync(
        int batchSize = DefaultBatchSize,
        CancellationToken token = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be greater than zero.");
        }

        await using var context = await contextFactory.CreateDbContextAsync(token);
        var replayRows = await GetCandidateReplays(context, batchSize, token);
        if (replayRows.Count == 0)
        {
            return new BuildDetailGenerationResult(0, 0, 0, 0);
        }

        var replayIds = replayRows.Select(x => x.ReplayId).ToArray();
        var playerRows = await GetCandidatePlayers(context, replayIds, token);
        var playerIds = playerRows.Select(x => x.ReplayPlayerId).ToArray();
        var spawnRows = await GetMin5Spawns(context, playerIds, token);
        var spawnIds = spawnRows.Select(x => x.SpawnId).ToArray();
        var unitRows = await GetSpawnUnits(context, spawnIds, token);

        var playersByReplay = playerRows
            .GroupBy(x => x.ReplayId)
            .ToDictionary(x => x.Key, x => x.OrderBy(p => p.GamePos).ToList());
        var spawnsByPlayer = spawnRows
            .GroupBy(x => x.ReplayPlayerId)
            .ToDictionary(x => x.Key, x => x.OrderBy(s => s.SpawnId).First());
        var unitsBySpawn = unitRows
            .GroupBy(x => x.SpawnId)
            .ToDictionary(x => x.Key, x => x.Select(u => new UnitDto
            {
                Name = u.UnitName,
                Count = u.Count
            }).ToList());

        var createdAt = DateTime.UtcNow;
        var details = new List<ReplayBuildDetail>(replayRows.Count);
        var detected = 0;
        var notDetectable = 0;
        var failed = 0;

        foreach (var replayRow in replayRows)
        {
            try
            {
                if (!playersByReplay.TryGetValue(replayRow.ReplayId, out var replayPlayers))
                {
                    details.Add(CreateNotDetectable(replayRow.ReplayId, createdAt, "Replay has no players."));
                    notDetectable++;
                    continue;
                }

                var replayDto = CreateReplayDto(replayRow, replayPlayers, spawnsByPlayer, unitsBySpawn);
                var detectedDetails = DetailBuilds.DetectStandardBuild(replayDto);
                if (detectedDetails is null)
                {
                    details.Add(CreateNotDetectable(replayRow.ReplayId, createdAt, "Standard build details could not be detected."));
                    notDetectable++;
                    continue;
                }

                details.Add(CreateDetected(replayRow.ReplayId, replayPlayers, detectedDetails, createdAt));
                detected++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                logger.LogError(ex, "Failed to generate build details for replay {ReplayId}. The replay will be retried later.", replayRow.ReplayId);
            }
        }

        if (details.Count > 0)
        {
            var detailReplayIds = details.Select(x => x.ReplayId).ToArray();
            var existingReplayIds = await context.ReplayBuildDetails
                .AsNoTracking()
                .Where(x => detailReplayIds.Contains(x.ReplayId))
                .Select(x => x.ReplayId)
                .ToListAsync(token);

            if (existingReplayIds.Count > 0)
            {
                var existingReplayIdSet = existingReplayIds.ToHashSet();
                details.RemoveAll(x => existingReplayIdSet.Contains(x.ReplayId));
            }
        }

        if (details.Count > 0)
        {
            var autoDetectChanges = context.ChangeTracker.AutoDetectChangesEnabled;
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                context.ReplayBuildDetails.AddRange(details);
                await context.SaveChangesAsync(token);
            }
            finally
            {
                context.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
            }
        }

        return new BuildDetailGenerationResult(replayRows.Count, detected, notDetectable, failed);
    }

    private static Task<List<CandidateReplayRow>> GetCandidateReplays(
        DsstatsContext context,
        int batchSize,
        CancellationToken token)
    {
        return context.Replays
            .AsNoTracking()
            .Where(r => r.GameMode == GameMode.Standard
                && r.WinnerTeam > 0
                && r.PlayerCount == 6
                && !context.ReplayBuildDetails.Any(d => d.ReplayId == r.ReplayId))
            .OrderBy(r => r.ReplayId)
            .Select(r => new CandidateReplayRow(
                r.ReplayId,
                r.Title,
                r.Version,
                r.GameMode,
                r.RegionId,
                r.Gametime,
                r.BaseBuild,
                r.Duration,
                r.Cannon,
                r.Bunker,
                r.WinnerTeam))
            .Take(batchSize)
            .ToListAsync(token);
    }

    private static Task<List<CandidatePlayerRow>> GetCandidatePlayers(
        DsstatsContext context,
        int[] replayIds,
        CancellationToken token)
    {
        return context.ReplayPlayers
            .AsNoTracking()
            .Where(p => replayIds.Contains(p.ReplayId))
            .Select(p => new CandidatePlayerRow(
                p.ReplayPlayerId,
                p.ReplayId,
                p.PlayerId,
                p.Name,
                p.Race,
                p.SelectedRace,
                p.TeamId,
                p.GamePos,
                p.Duration,
                p.Result))
            .ToListAsync(token);
    }

    private static Task<List<Min5SpawnRow>> GetMin5Spawns(
        DsstatsContext context,
        int[] replayPlayerIds,
        CancellationToken token)
    {
        return context.Spawns
            .AsNoTracking()
            .Where(s => replayPlayerIds.Contains(s.ReplayPlayerId)
                && s.Breakpoint == Breakpoint.Min5)
            .Select(s => new Min5SpawnRow(s.SpawnId, s.ReplayPlayerId, s.GasCount))
            .ToListAsync(token);
    }

    private static Task<List<SpawnUnitRow>> GetSpawnUnits(
        DsstatsContext context,
        int[] spawnIds,
        CancellationToken token)
    {
        return context.SpawnUnits
            .AsNoTracking()
            .Where(su => spawnIds.Contains(su.SpawnId))
            .Select(su => new SpawnUnitRow(
                su.SpawnId,
                su.Unit!.Name,
                su.Count))
            .ToListAsync(token);
    }

    private static ReplayDto CreateReplayDto(
        CandidateReplayRow replay,
        List<CandidatePlayerRow> players,
        Dictionary<int, Min5SpawnRow> spawnsByPlayer,
        Dictionary<int, List<UnitDto>> unitsBySpawn)
    {
        return new ReplayDto
        {
            Title = replay.Title,
            Version = replay.Version,
            GameMode = replay.GameMode,
            RegionId = replay.RegionId,
            Gametime = replay.Gametime,
            BaseBuild = replay.BaseBuild,
            Duration = replay.Duration,
            Cannon = replay.Cannon,
            Bunker = replay.Bunker,
            WinnerTeam = replay.WinnerTeam,
            Players = players.Select(p => CreatePlayerDto(p, spawnsByPlayer, unitsBySpawn)).ToList()
        };
    }

    private static ReplayPlayerDto CreatePlayerDto(
        CandidatePlayerRow player,
        Dictionary<int, Min5SpawnRow> spawnsByPlayer,
        Dictionary<int, List<UnitDto>> unitsBySpawn)
    {
        var dto = new ReplayPlayerDto
        {
            Name = player.Name,
            Race = player.Race,
            SelectedRace = player.SelectedRace,
            TeamId = player.TeamId,
            GamePos = player.GamePos,
            Duration = player.Duration,
            Result = player.Result,
            Player = new()
            {
                PlayerId = player.PlayerId
            }
        };

        if (spawnsByPlayer.TryGetValue(player.ReplayPlayerId, out var spawn))
        {
            dto.Spawns.Add(new SpawnDto
            {
                Breakpoint = Breakpoint.Min5,
                GasCount = spawn.GasCount,
                Units = unitsBySpawn.TryGetValue(spawn.SpawnId, out var units) ? units : []
            });
        }

        return dto;
    }

    private static ReplayBuildDetail CreateNotDetectable(int replayId, DateTime createdAt, string failureReason)
    {
        return new()
        {
            ReplayId = replayId,
            DetectionVersion = CurrentDetectionVersion,
            Status = ReplayBuildDetailStatus.NotDetectable,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            FailureReason = failureReason
        };
    }

    private static ReplayBuildDetail CreateDetected(
        int replayId,
        List<CandidatePlayerRow> replayPlayers,
        ReplayBuildDetails details,
        DateTime createdAt)
    {
        var replayPlayerByGamePos = replayPlayers.ToDictionary(x => x.GamePos);
        var buildDetail = new ReplayBuildDetail
        {
            ReplayId = replayId,
            DetectionVersion = CurrentDetectionVersion,
            Status = ReplayBuildDetailStatus.Detected,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        foreach (var matchup in details.MatchupInfos)
        {
            buildDetail.PlayerBuilds.Add(CreatePlayerBuildDetail(
                matchup.Lane,
                matchup.P1,
                matchup.P2,
                matchup.P1Won,
                replayPlayerByGamePos));

            buildDetail.PlayerBuilds.Add(CreatePlayerBuildDetail(
                matchup.Lane,
                matchup.P2,
                matchup.P1,
                matchup.P2Won,
                replayPlayerByGamePos));
        }

        foreach (var teamBuild in details.TeamBuildInfos)
        {
            buildDetail.TeamBuilds.Add(new ReplayTeamBuildDetail
            {
                TeamId = teamBuild.TeamId,
                TeamBuild = teamBuild.TeamBuild,
                LeaderReplayPlayerId = replayPlayerByGamePos[teamBuild.LeaderGamePos].ReplayPlayerId,
                FollowerReplayPlayerId = replayPlayerByGamePos[teamBuild.FollowerGamePos].ReplayPlayerId
            });
        }

        return buildDetail;
    }

    private static ReplayPlayerBuildDetail CreatePlayerBuildDetail(
        int lane,
        PlayerBuildInfo player,
        PlayerBuildInfo opponent,
        bool won,
        Dictionary<int, CandidatePlayerRow> replayPlayerByGamePos)
    {
        var replayPlayer = replayPlayerByGamePos[player.GamePos];
        var opponentReplayPlayer = replayPlayerByGamePos[opponent.GamePos];

        return new()
        {
            ReplayPlayerId = replayPlayer.ReplayPlayerId,
            OppReplayPlayerId = opponentReplayPlayer.ReplayPlayerId,
            GamePos = player.GamePos,
            TeamId = replayPlayer.TeamId,
            Commander = player.Commander,
            Build = player.Build,
            GasFirst = player.GasFirst,
            Lane = lane,
            OppGamePos = opponent.GamePos,
            OppCommander = opponent.Commander,
            OppBuild = opponent.Build,
            OppGasFirst = opponent.GasFirst,
            Won = won
        };
    }

    private sealed record CandidateReplayRow(
        int ReplayId,
        string Title,
        string Version,
        GameMode GameMode,
        int RegionId,
        DateTime Gametime,
        int BaseBuild,
        int Duration,
        int Cannon,
        int Bunker,
        int WinnerTeam);

    private sealed record CandidatePlayerRow(
        int ReplayPlayerId,
        int ReplayId,
        int PlayerId,
        string Name,
        Commander Race,
        Commander SelectedRace,
        int TeamId,
        int GamePos,
        int Duration,
        PlayerResult Result);

    private sealed record Min5SpawnRow(int SpawnId, int ReplayPlayerId, int GasCount);

    private sealed record SpawnUnitRow(int SpawnId, string UnitName, int Count);
}

public sealed record BuildDetailGenerationResult(
    int Candidates,
    int Detected,
    int NotDetectable,
    int Failed);
