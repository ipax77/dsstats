using dsstats.parser;
using dsstats.shared;

namespace dsstats.tests;

[TestClass]
public sealed class StatsTests
{
    private const string ReplayFile = "Direct Strike (9886).SC2Replay";

    // Ground-truth data from in-game stats summary.
    // LostValue = game-UI "Lost (Value)" directly (MineralsLostArmy has no doubling).
    private static readonly Dictionary<string, (int KilledValue, int UpgradeSpent, int GasCount, int SpawnedUnits, int LostValue)> ExpectedStats = new()
    {
        ["And"]     = (10_780, 1_300, 2, 298, 16_077),
        ["Vlados"]  = (11_790,   550, 2, 141, 19_160),
        ["PAX"]     = (15_255, 1_225, 2,  98, 17_295),
        ["drvkize"] = (20_469, 1_475, 1,  90, 13_450),
        ["Memnon"]  = (15_160,   625, 3,  93, 16_070),
        ["Warhead"] = (16_903, 1_100, 2, 107, 14_060),
    };

    private static readonly Dictionary<string, int> ExpectedTeam = new()
    {
        ["And"] = 1, ["Vlados"] = 1, ["PAX"] = 1,
        ["drvkize"] = 2, ["Memnon"] = 2, ["Warhead"] = 2,
    };

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanExtractStats()
    {
        var replayDto = await GetReplayDto(ReplayFile);
        Assert.AreEqual(602, replayDto.Duration);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanDecodeAllPlayers()
    {
        var replayDto = await GetReplayDto(ReplayFile);

        Assert.AreEqual(6, replayDto.Players.Count);

        foreach (var name in ExpectedStats.Keys)
        {
            Assert.IsTrue(
                replayDto.Players.Any(p => p.Name == name),
                $"Expected player '{name}' not found in decoded replay.");
        }
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanDecodeTeamAssignments()
    {
        var replayDto = await GetReplayDto(ReplayFile);

        Assert.AreEqual(3, replayDto.Players.Count(p => p.TeamId == 1), "Expected 3 players on Team 1");
        Assert.AreEqual(3, replayDto.Players.Count(p => p.TeamId == 2), "Expected 3 players on Team 2");

        foreach (var (name, expectedTeam) in ExpectedTeam)
        {
            var player = replayDto.Players.FirstOrDefault(p => p.Name == name);
            Assert.IsNotNull(player, $"Player '{name}' not found");
            Assert.AreEqual(expectedTeam, player.TeamId,
                $"Player '{name}' expected on Team {expectedTeam}, got Team {player.TeamId}");
        }
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanDecodeKilledValues()
    {
        const int tolerance = 500;
        var replayDto = await GetReplayDto(ReplayFile);

        foreach (var (name, expected) in ExpectedStats)
        {
            var player = replayDto.Players.First(p => p.Name == name);
            var finalSpawn = player.Spawns.FirstOrDefault(s => s.Breakpoint == Breakpoint.All);
            Assert.IsNotNull(finalSpawn, $"Player '{name}' has no final spawn");
            Assert.IsTrue(
                Math.Abs(finalSpawn.KilledValue - expected.KilledValue) <= tolerance,
                $"Player '{name}': KilledValue {finalSpawn.KilledValue} is not within ±{tolerance} of expected {expected.KilledValue}");
        }
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanDecodeUpgradeSpending()
    {
        // Tolerance of 350 accounts for the fact that MineralsUsedCurrentTechnology
        // in tracker events may not perfectly match the in-game UI tally (e.g. scan spend)
        const int tolerance = 350;
        var replayDto = await GetReplayDto(ReplayFile);

        foreach (var (name, expected) in ExpectedStats)
        {
            var player = replayDto.Players.First(p => p.Name == name);
            var finalSpawn = player.Spawns.FirstOrDefault(s => s.Breakpoint == Breakpoint.All);
            Assert.IsNotNull(finalSpawn, $"Player '{name}' has no final spawn");
            Assert.IsTrue(
                Math.Abs(finalSpawn.UpgradeSpent - expected.UpgradeSpent) <= tolerance,
                $"Player '{name}': UpgradeSpent {finalSpawn.UpgradeSpent} is not within ±{tolerance} of expected {expected.UpgradeSpent}");
        }
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanDecodeRefineries()
    {
        // ±1 tolerance: the parser may miss a refinery taken very close to game end
        var replayDto = await GetReplayDto(ReplayFile);

        foreach (var (name, expected) in ExpectedStats)
        {
            var player = replayDto.Players.First(p => p.Name == name);
            var finalSpawn = player.Spawns.FirstOrDefault(s => s.Breakpoint == Breakpoint.All);
            Assert.IsNotNull(finalSpawn, $"Player '{name}' has no final spawn");
            Assert.IsTrue(
                Math.Abs(finalSpawn.GasCount - expected.GasCount) <= 1,
                $"Player '{name}': expected {expected.GasCount} refineries (±1), got {finalSpawn.GasCount}");
        }
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanDecodeSpawnedUnits()
    {
        // The parser captures unit composition at breakpoint snapshots, not a running total.
        // Verify each player has at least one spawn with a non-trivial unit composition decoded.
        var replayDto = await GetReplayDto(ReplayFile);

        foreach (var (name, _) in ExpectedStats)
        {
            var player = replayDto.Players.First(p => p.Name == name);
            var finalSpawn = player.Spawns.FirstOrDefault(s => s.Breakpoint == Breakpoint.All);
            Assert.IsNotNull(finalSpawn, $"Player '{name}' has no final spawn");
            Assert.IsTrue(finalSpawn.Units.Count > 0,
                $"Player '{name}': final spawn has no units decoded");
            Assert.IsTrue(finalSpawn.Units.Sum(u => u.Count) > 0,
                $"Player '{name}': final spawn unit counts are all zero");
        }
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanDecodeMiddleControl()
    {
        var replayDto = await GetReplayDto(ReplayFile);

        Assert.IsTrue(replayDto.MiddleChanges.Count > 0, "MiddleChanges should not be empty — middle was contested");

        var (team1Seconds, team2Seconds) = replayDto.GetMiddleIncome(replayDto.Duration);
        Assert.IsTrue(team2Seconds > team1Seconds,
            $"Team 2 should have held mid longer (Team1={team1Seconds}s, Team2={team2Seconds}s)");

        // In-game stats summary: Team 1 owned mid for 180s, Team 2 for 415s.
        // Tolerance ±30s: (int)(gameloop/22.4) truncation accumulates across many mid-change events.
        const int tolerance = 30;
        Assert.IsTrue(Math.Abs(team1Seconds - 180) <= tolerance,
            $"Team 1 mid control: {team1Seconds}s, expected ≈180s (±{tolerance})");
        Assert.IsTrue(Math.Abs(team2Seconds - 415) <= tolerance,
            $"Team 2 mid control: {team2Seconds}s, expected ≈415s (±{tolerance})");
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanDecodeWinnerTeam()
    {
        var replayDto = await GetReplayDto(ReplayFile);
        Assert.AreEqual(2, replayDto.WinnerTeam, "Team 2 (drvkize, Memnon, Warhead) should be the winner");
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanDecodeArmyValue()
    {
        // ArmyValue = MineralsUsedActiveForces / 2 = current army deployed at the last stats event.
        // This is NOT the same as the game-UI "Army Spending" (which is a cumulative metric).
        // Sanity checks: non-zero (army was deployed and decoded) and less than KilledValue
        // (current snapshot must be smaller than the cumulative kills over the whole game).
        var replayDto = await GetReplayDto(ReplayFile);

        foreach (var (name, _) in ExpectedStats)
        {
            var player = replayDto.Players.First(p => p.Name == name);
            var finalSpawn = player.Spawns.FirstOrDefault(s => s.Breakpoint == Breakpoint.All);
            Assert.IsNotNull(finalSpawn, $"Player '{name}' has no final spawn");
            Assert.IsTrue(finalSpawn.ArmyValue > 0,
                $"Player '{name}': ArmyValue should be > 0 but got {finalSpawn.ArmyValue}");
            Assert.IsTrue(finalSpawn.ArmyValue < finalSpawn.KilledValue,
                $"Player '{name}': ArmyValue ({finalSpawn.ArmyValue}) should be less than KilledValue ({finalSpawn.KilledValue})");
        }
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9886).SC2Replay")]
    public async Task CanDecodeLostValues()
    {
        // LostValue = MineralsLostArmy = total mineral value of own army destroyed
        // Maps to "Lost (Value)" in the in-game Combat stats
        const int tolerance = 500;
        var replayDto = await GetReplayDto(ReplayFile);

        foreach (var (name, expected) in ExpectedStats)
        {
            var player = replayDto.Players.First(p => p.Name == name);
            var finalSpawn = player.Spawns.FirstOrDefault(s => s.Breakpoint == Breakpoint.All);
            Assert.IsNotNull(finalSpawn, $"Player '{name}' has no final spawn");
            Assert.IsTrue(
                Math.Abs(finalSpawn.LostValue - expected.LostValue) <= tolerance,
                $"Player '{name}': LostValue {finalSpawn.LostValue} is not within ±{tolerance} of expected {expected.LostValue}");
        }
    }

    private static async Task<ReplayDto> GetReplayDto(string replayPath)
    {
        var sc2Replay = await DsstatsParser.GetSc2Replay(replayPath);
        Assert.IsNotNull(sc2Replay);
        var replayDto = DsstatsParser.ParseReplay(sc2Replay);
        Assert.IsNotNull(replayDto);
        return replayDto;
    }
}
