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
    public const int CurrentDetectionVersion = 2;
    public const int DefaultBatchSize = 500;
    private readonly SemaphoreSlim processingLock = new(1, 1);

    public bool IsRunning => processingLock.CurrentCount == 0;

    public bool TryStartFullRun(int batchSize = DefaultBatchSize)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be greater than zero.");
        }

        if (!processingLock.Wait(0))
        {
            logger.LogWarning("Build detail generation is already running - ignoring full run trigger.");
            return false;
        }

        _ = RunFullRunAsync(batchSize, CancellationToken.None);
        return true;
    }

    public async Task<BuildDetailGenerationResult> ProcessPendingBatchAsync(
        int batchSize = DefaultBatchSize,
        CancellationToken token = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be greater than zero.");
        }

        await processingLock.WaitAsync(token);
        try
        {
            return await ProcessPendingBatchCoreAsync(batchSize, token);
        }
        finally
        {
            processingLock.Release();
        }
    }

    private async Task RunFullRunAsync(int batchSize, CancellationToken token)
    {
        var totalCandidates = 0;
        var totalDetected = 0;
        var totalNotDetectable = 0;
        var totalFailed = 0;

        try
        {
            logger.LogInformation("Full build detail generation started.");

            while (true)
            {
                var result = await ProcessPendingBatchCoreAsync(batchSize, token);
                if (result.Candidates == 0)
                {
                    break;
                }

                totalCandidates += result.Candidates;
                totalDetected += result.Detected;
                totalNotDetectable += result.NotDetectable;
                totalFailed += result.Failed;

                logger.LogInformation(
                    "Generated replay build detail batch: {Detected} detected, {NotDetectable} not detectable, {Failed} failed from {Candidates} candidates.",
                    result.Detected,
                    result.NotDetectable,
                    result.Failed,
                    result.Candidates);

                if (result.Detected + result.NotDetectable == 0)
                {
                    logger.LogWarning(
                        "Full build detail generation stopped because the latest batch made no progress. Failed candidates will remain pending for a later retry.");
                    break;
                }
            }

            logger.LogInformation(
                "Full build detail generation completed: {Detected} detected, {NotDetectable} not detectable, {Failed} failed from {Candidates} candidates.",
                totalDetected,
                totalNotDetectable,
                totalFailed,
                totalCandidates);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            logger.LogInformation(
                "Full build detail generation was cancelled: {Detected} detected, {NotDetectable} not detectable, {Failed} failed from {Candidates} candidates.",
                totalDetected,
                totalNotDetectable,
                totalFailed,
                totalCandidates);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Full build detail generation failed: {Detected} detected, {NotDetectable} not detectable, {Failed} failed from {Candidates} candidates.",
                totalDetected,
                totalNotDetectable,
                totalFailed,
                totalCandidates);
        }
        finally
        {
            processingLock.Release();
        }
    }

    private async Task<BuildDetailGenerationResult> ProcessPendingBatchCoreAsync(
        int batchSize,
        CancellationToken token)
    {
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

        var playersByReplay = BuildPlayersByReplay(playerRows, replayRows.Count);
        var spawnsByPlayer = BuildSpawnsByPlayer(spawnRows, playerRows.Count);
        var unitsBySpawn = BuildUnitsBySpawn(unitRows, spawnRows.Count);

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
            await using var transaction = await context.Database.BeginTransactionAsync(token);
            var detailReplayIds = ToReplayIdArray(details);

            await context.ReplayBuildDetails
                .Where(x => detailReplayIds.Contains(x.ReplayId)
                    && x.DetectionVersion != CurrentDetectionVersion)
                .ExecuteDeleteAsync(token);

            var currentReplayIds = await context.ReplayBuildDetails
                .AsNoTracking()
                .Where(x => detailReplayIds.Contains(x.ReplayId))
                .Select(x => x.ReplayId)
                .ToListAsync(token);

            if (currentReplayIds.Count > 0)
            {
                var currentReplayIdSet = currentReplayIds.ToHashSet();
                details.RemoveAll(x => currentReplayIdSet.Contains(x.ReplayId));
            }

            var autoDetectChanges = context.ChangeTracker.AutoDetectChangesEnabled;
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                if (details.Count > 0)
                {
                    context.ReplayBuildDetails.AddRange(details);
                    await context.SaveChangesAsync(token);
                }

                await transaction.CommitAsync(token);
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
                && !context.ReplayBuildDetails.Any(d =>
                    d.ReplayId == r.ReplayId
                    && d.DetectionVersion == CurrentDetectionVersion))
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
            .OrderBy(p => p.ReplayId)
            .ThenBy(p => p.GamePos)
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
                p.Result,
                p.Refineries))
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
            .OrderBy(s => s.ReplayPlayerId)
            .ThenBy(s => s.SpawnId)
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
            .OrderBy(su => su.SpawnId)
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
            Players = CreatePlayerDtos(players, spawnsByPlayer, unitsBySpawn)
        };
    }

    private static Dictionary<int, List<CandidatePlayerRow>> BuildPlayersByReplay(
        List<CandidatePlayerRow> playerRows,
        int replayCount)
    {
        var playersByReplay = new Dictionary<int, List<CandidatePlayerRow>>(replayCount);
        foreach (var player in playerRows)
        {
            if (!playersByReplay.TryGetValue(player.ReplayId, out var players))
            {
                players = new List<CandidatePlayerRow>(6);
                playersByReplay.Add(player.ReplayId, players);
            }

            players.Add(player);
        }

        return playersByReplay;
    }

    private static Dictionary<int, Min5SpawnRow> BuildSpawnsByPlayer(
        List<Min5SpawnRow> spawnRows,
        int playerCount)
    {
        var spawnsByPlayer = new Dictionary<int, Min5SpawnRow>(playerCount);
        foreach (var spawn in spawnRows)
        {
            spawnsByPlayer.TryAdd(spawn.ReplayPlayerId, spawn);
        }

        return spawnsByPlayer;
    }

    private static Dictionary<int, List<UnitDto>> BuildUnitsBySpawn(
        List<SpawnUnitRow> unitRows,
        int spawnCount)
    {
        var unitsBySpawn = new Dictionary<int, List<UnitDto>>(spawnCount);
        foreach (var unit in unitRows)
        {
            if (!unitsBySpawn.TryGetValue(unit.SpawnId, out var units))
            {
                units = [];
                unitsBySpawn.Add(unit.SpawnId, units);
            }

            units.Add(new UnitDto
            {
                Name = unit.UnitName,
                Count = unit.Count
            });
        }

        return unitsBySpawn;
    }

    private static int[] ToReplayIdArray(List<ReplayBuildDetail> details)
    {
        var replayIds = new int[details.Count];
        for (var i = 0; i < details.Count; i++)
        {
            replayIds[i] = details[i].ReplayId;
        }

        return replayIds;
    }

    private static List<ReplayPlayerDto> CreatePlayerDtos(
        List<CandidatePlayerRow> players,
        Dictionary<int, Min5SpawnRow> spawnsByPlayer,
        Dictionary<int, List<UnitDto>> unitsBySpawn)
    {
        var playerDtos = new List<ReplayPlayerDto>(players.Count);
        foreach (var player in players)
        {
            playerDtos.Add(CreatePlayerDto(player, spawnsByPlayer, unitsBySpawn));
        }

        return playerDtos;
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
            Refineries = player.Refineries.ToList(),
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
        var replayPlayerByGamePos = new Dictionary<int, CandidatePlayerRow>(replayPlayers.Count);
        foreach (var replayPlayer in replayPlayers)
        {
            replayPlayerByGamePos.Add(replayPlayer.GamePos, replayPlayer);
        }

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
        PlayerResult Result,
        int[] Refineries);

    private sealed record Min5SpawnRow(int SpawnId, int ReplayPlayerId, int GasCount);

    private sealed record SpawnUnitRow(int SpawnId, string UnitName, int Count);
}

public sealed record BuildDetailGenerationResult(
    int Candidates,
    int Detected,
    int NotDetectable,
    int Failed);
