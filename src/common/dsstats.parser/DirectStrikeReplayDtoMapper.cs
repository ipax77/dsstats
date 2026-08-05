using System.Globalization;
using System.Text;
using System.Collections.ObjectModel;
using dsstats.shared.Units;
using s2protocol.NET;
using s2protocol.NET.Models;
using Sc2DirectStrike.Parser;
using DsstatsBreakpoint = dsstats.shared.Breakpoint;
using DsstatsCommander = dsstats.shared.Commander;
using DsstatsPlayerDto = dsstats.shared.PlayerDto;
using DsstatsReplayDto = dsstats.shared.ReplayDto;
using DsstatsReplayPlayerDto = dsstats.shared.ReplayPlayerDto;
using DsstatsReplayRules = dsstats.shared.ReplayRules;
using DsstatsSpawnDto = dsstats.shared.SpawnDto;
using DsstatsToonIdDto = dsstats.shared.ToonIdDto;
using DsstatsUnitDto = dsstats.shared.UnitDto;
using DsstatsUpgradeDto = dsstats.shared.UpgradeDto;

namespace dsstats.parser;

internal static class DirectStrikeReplayDtoMapper
{
    private const double GameLoopsPerSecond = 22.4D;
    private const int CompatHashGameloop = 6_720;
    private const double GameLoopsPerMinute = GameLoopsPerSecond * 60.0;

    private static readonly int[] RefineryCosts = [150, 225, 300, 375, 500];

    private static readonly BreakpointDefinition[] BreakpointDefinitions =
    [
        new(Breakpoint.Min5, CompatHashGameloop),
        new(Breakpoint.Min10, 13_440),
        new(Breakpoint.Min15, 20_160),
    ];

    internal static DsstatsReplayDto Map(Sc2Replay replay, DirectStrikeReplay directStrikeReplay)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(directStrikeReplay);

        Dictionary<DirectStrikePlayer, MessageCounts> messageCountsByPlayer = GetMessageCountsByPlayer(replay, directStrikeReplay);
        int compatHashGameloop = GetCompatHashGameloop(directStrikeReplay);
        List<DsstatsReplayPlayerDto> players = new(directStrikeReplay.Players.Count);
        foreach (DirectStrikePlayer player in directStrikeReplay.Players)
        {
            players.Add(CreatePlayerDto(player, messageCountsByPlayer.GetValueOrDefault(player), compatHashGameloop));
        }

        string title = replay.Details?.Title ?? replay.Metadata?.Title ?? string.Empty;
        string version = GetReplayVersion(replay);
        int regionId = GetRegionId(directStrikeReplay);
        int baseBuild = ParseBaseBuild(directStrikeReplay.BaseBuild, replay);

        DsstatsReplayDto result = new()
        {
            FileName = replay.FileName ?? string.Empty,
            CompatHash = CreateCompatHash(title, version, directStrikeReplay.GameMode, regionId, baseBuild, directStrikeReplay.Duration, players),
            Title = title,
            Version = version,
            GameMode = (dsstats.shared.GameMode)(int)directStrikeReplay.GameMode,
            RegionId = regionId,
            Gametime = directStrikeReplay.GameTime,
            BaseBuild = baseBuild,
            Duration = ToSeconds(directStrikeReplay.Duration),
            Cannon = ToSeconds(directStrikeReplay.CannonTime),
            Bunker = ToSeconds(directStrikeReplay.BunkerTime),
            WinnerTeam = directStrikeReplay.WinnerTeam,
            ResumedFromReplay = directStrikeReplay.ResumedFromReplay,
            MiddleChanges = CreateMiddleChanges(directStrikeReplay),
            Players = players,
        };
        SetMvp(result);
        return result;
    }

    private static int GetCompatHashGameloop(DirectStrikeReplay replay)
    {
        if (ToGameloop(replay.Duration) < CompatHashGameloop)
        {
            return 0;
        }

        int compatHashGameloop = CompatHashGameloop;
        foreach (DirectStrikePlayer player in replay.Players)
        {
            if (player.DurationGameloop > 0 && player.DurationGameloop < compatHashGameloop)
            {
                compatHashGameloop = player.DurationGameloop;
            }
        }

        return compatHashGameloop;
    }

    private static string CreateCompatHash(
        string title,
        string version,
        GameMode gameMode,
        int regionId,
        int baseBuild,
        TimeSpan duration,
        List<DsstatsReplayPlayerDto> players)
    {
        if (players.Count == 0 || ToGameloop(duration) < CompatHashGameloop)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        AppendString(builder, "ds-compat-v1");
        AppendString(builder, title);
        AppendString(builder, version);
        AppendInt(builder, (int)gameMode);
        AppendInt(builder, regionId);
        AppendInt(builder, baseBuild);
        AppendInt(builder, players.Count);

        foreach (DsstatsReplayPlayerDto player in players
            .OrderBy(static player => player.TeamId)
            .ThenBy(static player => player.GamePos)
            .ThenBy(static player => player.Player.ToonId.Region)
            .ThenBy(static player => player.Player.ToonId.Realm)
            .ThenBy(static player => player.Player.ToonId.Id)
            .ThenBy(static player => player.Player.PlayerId)
            .ThenBy(static player => player.Name, StringComparer.Ordinal))
        {
            AppendString(builder, player.CompatHash ?? string.Empty);
        }

        return builder.ToString();
    }

    private static string CreatePlayerCompatHash(
        DirectStrikePlayer player,
        DsstatsPlayerDto playerDto,
        DsstatsCommander selectedRace,
        DsstatsSpawnDto? snapshot)
    {
        StringBuilder builder = new();
        AppendString(builder, "ds-player-compat-v1");
        AppendInt(builder, player.TeamId);
        AppendInt(builder, player.GamePos);
        AppendInt(builder, (int)player.Commander);
        AppendInt(builder, (int)selectedRace);
        AppendInt(builder, playerDto.ToonId.Region);
        AppendInt(builder, playerDto.ToonId.Realm);
        AppendInt(builder, playerDto.ToonId.Id);
        AppendInt(builder, playerDto.PlayerId);
        AppendString(builder, player.Name);
        AppendString(builder, player.Clan ?? string.Empty);
        AppendInt(builder, snapshot is null ? 0 : (int)snapshot.Breakpoint);
        AppendInt(builder, snapshot?.Income ?? 0);
        AppendInt(builder, snapshot?.GasCount ?? 0);
        AppendInt(builder, snapshot?.ArmyValue ?? 0);
        AppendInt(builder, snapshot?.KilledValue ?? 0);
        AppendInt(builder, snapshot?.LostValue ?? 0);
        AppendInt(builder, snapshot?.UpgradeSpent ?? 0);
        AppendInt(builder, snapshot?.Units.Count ?? 0);

        if (snapshot is not null)
        {
            foreach (DsstatsUnitDto unit in snapshot.Units.OrderBy(static unit => unit.Name, StringComparer.Ordinal))
            {
                AppendString(builder, unit.Name);
                AppendInt(builder, unit.Count);
                AppendInt(builder, unit.Positions?.Count ?? 0);
                foreach (int position in unit.Positions ?? [])
                {
                    AppendInt(builder, position);
                }
            }
        }

        return builder.ToString();
    }

    private static void AppendInt(StringBuilder builder, int value)
    {
        builder.Append('i');
        builder.Append(value.ToString(CultureInfo.InvariantCulture));
        builder.Append('|');
    }

    private static void AppendString(StringBuilder builder, string value)
    {
        builder.Append('s');
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
        builder.Append('|');
    }

    private static DsstatsReplayPlayerDto CreatePlayerDto(
        DirectStrikePlayer player,
        MessageCounts messageCounts,
        int compatHashGameloop)
    {
        Dictionary<DirectStrikePlayerSpawn, int> armyValuesBySpawn = GetArmyValuesBySpawn(player);
        Dictionary<DirectStrikePlayerSpawn, int> incomesBySpawn = GetIncomesBySpawn(player);
        DsstatsSpawnDto? compatHashSnapshot =
            CreateCompatHashSnapshotDto(player, compatHashGameloop, armyValuesBySpawn, incomesBySpawn);
        DsstatsCommander selectedRace = ToCommander(player.SelectedRace);
        DsstatsCommander commander = (DsstatsCommander)(int)player.Commander;
        DsstatsPlayerDto playerDto = new()
        {
            PlayerId = player.Id,
            Name = player.Name,
            ToonId = new()
            {
                Region = player.Region,
                Realm = player.Realm,
                Id = player.Id,
            },
        };

        return new()
        {
            CompatHash = CreatePlayerCompatHash(player, playerDto, selectedRace, compatHashSnapshot),
            Name = player.Name,
            Clan = player.Clan,
            Race = commander,
            SelectedRace = selectedRace,
            TeamId = player.TeamId,
            GamePos = player.GamePos,
            Result = ToPlayerResult(player.Result),
            Duration = ToSeconds(player.Duration),
            Apm = (int)Math.Round(player.APM),
            Messages = messageCounts.Messages,
            Pings = messageCounts.Pings,
            IsMvp = false,
            ScanCount = player.ScanCount,
            Spawns = CreateSpawnDtos(player, armyValuesBySpawn, incomesBySpawn),
            Upgrades = CreateUpgradeDtos(player),
            TierUpgrades = ToSeconds(player.TierUpgrades),
            Refineries = ToSeconds(player.RefineryTimes),
            Player = playerDto,
        };
    }

    private static int GetRegionId(DirectStrikeReplay directStrikeReplay)
    {
        foreach (DirectStrikePlayer player in directStrikeReplay.Players)
        {
            if (player.Region != 0)
            {
                return player.Region;
            }
        }

        return 0;
    }

    private static List<DsstatsSpawnDto> CreateSpawnDtos(
        DirectStrikePlayer player,
        IReadOnlyDictionary<DirectStrikePlayerSpawn, int> armyValuesBySpawn,
        IReadOnlyDictionary<DirectStrikePlayerSpawn, int> incomesBySpawn)
    {
        List<DirectStrikePlayerSpawn> statsBackedSpawns = GetStatsBackedSpawns(player);
        if (statsBackedSpawns.Count == 0)
        {
            return [];
        }

        List<DsstatsSpawnDto> spawns = new(BreakpointDefinitions.Length + 1);
        IReadOnlyCollection<BuildUnitModificationCountDto>[] modificationCounts =
            CreateBuildUnitModificationCounts(player);
        foreach (BreakpointDefinition breakpoint in BreakpointDefinitions)
        {
            if (player.DurationGameloop > 0 && breakpoint.Gameloop > player.DurationGameloop)
            {
                continue;
            }

            DirectStrikePlayerSpawn spawn = FindClosestBreakpointSpawn(statsBackedSpawns, breakpoint.Gameloop);
            spawns.Add(CreateSpawnDto(
                breakpoint.Breakpoint,
                spawn,
                player,
                armyValuesBySpawn,
                incomesBySpawn,
                buildUnitModifications: modificationCounts[(int)breakpoint.Breakpoint]));
        }

        DirectStrikePlayerSpawn finalSpawn = statsBackedSpawns[^1];
        DirectStrikePlayerStats? finalStats = GetFinalPositiveIncomeStats(player);
        spawns.Add(CreateSpawnDto(
            Breakpoint.All,
            finalSpawn,
            player,
            armyValuesBySpawn,
            incomesBySpawn,
            finalStats,
            modificationCounts[(int)Breakpoint.All]));
        return spawns;
    }

    private static DsstatsSpawnDto? CreateCompatHashSnapshotDto(
        DirectStrikePlayer player,
        int compatHashGameloop,
        IReadOnlyDictionary<DirectStrikePlayerSpawn, int> armyValuesBySpawn,
        IReadOnlyDictionary<DirectStrikePlayerSpawn, int> incomesBySpawn)
    {
        if (compatHashGameloop <= 0)
        {
            return null;
        }

        List<DirectStrikePlayerSpawn> statsBackedSpawns = new(player.Spawns.Count);
        foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
        {
            if (spawn.SummaryStats is { MineralsCollectionRate: > 0 } stats && stats.Gameloop <= compatHashGameloop)
            {
                statsBackedSpawns.Add(spawn);
            }
        }

        if (statsBackedSpawns.Count == 0)
        {
            return null;
        }

        DirectStrikePlayerSpawn compatHashSpawn = FindClosestBreakpointSpawn(statsBackedSpawns, compatHashGameloop);
        return CreateSpawnDto(Breakpoint.Min5, compatHashSpawn, player, armyValuesBySpawn, incomesBySpawn);
    }

    private static List<DirectStrikePlayerSpawn> GetStatsBackedSpawns(DirectStrikePlayer player)
    {
        List<DirectStrikePlayerSpawn> statsBackedSpawns = new(player.Spawns.Count);
        foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
        {
            if (spawn.SummaryStats is { MineralsCollectionRate: > 0 })
            {
                statsBackedSpawns.Add(spawn);
            }
        }

        return statsBackedSpawns;
    }

    private static DirectStrikePlayerStats? GetFinalPositiveIncomeStats(DirectStrikePlayer player)
    {
        for (int i = player.Stats.Count - 1; i >= 0; i--)
        {
            DirectStrikePlayerStats stats = player.Stats[i];
            if (stats.MineralsCollectionRate > 0)
            {
                return stats;
            }
        }

        return null;
    }

    private static DirectStrikePlayerSpawn FindClosestBreakpointSpawn(List<DirectStrikePlayerSpawn> spawns, int targetGameloop)
    {
        DirectStrikePlayerSpawn bestSpawn = spawns[0];
        int bestDistance = Math.Abs(bestSpawn.EndGameloop - targetGameloop);

        for (int i = 1; i < spawns.Count; i++)
        {
            DirectStrikePlayerSpawn spawn = spawns[i];
            int distance = Math.Abs(spawn.EndGameloop - targetGameloop);
            if (distance < bestDistance || (distance == bestDistance && spawn.EndGameloop < bestSpawn.EndGameloop))
            {
                bestSpawn = spawn;
                bestDistance = distance;
            }
        }

        return bestSpawn;
    }

    private static List<DsstatsUpgradeDto> CreateUpgradeDtos(DirectStrikePlayer player)
    {
        List<KeyValuePair<string, TimeSpan>> upgrades = [.. player.Upgrades];

        upgrades.Sort(static (left, right) =>
        {
            int timeComparison = left.Value.CompareTo(right.Value);
            return timeComparison != 0 ? timeComparison : string.Compare(left.Key, right.Key, StringComparison.Ordinal);
        });

        List<DsstatsUpgradeDto> upgradeDtos = new(upgrades.Count);
        foreach (KeyValuePair<string, TimeSpan> upgrade in upgrades)
        {
            upgradeDtos.Add(new()
            {
                Name = upgrade.Key,
                Gameloop = ToSeconds(upgrade.Value),
            });
        }

        return upgradeDtos;
    }

    private static DsstatsSpawnDto CreateSpawnDto(
        Breakpoint breakpoint,
        DirectStrikePlayerSpawn spawn,
        DirectStrikePlayer player,
        IReadOnlyDictionary<DirectStrikePlayerSpawn, int> armyValuesBySpawn,
        IReadOnlyDictionary<DirectStrikePlayerSpawn, int> incomesBySpawn,
        DirectStrikePlayerStats? cumulativeStats = null,
        IReadOnlyCollection<BuildUnitModificationCountDto>? buildUnitModifications = null)
    {
        DirectStrikePlayerStats stats = spawn.SummaryStats
            ?? throw new InvalidOperationException("Breakpoint spawns must have summary stats.");
        cumulativeStats ??= stats;

        return new()
        {
            Breakpoint = (DsstatsBreakpoint)(int)breakpoint,
            Income = incomesBySpawn.GetValueOrDefault(spawn),
            GasCount = GetGasCount(player, stats.Time),
            ArmyValue = armyValuesBySpawn.GetValueOrDefault(spawn),
            KilledValue = cumulativeStats.MineralsKilledArmy,
            LostValue = cumulativeStats.MineralsLostArmy,
            UpgradeSpent = cumulativeStats.MineralsUsedCurrentTechnology,
            Units = CreateUnitDtos(
                spawn,
                buildUnitModifications ?? [],
                (DsstatsCommander)(int)player.Commander),
        };
    }

    private static IReadOnlyCollection<BuildUnitModificationCountDto>[] CreateBuildUnitModificationCounts(
        DirectStrikePlayer player)
    {
        IReadOnlyCollection<BuildUnitModificationCountDto>[] result =
            new IReadOnlyCollection<BuildUnitModificationCountDto>[(int)Breakpoint.All + 1];
        Array.Fill(result, BuildUnitModificationCollections.EmptyCounts);
        if (player.BuildUnitModifications.Count == 0)
        {
            return result;
        }

        HashSet<(BuildUnitModificationType Type, int TargetUnitTag)> seenTargets = [];
        Dictionary<(BuildUnitModificationType Type, string TargetUnitName), int> counts = [];
        int modificationIndex = 0;

        foreach (BreakpointDefinition breakpoint in BreakpointDefinitions)
        {
            AddBuildUnitModificationCountsUntil(
                player.BuildUnitModifications,
                breakpoint.Gameloop,
                seenTargets,
                counts,
                ref modificationIndex);
            result[(int)breakpoint.Breakpoint] = CreateBuildUnitModificationCountDtos(counts);
        }

        AddBuildUnitModificationCountsUntil(
            player.BuildUnitModifications,
            int.MaxValue,
            seenTargets,
            counts,
            ref modificationIndex);
        result[(int)Breakpoint.All] = CreateBuildUnitModificationCountDtos(counts);
        return result;
    }

    private static void AddBuildUnitModificationCountsUntil(
        ReadOnlyCollection<DirectStrikeBuildUnitModification> modifications,
        int inclusiveGameloop,
        HashSet<(BuildUnitModificationType Type, int TargetUnitTag)> seenTargets,
        Dictionary<(BuildUnitModificationType Type, string TargetUnitName), int> counts,
        ref int modificationIndex)
    {
        while (modificationIndex < modifications.Count
            && modifications[modificationIndex].Gameloop <= inclusiveGameloop)
        {
            DirectStrikeBuildUnitModification modification = modifications[modificationIndex++];

            if (modification.Type == BuildUnitModificationType.PowerOverwhelming)
            {
                (BuildUnitModificationType Type, string TargetUnitName) key =
                    (modification.Type, "Supplicant");
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }
            else if (seenTargets.Add((modification.Type, modification.TargetUnitTag)))
            {
                (BuildUnitModificationType Type, string TargetUnitName) key =
                    (modification.Type, modification.TargetUnitName);
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }
        }
    }

    private static List<BuildUnitModificationCountDto> CreateBuildUnitModificationCountDtos(
        Dictionary<(BuildUnitModificationType Type, string TargetUnitName), int> counts)
    {
        List<BuildUnitModificationCountDto> result = new(counts.Count);
        foreach (KeyValuePair<(BuildUnitModificationType Type, string TargetUnitName), int> count in counts)
        {
            result.Add(new(count.Key.Type, count.Key.TargetUnitName, count.Value));
        }

        result.Sort(static (left, right) =>
        {
            int comparison = left.Type.CompareTo(right.Type);
            return comparison != 0
                ? comparison
                : string.Compare(left.TargetUnitName, right.TargetUnitName, StringComparison.Ordinal);
        });
        return result;
    }

    private static int GetGasCount(DirectStrikePlayer player, TimeSpan targetTime)
    {
        int gasCount = 0;
        foreach (TimeSpan refinery in player.RefineryTimes)
        {
            if (refinery <= targetTime)
            {
                gasCount++;
            }
        }

        return gasCount;
    }

    private static List<DsstatsUnitDto> CreateUnitDtos(
        DirectStrikePlayerSpawn spawn,
        IReadOnlyCollection<BuildUnitModificationCountDto> buildUnitModifications,
        DsstatsCommander commander)
    {
        Dictionary<string, int>? modificationCounts = null;
        if (buildUnitModifications.Count > 0)
        {
            modificationCounts = new(buildUnitModifications.Count, StringComparer.Ordinal);
            foreach (BuildUnitModificationCountDto modification in buildUnitModifications)
            {
                string normalizedName = UnitMap.GetNormalizedUnitName(modification.TargetUnitName, commander);
                modificationCounts[normalizedName] =
                    modificationCounts.GetValueOrDefault(normalizedName) + modification.Count;
            }
        }

        Dictionary<string, UnitDtoBuilder> unitsByName = new(StringComparer.Ordinal);
        foreach (DirectStrikeSpawnUnit unit in spawn.Units)
        {
            if (!unitsByName.TryGetValue(unit.Name, out UnitDtoBuilder? builder))
            {
                builder = new(unit.Name);
                unitsByName.Add(unit.Name, builder);
            }

            builder.Count++;
            builder.Positions.Add(unit.X);
            builder.Positions.Add(unit.Y);
        }

        List<UnitDtoBuilder> builders = new(unitsByName.Count);
        foreach (UnitDtoBuilder builder in unitsByName.Values)
        {
            builders.Add(builder);
        }

        builders.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));

        List<DsstatsUnitDto> units = new(builders.Count);
        foreach (UnitDtoBuilder builder in builders)
        {
            string normalizedName = UnitMap.GetNormalizedUnitName(builder.Name, commander);
            units.Add(new()
            {
                Name = builder.Name,
                Count = builder.Count,
                Special = modificationCounts is not null
                    && modificationCounts.TryGetValue(normalizedName, out int count)
                    ? count
                    : null,
                Positions = builder.Positions,
            });
        }

        return units;
    }

    private static Dictionary<DirectStrikePlayerSpawn, int> GetIncomesBySpawn(DirectStrikePlayer player)
    {
        Dictionary<DirectStrikePlayerSpawn, int> incomesBySpawn = new(player.Spawns.Count);
        if (player.Stats.Count == 0)
        {
            return incomesBySpawn;
        }

        foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
        {
            if (spawn.SummaryStats is not { } stats)
            {
                continue;
            }

            incomesBySpawn.Add(spawn, GetAccumulatedIncome(player, stats.Gameloop));
        }

        return incomesBySpawn;
    }

    private static int GetAccumulatedIncome(DirectStrikePlayer player, int targetGameloop)
    {
        var stats = player.Stats;
        if (targetGameloop <= 0 || stats.Count == 0)
        {
            return 0;
        }

        // Assumes stats are sorted by Gameloop ascending.
        if (targetGameloop <= stats[0].Gameloop)
        {
            return -GetRefineryCost(player, targetGameloop);
        }

        double income = 0;
        int previousGameloop = stats[0].Gameloop;
        int previousRate = stats[0].MineralsCollectionRate;

        for (int i = 1; i < stats.Count; i++)
        {
            DirectStrikePlayerStats stat = stats[i];
            int currentGameloop = Math.Min(stat.Gameloop, targetGameloop);

            if (currentGameloop > previousGameloop)
            {
                income += GetIncomeForInterval(previousRate, currentGameloop - previousGameloop);
                previousGameloop = currentGameloop;
            }

            if (stat.Gameloop >= targetGameloop)
            {
                return (int)income - GetRefineryCost(player, targetGameloop);
            }

            previousRate = stat.MineralsCollectionRate;
        }

        if (previousGameloop < targetGameloop)
        {
            income += GetIncomeForInterval(previousRate, targetGameloop - previousGameloop);
        }

        return (int)income - GetRefineryCost(player, targetGameloop);
    }

    private static double GetIncomeForInterval(int mineralsPerMinute, int gameloops)
    {
        return mineralsPerMinute * gameloops / GameLoopsPerMinute;
    }

    private static int GetRefineryCost(DirectStrikePlayer player, int targetGameloop)
    {
        int refineryCount = 0;
        foreach (TimeSpan refinery in player.RefineryTimes)
        {
            if (ToGameloop(refinery) < targetGameloop)
            {
                refineryCount++;
            }
        }

        int cost = 0;
        for (int i = 0; i < refineryCount && i < RefineryCosts.Length; i++)
        {
            cost += RefineryCosts[i];
        }

        return cost;
    }

    private static int ToGameloop(TimeSpan time)
    {
        return (int)(time.TotalSeconds * GameLoopsPerSecond);
    }

    private static Dictionary<DirectStrikePlayerSpawn, int> GetArmyValuesBySpawn(DirectStrikePlayer player)
    {
        Dictionary<DirectStrikePlayerSpawn, int> armyValuesBySpawn = new(player.Spawns.Count);
        int cumulativePreviousArmyValue = 0;

        foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
        {
            if (spawn.SummaryStats is not { } stats)
            {
                continue;
            }

            int armyValue = (stats.MineralsUsedActiveForces - cumulativePreviousArmyValue + stats.MineralsLostArmy) / 2;
            armyValuesBySpawn.Add(spawn, armyValue);
            cumulativePreviousArmyValue += armyValue;
        }

        return armyValuesBySpawn;
    }

    private static Dictionary<DirectStrikePlayer, MessageCounts> GetMessageCountsByPlayer(Sc2Replay replay, DirectStrikeReplay directStrikeReplay)
    {
        Dictionary<int, DirectStrikePlayer> playersByUserId = GetPlayersByUserId(replay, directStrikeReplay);
        Dictionary<DirectStrikePlayer, MessageCounts> countsByPlayer = new(directStrikeReplay.Players.Count);

        foreach (ChatMessageEvent chatMessage in replay.ChatMessages ?? [])
        {
            if (playersByUserId.TryGetValue(chatMessage.UserId, out DirectStrikePlayer? player))
            {
                countsByPlayer[player] = countsByPlayer.GetValueOrDefault(player).AddMessage();
            }
        }

        foreach (PingMessageEvent pingMessage in replay.PingMessages ?? [])
        {
            if (playersByUserId.TryGetValue(pingMessage.UserId, out DirectStrikePlayer? player))
            {
                countsByPlayer[player] = countsByPlayer.GetValueOrDefault(player).AddPing();
            }
        }

        return countsByPlayer;
    }

    private static Dictionary<int, DirectStrikePlayer> GetPlayersByUserId(Sc2Replay replay, DirectStrikeReplay directStrikeReplay)
    {
        Dictionary<int, DirectStrikePlayer> playersByUserId = [];
        Dictionary<(int Region, int Realm, int Id), DirectStrikePlayer> playersByToon = [];
        Dictionary<int, DirectStrikePlayer> playersBySlotId = [];

        foreach (DirectStrikePlayer player in directStrikeReplay.Players)
        {
            playersByToon.TryAdd((player.Region, player.Realm, player.Id), player);
            playersBySlotId.TryAdd(player.SlotId, player);
        }

        foreach (Slot slot in replay.Initdata?.LobbyState?.Slots ?? [])
        {
            if (slot.UserId is not { } userId)
            {
                continue;
            }

            if ((TryParseToonHandle(slot.ToonHandle, out int region, out int realm, out int id)
                    && playersByToon.TryGetValue((region, realm, id), out DirectStrikePlayer? player))
                || playersBySlotId.TryGetValue(slot.WorkingSetSlotId, out player))
            {
                playersByUserId.TryAdd(userId, player);
            }
        }

        return playersByUserId;
    }

    private static DsstatsCommander ToCommander(Race race)
    {
        return race switch
        {
            Race.Terran => DsstatsCommander.Terran,
            Race.Protoss => DsstatsCommander.Protoss,
            Race.Zerg => DsstatsCommander.Zerg,
            Race.Random => DsstatsCommander.Random,
            _ => DsstatsCommander.None,
        };
    }

    private static dsstats.shared.PlayerResult ToPlayerResult(PlayerResult result)
    {
        return result switch
        {
            PlayerResult.Win => dsstats.shared.PlayerResult.Win,
            PlayerResult.Loss => dsstats.shared.PlayerResult.Los,
            _ => dsstats.shared.PlayerResult.None,
        };
    }

    private static List<int> CreateMiddleChanges(DirectStrikeReplay replay)
    {
        if (replay.FirstMiddleControlTeam is not (1 or 2) || replay.MiddleChanges.Length == 0)
        {
            return [];
        }

        List<int> middleChanges = new(replay.MiddleChanges.Length + 1)
        {
            replay.FirstMiddleControlTeam,
        };
        foreach (TimeSpan middleChange in replay.MiddleChanges)
        {
            middleChanges.Add(ToSeconds(middleChange));
        }

        return middleChanges;
    }

    private static List<int> ToSeconds(IReadOnlyCollection<TimeSpan> values)
    {
        List<int> result = new(values.Count);
        foreach (TimeSpan value in values)
        {
            result.Add(ToSeconds(value));
        }

        return result;
    }

    private static int ToSeconds(TimeSpan value)
    {
        return value <= TimeSpan.Zero ? 0 : (int)value.TotalSeconds;
    }

    internal static void SetMvp(DsstatsReplayDto replay)
    {
        int maxKilledValue = 0;
        bool hasFinalSpawn = false;
        bool hasLeaver = false;
        foreach (DsstatsReplayPlayerDto player in replay.Players)
        {
            player.IsMvp = false;
            hasLeaver |= DsstatsReplayRules.IsLeaver(replay.Duration, player.Duration);

            foreach (DsstatsSpawnDto spawn in player.Spawns)
            {
                if (spawn.Breakpoint == DsstatsBreakpoint.All)
                {
                    hasFinalSpawn = true;
                    maxKilledValue = Math.Max(maxKilledValue, spawn.KilledValue);
                    break;
                }
            }
        }

        if (hasLeaver || !hasFinalSpawn)
        {
            return;
        }

        foreach (DsstatsReplayPlayerDto player in replay.Players)
        {
            foreach (DsstatsSpawnDto spawn in player.Spawns)
            {
                if (spawn.Breakpoint == DsstatsBreakpoint.All)
                {
                    player.IsMvp = spawn.KilledValue == maxKilledValue;
                    break;
                }
            }
        }
    }

    private static string GetReplayVersion(Sc2Replay replay)
    {
        string metadataVersion = replay.Metadata?.GameVersion?.ToString() ?? string.Empty;
        return !string.IsNullOrEmpty(metadataVersion)
            ? metadataVersion
            : replay.Header is Header header ? header.Version.ToString() : string.Empty;
    }

    private static int ParseBaseBuild(string baseBuild, Sc2Replay replay)
    {
        return int.TryParse(baseBuild, out int parsedBaseBuild) ? parsedBaseBuild : replay.Header is Header header ? header.BaseBuild : 0;
    }

    private static bool TryParseToonHandle(
        string? toonHandle,
        out int region,
        out int realm,
        out int id)
    {
        region = 0;
        realm = 0;
        id = 0;

        if (string.IsNullOrEmpty(toonHandle))
        {
            return false;
        }

        ReadOnlySpan<char> value = toonHandle.AsSpan();
        int firstSeparator = value.IndexOf('-');
        if (firstSeparator <= 0)
        {
            return false;
        }

        ReadOnlySpan<char> remaining = value[(firstSeparator + 1)..];
        int secondSeparator = remaining.IndexOf('-');
        if (secondSeparator <= 0
            || !remaining[..secondSeparator].SequenceEqual("S2"))
        {
            return false;
        }

        remaining = remaining[(secondSeparator + 1)..];
        int thirdSeparator = remaining.IndexOf('-');
        return thirdSeparator > 0
            && int.TryParse(value[..firstSeparator], CultureInfo.InvariantCulture, out region)
            && int.TryParse(remaining[..thirdSeparator], CultureInfo.InvariantCulture, out realm)
            && int.TryParse(remaining[(thirdSeparator + 1)..], CultureInfo.InvariantCulture, out id);
    }

    private readonly record struct BreakpointDefinition(Breakpoint Breakpoint, int Gameloop);

    private readonly record struct MessageCounts(int Messages, int Pings)
    {
        public MessageCounts AddMessage()
        {
            return this with { Messages = Messages + 1 };
        }

        public MessageCounts AddPing()
        {
            return this with { Pings = Pings + 1 };
        }
    }

    private sealed class UnitDtoBuilder(string name)
    {
        public string Name { get; } = name;

        public int Count { get; set; }

        public List<int> Positions { get; } = [];
    }
}
