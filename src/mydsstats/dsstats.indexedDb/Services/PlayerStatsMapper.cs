using dsstats.shared;

namespace dsstats.indexedDb.Services;

internal static class PlayerStatsMapper
{
    private record ProcessedStats
    {
        public Dictionary<Commander, (int Count, int Wins, int Mvp)> CommanderStats { get; init; } = [];
        public Dictionary<GameMode, int> GameModeCounts { get; init; } = [];
        public Dictionary<(int, int, int), (PlayerDto Player, int Count, int Wins)> TeammateStats { get; init; } = [];
        public Dictionary<(int, int, int), (PlayerDto Player, int Count, int Wins)> OpponentStats { get; init; } = [];
    }

    private static ProcessedStats ProcessGameModeStats(List<GameModeStats> gameModeStats)
    {
        var commanderStats = new Dictionary<Commander, (int Count, int Wins, int Mvp)>();
        var gameModeCounts = new Dictionary<GameMode, int>();
        var teammateStats = new Dictionary<(int, int, int), (PlayerDto Player, int Count, int Wins)>();
        var opponentStats = new Dictionary<(int, int, int), (PlayerDto Player, int Count, int Wins)>();

        if (gameModeStats is null)
        {
            return new ProcessedStats { CommanderStats = commanderStats, GameModeCounts = gameModeCounts, TeammateStats = teammateStats, OpponentStats = opponentStats };
        }

        foreach (var gms in gameModeStats)
        {
            var gameMode = (GameMode)gms.GameMode;
            gameModeCounts.TryGetValue(gameMode, out var currentGameModeCount);
            gameModeCounts[gameMode] = currentGameModeCount + (gms.CommanderStats?.Sum(cs => cs.Count) ?? 0);

            foreach (var cs in gms.CommanderStats ?? [])
            {
                var commander = (Commander)cs.Commander;
                commanderStats.TryGetValue(commander, out var currentCommanderStat);
                commanderStats[commander] = (currentCommanderStat.Count + cs.Count, currentCommanderStat.Wins + cs.Wins, currentCommanderStat.Mvp + cs.Mvp);
            }

            foreach (var ts in gms.TeammateStats ?? [])
            {
                var key = (ts.Player.ToonId.Region, ts.Player.ToonId.Realm, ts.Player.ToonId.Id);
                teammateStats.TryGetValue(key, out var currentTeammateStat);
                teammateStats[key] = (ts.Player, currentTeammateStat.Count + ts.Count, currentTeammateStat.Wins + ts.Wins);
            }

            foreach (var os in gms.OpponentStats ?? [])
            {
                var key = (os.Player.ToonId.Region, os.Player.ToonId.Realm, os.Player.ToonId.Id);
                opponentStats.TryGetValue(key, out var currentOpponentStat);
                opponentStats[key] = (os.Player, currentOpponentStat.Count + os.Count, currentOpponentStat.Wins + os.Wins);
            }
        }

        return new ProcessedStats { CommanderStats = commanderStats, GameModeCounts = gameModeCounts, TeammateStats = teammateStats, OpponentStats = opponentStats };
    }

    public static PlayerStatsResponse MapToPlayerStatsResponse(MyPlayerStats myPlayerStats)
    {
        var processedStats = ProcessGameModeStats(myPlayerStats.GameModeStats);

        var response = new PlayerStatsResponse
        {
            Name = myPlayerStats.Player?.Name ?? string.Empty,
            RegionId = myPlayerStats.Player?.ToonId?.Region ?? 0,
            Ratings = [GetAllRating(myPlayerStats, processedStats)],
            RatingDetails = MapToRatingDetails(myPlayerStats, processedStats),
        };
        return response;
    }

    private static PlayerRatingListItem GetAllRating(MyPlayerStats stats, ProcessedStats processedStats)
    {
        var cmdrCounts = processedStats.CommanderStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        var wins = processedStats.CommanderStats.Sum(kvp => kvp.Value.Wins);
        var mvps = processedStats.CommanderStats.Sum(kvp => kvp.Value.Mvp);

        if (cmdrCounts.Count == 0)
        {
            return new()
            {
                RatingType = RatingType.All,
                RegionId = stats.Player.ToonId.Region,
                Name = stats.Player.Name,
                Games = 0,
            };
        }
        
        var total = cmdrCounts.Sum(s => s.Value);
        var mainCmdrCount = cmdrCounts.Max(m => m.Value);
        var mainCmdr = cmdrCounts.First(x => x.Value == mainCmdrCount).Key;

        return new()
        {
            RatingType = RatingType.All,
            RegionId = stats.Player.ToonId.Region,
            Name = stats.Player.Name,
            Main = mainCmdr,
            MainCount = mainCmdrCount,
            Games = total,
            Wins = wins,
            Mvps = mvps,
        };
    }

    private static List<RatingDetails> MapToRatingDetails(MyPlayerStats playerStats, ProcessedStats processedStats)
    {
        return new List<RatingDetails>
        {
            new RatingDetails
            {
                RatingType = RatingType.All,
                GameModes = MapToGameModeCounts(processedStats.GameModeCounts),
                Commanders = MapToCommanderCounts(processedStats.CommanderStats),
                Ratings = [], // Empty - no rating history in MyPlayerStats
                Replays = playerStats.RecentReplays, 
                AvgGainResponses = MapToCmdrAvgGainResponse(processedStats.CommanderStats),
                TeammateStats = MapToPlayerStatsList(processedStats.TeammateStats),
                OpponentStats = MapToPlayerStatsList(processedStats.OpponentStats),
            }
        };
    }

    private static List<OtherPlayerStats> MapToPlayerStatsList(Dictionary<(int, int, int), (PlayerDto Player, int Count, int Wins)> playerStats)
    {
        return playerStats.Select(kvp => new OtherPlayerStats
            {
                Player = kvp.Value.Player,
                Count = kvp.Value.Count,
                Wins = kvp.Value.Wins
            })
            .OrderByDescending(s => s.Count)
            .ToList();
    }

    private static List<GameModeCount> MapToGameModeCounts(Dictionary<GameMode, int> gameModeCounts)
    {
        return gameModeCounts.Select(kvp => new GameModeCount
        {
            GameMode = kvp.Key,
            Count = kvp.Value
        }).ToList();
    }

    private static List<CommanderCount> MapToCommanderCounts(Dictionary<Commander, (int Count, int Wins, int Mvp)> commanderStats)
    {
        return commanderStats.Select(kvp => new CommanderCount
        {
            Commander = kvp.Key,
            Count = kvp.Value.Count
        }).ToList();
    }

    private static List<CmdrAvgGainResponse> MapToCmdrAvgGainResponse(Dictionary<Commander, (int Count, int Wins, int Mvp)> commanderStats)
    {
        var commanderWinRates = commanderStats
            .Where(kvp => kvp.Value.Count > 0)
            .Select(kvp => new PlayerCmdrAvgGain
            {
                Commander = kvp.Key,
                Count = kvp.Value.Count,
                Wins = kvp.Value.Wins,
                AvgGain = 0
            });

        return new List<CmdrAvgGainResponse>
        {
            new CmdrAvgGainResponse
            {
                TimePeriod = TimePeriod.AllTime,
                AvgGains = commanderWinRates.ToList()
            }
        };
    }
}

public static class MyPlayerStatsExtensions
{
    public static PlayerStatsResponse ToPlayerStatsResponse(this MyPlayerStats myPlayerStats)
    {
        return PlayerStatsMapper.MapToPlayerStatsResponse(myPlayerStats);
    }
}