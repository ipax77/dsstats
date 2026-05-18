using dsstats.db;
using dsstats.dbServices.BuildDetails;
using dsstats.shared;
using dsstats.shared.DetailBuild;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.tests;

[TestClass]
public sealed class BuildDetailsServiceTests
{
    [TestMethod]
    public async Task GetOverview_AggregatesRatingGainWinrateAndGasFirstStats()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: true, won: true, ratingDelta: 2);
        await fixture.SeedReplayAsync(4, ProtossBuild.Zealots, TerranBuild.Mech, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 6);

        var rows = await fixture.Service.GetOverview(CreateRequest());
        var stalker = rows.Single(x => x.Commander == Commander.Protoss && x.Build == (int)ProtossBuild.Stalker);

        Assert.AreEqual(3, stalker.Games);
        Assert.AreEqual(2, stalker.Wins);
        Assert.AreEqual(2.67, stalker.AverageRatingGain, 0.001);
        Assert.AreEqual(66.67, stalker.Winrate, 0.001);
        Assert.AreEqual(2, stalker.GasFirstGames);
        Assert.AreEqual(66.67, stalker.GasFirstRate, 0.001);
    }

    [TestMethod]
    public async Task GetMatchups_AggregatesSelectedBuildVersusOpponentBuild()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: true, won: true, ratingDelta: 2);
        await fixture.SeedReplayAsync(4, ProtossBuild.Stalker, TerranBuild.Mech, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 8);

        var rows = await fixture.Service.GetMatchups(CreateMatchupRequest());
        var bio = rows.Single(x => x.OpponentCommander == Commander.Terran && x.OpponentBuild == (int)TerranBuild.Bio);

        Assert.AreEqual(3, bio.Games);
        Assert.AreEqual(2, bio.Wins);
        Assert.AreEqual(2.67, bio.AverageRatingGain, 0.001);
        Assert.AreEqual(2, bio.SelectedGasFirstGames);
        Assert.AreEqual(2, bio.OpponentGasFirstGames);
    }

    [TestMethod]
    public async Task GetMatchups_AppliesGasFirstFilters()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: true, won: true, ratingDelta: 2);

        var anyGas = await fixture.Service.GetMatchups(CreateMatchupRequest(BuildDetailsGasFilter.Any));
        var withGas = await fixture.Service.GetMatchups(CreateMatchupRequest(BuildDetailsGasFilter.WithGas));
        var withoutGas = await fixture.Service.GetMatchups(CreateMatchupRequest(BuildDetailsGasFilter.WithoutGas));

        Assert.AreEqual(3, anyGas.Single().Games);
        Assert.AreEqual(2.67, anyGas.Single().AverageRatingGain, 0.001);
        Assert.AreEqual(2, withGas.Single().Games);
        Assert.AreEqual(-1.0, withGas.Single().AverageRatingGain, 0.001);
        Assert.AreEqual(1, withoutGas.Single().Games);
        Assert.AreEqual(10.0, withoutGas.Single().AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetSampleReplays_ReturnsBoundedClickableReplayHashesForSelectedMatchup()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: true, won: true, ratingDelta: 2);

        var rows = await fixture.Service.GetSampleReplays(new BuildDetailsSamplesRequest
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.Protoss,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            SelectedCommander = Commander.Protoss,
            SelectedBuild = (int)ProtossBuild.Stalker,
            OpponentCommander = Commander.Terran,
            OpponentBuild = (int)TerranBuild.Bio,
            Count = 2,
        });

        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("hash-3", rows[0].Replay.ReplayHash);
        Assert.AreEqual("hash-2", rows[1].Replay.ReplayHash);
        Assert.AreEqual(1, rows[0].Replay.PlayerPos);
        Assert.AreEqual(2.0, rows[0].Replay.PlayerGain, 0.001);
        Assert.IsTrue(rows[0].Replay.CommandersTeam1.Contains(Commander.Protoss));
        Assert.IsTrue(rows[0].Replay.CommandersTeam2.Contains(Commander.Terran));
    }

    [TestMethod]
    public async Task GetOverview_ExcludesLeaversUnlessRequested()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 10);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 30, leaverType: LeaverType.OneLeaver);

        var withoutLeavers = await fixture.Service.GetOverview(CreateRequest());
        var withLeaversRequest = CreateRequest();
        withLeaversRequest.WithLeavers = true;
        var withLeavers = await fixture.Service.GetOverview(withLeaversRequest);

        Assert.AreEqual(1, withoutLeavers.Single().Games);
        Assert.AreEqual(2, withLeavers.Single().Games);
        Assert.AreEqual(20.0, withLeavers.Single().AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetOverview_FiltersTeAndNonTeUsingMatchingRatingTypes()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 10, te: true);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: false, ratingDelta: -4, te: false);

        var all = await fixture.Service.GetOverview(CreateRequest(BuildDetailsTeFilter.All));
        var te = await fixture.Service.GetOverview(CreateRequest(BuildDetailsTeFilter.TE));
        var nonTe = await fixture.Service.GetOverview(CreateRequest(BuildDetailsTeFilter.NonTE));

        Assert.AreEqual(2, all.Single().Games);
        Assert.AreEqual(3.0, all.Single().AverageRatingGain, 0.001);
        Assert.AreEqual(1, te.Single().Games);
        Assert.AreEqual(10.0, te.Single().AverageRatingGain, 0.001);
        Assert.AreEqual(1, nonTe.Single().Games);
        Assert.AreEqual(-4.0, nonTe.Single().AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetOverview_FiltersToSelectedPlayerAsBuildOwner()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 10, selectedPlayerId: 42);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: false, ratingDelta: -4, selectedPlayerId: 99);
        await fixture.SeedReplayAsync(3, ProtossBuild.Zealots, TerranBuild.Mech, selectedGasFirst: true, opponentGasFirst: false, won: true, ratingDelta: 6, selectedPlayerId: 42);

        var request = CreateRequest();
        request.Player = CreatePlayer(42);
        var rows = await fixture.Service.GetOverview(request);

        Assert.AreEqual(2, rows.Count);
        var stalker = rows.Single(x => x.Build == (int)ProtossBuild.Stalker);
        var zealots = rows.Single(x => x.Build == (int)ProtossBuild.Zealots);
        Assert.AreEqual(1, stalker.Games);
        Assert.AreEqual(10.0, stalker.AverageRatingGain, 0.001);
        Assert.AreEqual(1, zealots.Games);
        Assert.AreEqual(6.0, zealots.AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetMatchups_FiltersToSelectedPlayerAsBuildOwner()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10, selectedPlayerId: 42);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4, selectedPlayerId: 42);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 20, selectedPlayerId: 99);
        await fixture.SeedReplayAsync(4, ProtossBuild.Stalker, TerranBuild.Mech, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 8, selectedPlayerId: 42);

        var request = CreateMatchupRequest();
        request.Player = CreatePlayer(42);
        var rows = await fixture.Service.GetMatchups(request);

        Assert.AreEqual(2, rows.Count);
        var bio = rows.Single(x => x.OpponentBuild == (int)TerranBuild.Bio);
        var mech = rows.Single(x => x.OpponentBuild == (int)TerranBuild.Mech);
        Assert.AreEqual(2, bio.Games);
        Assert.AreEqual(3.0, bio.AverageRatingGain, 0.001);
        Assert.AreEqual(1, mech.Games);
        Assert.AreEqual(8.0, mech.AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetSampleReplays_FiltersToSelectedPlayerAsBuildOwner()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10, selectedPlayerId: 42);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4, selectedPlayerId: 99);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: true, won: true, ratingDelta: 2, selectedPlayerId: 42);

        var rows = await fixture.Service.GetSampleReplays(new BuildDetailsSamplesRequest
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.Protoss,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            Player = CreatePlayer(42),
            SelectedCommander = Commander.Protoss,
            SelectedBuild = (int)ProtossBuild.Stalker,
            OpponentCommander = Commander.Terran,
            OpponentBuild = (int)TerranBuild.Bio,
            Count = 10,
        });

        CollectionAssert.AreEqual(new[] { "hash-3", "hash-1" }, rows.Select(x => x.Replay.ReplayHash).ToArray());
        Assert.IsTrue(rows.All(x => x.Replay.PlayerPos is 1));
    }

    [TestMethod]
    public async Task GetOverview_UsesSeparateCacheEntriesForPlayerFilteredRequests()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 10, selectedPlayerId: 42);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: false, ratingDelta: -4, selectedPlayerId: 99);

        var globalRows = await fixture.Service.GetOverview(CreateRequest());
        var request = CreateRequest();
        request.Player = CreatePlayer(42);
        var playerRows = await fixture.Service.GetOverview(request);

        Assert.AreEqual(2, globalRows.Single().Games);
        Assert.AreEqual(1, playerRows.Single().Games);
        Assert.AreNotEqual(CreateRequest().GetMemKey(), request.GetMemKey());
    }

    [TestMethod]
    public async Task GetTeamBuildOverview_AggregatesTeamAverageGainAndWinrate()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedTeamBuildReplayAsync(1, won: true, leaderDelta: 10, followerDelta: 6);
        await fixture.SeedTeamBuildReplayAsync(2, won: false, leaderDelta: -4, followerDelta: -2);

        var rows = await fixture.Service.GetTeamBuildOverview(CreateRequest());
        var row = rows.Single(x => x.TeamBuild == TeamBuild.PTStack);

        Assert.AreEqual(2, row.Games);
        Assert.AreEqual(1, row.Wins);
        Assert.AreEqual(2.5, row.AverageRatingGain, 0.001);
        Assert.AreEqual(50.0, row.Winrate, 0.001);
    }

    [TestMethod]
    public async Task GetTeamBuildOverview_FiltersCommanderAndPlayerAcrossLeaderAndFollower()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedTeamBuildReplayAsync(1, won: true, leaderDelta: 10, followerDelta: 6, followerPlayerId: 42);
        await fixture.SeedTeamBuildReplayAsync(2, won: true, leaderDelta: 8, followerDelta: 4, followerPlayerId: 99);

        var commanderRequest = CreateRequest();
        commanderRequest.Commander = Commander.Terran;
        var commanderRows = await fixture.Service.GetTeamBuildOverview(commanderRequest);

        var playerRequest = CreateRequest();
        playerRequest.Player = CreatePlayer(42);
        var playerRows = await fixture.Service.GetTeamBuildOverview(playerRequest);

        Assert.AreEqual(2, commanderRows.Single().Games);
        Assert.AreEqual(1, playerRows.Single().Games);
        Assert.AreEqual(8.0, playerRows.Single().AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetTeamBuildOverview_AppliesTeAndLeaverFilters()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedTeamBuildReplayAsync(1, won: true, leaderDelta: 10, followerDelta: 6, te: true);
        await fixture.SeedTeamBuildReplayAsync(2, won: true, leaderDelta: 8, followerDelta: 4, te: false);
        await fixture.SeedTeamBuildReplayAsync(3, won: true, leaderDelta: 20, followerDelta: 10, leaverType: LeaverType.OneLeaver);

        var all = await fixture.Service.GetTeamBuildOverview(CreateRequest(BuildDetailsTeFilter.All));
        var te = await fixture.Service.GetTeamBuildOverview(CreateRequest(BuildDetailsTeFilter.TE));
        var nonTe = await fixture.Service.GetTeamBuildOverview(CreateRequest(BuildDetailsTeFilter.NonTE));
        var withLeaversRequest = CreateRequest();
        withLeaversRequest.WithLeavers = true;
        var withLeavers = await fixture.Service.GetTeamBuildOverview(withLeaversRequest);

        Assert.AreEqual(2, all.Single().Games);
        Assert.AreEqual(1, te.Single().Games);
        Assert.AreEqual(1, nonTe.Single().Games);
        Assert.AreEqual(3, withLeavers.Single().Games);
    }

    [TestMethod]
    public async Task GetTeamBuildSampleReplays_ReturnsNewestDistinctReplayMetadata()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedTeamBuildReplayAsync(1, won: true, leaderDelta: 10, followerDelta: 6);
        await fixture.SeedTeamBuildReplayAsync(2, won: false, leaderDelta: -4, followerDelta: -2);
        await fixture.SeedTeamBuildReplayAsync(3, won: true, leaderDelta: 8, followerDelta: 4);

        var rows = await fixture.Service.GetTeamBuildSampleReplays(new BuildDetailsTeamBuildSamplesRequest
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.Protoss,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            SelectedTeamBuild = TeamBuild.PTStack,
            Count = 2,
        });

        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("team-hash-3", rows[0].Replay.ReplayHash);
        Assert.AreEqual("team-hash-2", rows[1].Replay.ReplayHash);
        Assert.AreEqual(1, rows[0].LeaderGamePos);
        Assert.AreEqual(2, rows[0].FollowerGamePos);
        Assert.AreEqual(Commander.Protoss, rows[0].LeaderCommander);
        Assert.AreEqual(Commander.Terran, rows[0].FollowerCommander);
        Assert.AreEqual(6.0, rows[0].Replay.PlayerGain, 0.001);
        Assert.IsTrue(rows[0].Replay.CommandersTeam1.Contains(Commander.Protoss));
        Assert.IsTrue(rows[0].Replay.CommandersTeam1.Contains(Commander.Terran));
    }

    [TestMethod]
    public async Task GetRaceRosterOverview_AggregatesOrderedRosterAverageGainAndWinrate()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedRaceRosterReplayAsync(1, winnerTeam: 1, team1Deltas: [9, 6, 3], team2Deltas: [-3, -6, -9]);
        await fixture.SeedRaceRosterReplayAsync(2, winnerTeam: 2, team1Deltas: [-3, -6, -9], team2Deltas: [3, 6, 9]);

        var rows = await fixture.Service.GetRaceRosterOverview(CreateRaceRosterRequest());
        var row = rows.Single(x => x.Race1 == Commander.Protoss && x.Race2 == Commander.Terran && x.Race3 == Commander.Zerg);

        Assert.AreEqual(2, row.Games);
        Assert.AreEqual(1, row.Wins);
        Assert.AreEqual(0.0, row.AverageRatingGain, 0.001);
        Assert.AreEqual(50.0, row.Winrate, 0.001);
    }

    [TestMethod]
    public async Task GetRaceRosterOverview_KeepsRosterOrderDistinct()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedRaceRosterReplayAsync(1, team1Races: [Commander.Protoss, Commander.Terran, Commander.Zerg]);
        await fixture.SeedRaceRosterReplayAsync(2, team1Races: [Commander.Terran, Commander.Protoss, Commander.Zerg]);

        var rows = await fixture.Service.GetRaceRosterOverview(CreateRaceRosterRequest());

        Assert.IsTrue(rows.Any(x => x.Race1 == Commander.Protoss && x.Race2 == Commander.Terran && x.Race3 == Commander.Zerg));
        Assert.IsTrue(rows.Any(x => x.Race1 == Commander.Terran && x.Race2 == Commander.Protoss && x.Race3 == Commander.Zerg));
    }

    [TestMethod]
    public async Task GetRaceRosterMatchups_AggregatesSelectedRosterVersusOpponentRoster()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedRaceRosterReplayAsync(1, winnerTeam: 1, team1Deltas: [9, 6, 3], team2Races: [Commander.Terran, Commander.Terran, Commander.Terran]);
        await fixture.SeedRaceRosterReplayAsync(2, winnerTeam: 2, team1Deltas: [-3, -6, -9], team2Races: [Commander.Terran, Commander.Terran, Commander.Terran]);
        await fixture.SeedRaceRosterReplayAsync(3, winnerTeam: 1, team2Races: [Commander.Zerg, Commander.Zerg, Commander.Zerg]);

        var rows = await fixture.Service.GetRaceRosterMatchups(new BuildDetailsRaceRosterMatchupRequest
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.None,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            Race1 = Commander.Protoss,
            Race2 = Commander.Terran,
            Race3 = Commander.Zerg,
        });
        var row = rows.Single(x => x.OpponentRace1 == Commander.Terran && x.OpponentRace2 == Commander.Terran && x.OpponentRace3 == Commander.Terran);

        Assert.AreEqual(2, row.Games);
        Assert.AreEqual(1, row.Wins);
        Assert.AreEqual(0.0, row.AverageRatingGain, 0.001);
        Assert.AreEqual(50.0, row.Winrate, 0.001);
    }

    [TestMethod]
    public async Task GetRaceRosterOverview_FiltersCommanderPlayerTeLeaversRatingAndStandardOnly()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedRaceRosterReplayAsync(1, te: true, team1PlayerIds: [42, 11, 12], team1Ratings: [1800, 1800, 1800]);
        await fixture.SeedRaceRosterReplayAsync(2, te: false, team1PlayerIds: [42, 21, 22], team1Ratings: [1800, 1800, 1800]);
        await fixture.SeedRaceRosterReplayAsync(3, leaverType: LeaverType.OneLeaver, team1PlayerIds: [42, 31, 32], team1Ratings: [1800, 1800, 1800]);
        await fixture.SeedRaceRosterReplayAsync(4, team1PlayerIds: [99, 41, 42], team1Ratings: [1300, 1300, 1300]);
        await fixture.SeedRaceRosterReplayAsync(5, gameMode: GameMode.Commanders);
        await fixture.SeedRaceRosterReplayAsync(6, playerCount: 4);
        await fixture.SeedRaceRosterReplayAsync(7, winnerTeam: 0);
        await fixture.SeedRaceRosterReplayAsync(8, team1Races: [Commander.Protoss, Commander.Abathur, Commander.Zerg]);

        var request = CreateRaceRosterRequest(BuildDetailsTeFilter.TE);
        request.Commander = Commander.Terran;
        request.Player = CreatePlayer(42);
        request.FromRating = 1500;
        var rows = await fixture.Service.GetRaceRosterOverview(request);

        Assert.AreEqual(1, rows.Single(x => x.Race1 == Commander.Protoss && x.Race2 == Commander.Terran && x.Race3 == Commander.Zerg).Games);

        request.Commander = Commander.Abathur;
        var nonStandardRows = await fixture.Service.GetRaceRosterOverview(request);
        Assert.AreEqual(0, nonStandardRows.Count);
    }

    [TestMethod]
    public async Task GetRaceRosterSampleReplays_ReturnsNewestDistinctReplayMetadata()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedRaceRosterReplayAsync(1);
        await fixture.SeedRaceRosterReplayAsync(2);
        await fixture.SeedRaceRosterReplayAsync(3);

        var rows = await fixture.Service.GetRaceRosterSampleReplays(new BuildDetailsRaceRosterSamplesRequest
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.None,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            Race1 = Commander.Protoss,
            Race2 = Commander.Terran,
            Race3 = Commander.Zerg,
            OpponentRace1 = Commander.Terran,
            OpponentRace2 = Commander.Terran,
            OpponentRace3 = Commander.Terran,
            Count = 2,
        });

        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("race-hash-3", rows[0].Replay.ReplayHash);
        Assert.AreEqual("race-hash-2", rows[1].Replay.ReplayHash);
        Assert.AreEqual(1, rows[0].Replay.PlayerPos);
        CollectionAssert.AreEqual(new[] { Commander.Protoss, Commander.Terran, Commander.Zerg }, rows[0].Replay.CommandersTeam1);
        CollectionAssert.AreEqual(new[] { Commander.Terran, Commander.Terran, Commander.Terran }, rows[0].Replay.CommandersTeam2);
    }

    private static BuildDetailsRequest CreateRequest(BuildDetailsTeFilter teFilter = BuildDetailsTeFilter.All)
    {
        return new()
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.Protoss,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            TeFilter = teFilter,
        };
    }

    private static BuildDetailsRequest CreateRaceRosterRequest(BuildDetailsTeFilter teFilter = BuildDetailsTeFilter.All)
    {
        return new()
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.None,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            TeFilter = teFilter,
        };
    }

    private static BuildDetailsMatchupRequest CreateMatchupRequest(BuildDetailsGasFilter gasFilter = BuildDetailsGasFilter.Any)
    {
        return new()
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.Protoss,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            GasFilter = gasFilter,
            SelectedCommander = Commander.Protoss,
            SelectedBuild = (int)ProtossBuild.Stalker,
        };
    }

    private static PlayerDto CreatePlayer(int playerId)
    {
        return new()
        {
            PlayerId = playerId,
            Name = $"Player{playerId}",
            ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = playerId }
        };
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(SqliteConnection connection, DsstatsContext context, BuildDetailsService service, IMemoryCache memoryCache)
        {
            Connection = connection;
            Context = context;
            Service = service;
            MemoryCache = memoryCache;
        }

        public SqliteConnection Connection { get; }
        public DsstatsContext Context { get; }
        public BuildDetailsService Service { get; }
        public IMemoryCache MemoryCache { get; }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<DsstatsContext>()
                .UseSqlite(connection, o => o.MigrationsAssembly("dsstats.migrations.sqlite"))
                .Options;

            var context = new DsstatsContext(options);
            var contextFactory = new TestDbContextFactory<DsstatsContext>(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new BuildDetailsService(contextFactory, cache);
            return new TestFixture(connection, context, service, cache);
        }

        public async Task SeedReplayAsync(
            int replayId,
            ProtossBuild selectedBuild,
            TerranBuild opponentBuild,
            bool selectedGasFirst,
            bool opponentGasFirst,
            bool won,
            double ratingDelta,
            LeaverType leaverType = LeaverType.None,
            bool te = true,
            int? selectedPlayerId = null,
            int? opponentPlayerId = null)
        {
            var ratingType = te ? RatingType.StandardTE : RatingType.Standard;
            var gametime = new DateTime(2026, 1, 1).AddDays(replayId);
            var selectedReplayPlayerId = replayId * 10 + 1;
            var opponentReplayPlayerId = replayId * 10 + 4;
            var selectedPersistentPlayerId = selectedPlayerId ?? selectedReplayPlayerId;
            var opponentPersistentPlayerId = opponentPlayerId ?? opponentReplayPlayerId;
            var replayRatingId = replayId * 100;
            var detailId = replayId * 1000;

            await AddPlayerIfMissing(selectedPersistentPlayerId, $"Protoss{selectedPersistentPlayerId}");
            await AddPlayerIfMissing(opponentPersistentPlayerId, $"Terran{opponentPersistentPlayerId}");

            Context.Replays.Add(new Replay
            {
                ReplayId = replayId,
                FileName = $"Replay-{replayId}.SC2Replay",
                Title = $"Replay {replayId}",
                Version = "1.0",
                GameMode = GameMode.Standard,
                RegionId = 1,
                TE = te,
                PlayerCount = 6,
                Gametime = gametime,
                Duration = 900,
                WinnerTeam = won ? 1 : 2,
                ReplayHash = $"hash-{replayId}",
                CompatHash = $"compat-{replayId}",
                Imported = gametime.AddMinutes(1),
                Uploaded = true
            });

            Context.ReplayPlayers.AddRange(
                new ReplayPlayer
                {
                    ReplayPlayerId = selectedReplayPlayerId,
                    ReplayId = replayId,
                    PlayerId = selectedPersistentPlayerId,
                    Name = $"Protoss{replayId}",
                    Race = Commander.Protoss,
                    SelectedRace = Commander.Protoss,
                    OppRace = Commander.Terran,
                    TeamId = 1,
                    GamePos = 1,
                    Duration = 900,
                    Result = won ? PlayerResult.Win : PlayerResult.Los
                },
                new ReplayPlayer
                {
                    ReplayPlayerId = opponentReplayPlayerId,
                    ReplayId = replayId,
                    PlayerId = opponentPersistentPlayerId,
                    Name = $"Terran{replayId}",
                    Race = Commander.Terran,
                    SelectedRace = Commander.Terran,
                    OppRace = Commander.Protoss,
                    TeamId = 2,
                    GamePos = 4,
                    Duration = 900,
                    Result = won ? PlayerResult.Los : PlayerResult.Win
                });

            Context.ReplayRatings.Add(new ReplayRating
            {
                ReplayRatingId = replayRatingId,
                RatingType = ratingType,
                LeaverType = leaverType,
                ExpectedWinProbability = 0.5,
                AvgRating = 1800 + replayId,
                ReplayId = replayId
            });

            Context.ReplayPlayerRatings.AddRange(
                new ReplayPlayerRating
                {
                    ReplayPlayerRatingId = replayId * 10000 + 1,
                    RatingType = ratingType,
                    RatingBefore = 1800,
                    RatingDelta = ratingDelta,
                    ExpectedDelta = 0,
                    Games = 10,
                    ReplayRatingId = replayRatingId,
                    ReplayPlayerId = selectedReplayPlayerId,
                    PlayerId = selectedPersistentPlayerId
                },
                new ReplayPlayerRating
                {
                    ReplayPlayerRatingId = replayId * 10000 + 4,
                    RatingType = ratingType,
                    RatingBefore = 1800,
                    RatingDelta = -ratingDelta,
                    ExpectedDelta = 0,
                    Games = 10,
                    ReplayRatingId = replayRatingId,
                    ReplayPlayerId = opponentReplayPlayerId,
                    PlayerId = opponentPersistentPlayerId
                });

            Context.ReplayBuildDetails.Add(new ReplayBuildDetail
            {
                ReplayBuildDetailId = detailId,
                ReplayId = replayId,
                DetectionVersion = 1,
                Status = ReplayBuildDetailStatus.Detected,
                CreatedAt = gametime,
                UpdatedAt = gametime,
                PlayerBuilds =
                [
                    new ReplayPlayerBuildDetail
                    {
                        GamePos = 1,
                        TeamId = 1,
                        Commander = Commander.Protoss,
                        Build = (int)selectedBuild,
                        GasFirst = selectedGasFirst,
                        Lane = 1,
                        OppGamePos = 4,
                        OppCommander = Commander.Terran,
                        OppBuild = (int)opponentBuild,
                        OppGasFirst = opponentGasFirst,
                        Won = won,
                        ReplayPlayerId = selectedReplayPlayerId,
                        OppReplayPlayerId = opponentReplayPlayerId
                    },
                    new ReplayPlayerBuildDetail
                    {
                        GamePos = 4,
                        TeamId = 2,
                        Commander = Commander.Terran,
                        Build = (int)opponentBuild,
                        GasFirst = opponentGasFirst,
                        Lane = 1,
                        OppGamePos = 1,
                        OppCommander = Commander.Protoss,
                        OppBuild = (int)selectedBuild,
                        OppGasFirst = selectedGasFirst,
                        Won = !won,
                        ReplayPlayerId = opponentReplayPlayerId,
                        OppReplayPlayerId = selectedReplayPlayerId
                    }
                ]
            });

            await Context.SaveChangesAsync();
        }

        public async Task SeedTeamBuildReplayAsync(
            int replayId,
            bool won,
            double leaderDelta,
            double followerDelta,
            LeaverType leaverType = LeaverType.None,
            bool te = true,
            int? leaderPlayerId = null,
            int? followerPlayerId = null)
        {
            var ratingType = te ? RatingType.StandardTE : RatingType.Standard;
            var gametime = new DateTime(2026, 2, 1).AddDays(replayId);
            var leaderReplayPlayerId = replayId * 100 + 1;
            var followerReplayPlayerId = replayId * 100 + 2;
            var leaderPersistentPlayerId = leaderPlayerId ?? leaderReplayPlayerId;
            var followerPersistentPlayerId = followerPlayerId ?? followerReplayPlayerId;
            var replayRatingId = replayId * 1000 + 50;
            var detailId = replayId * 10000 + 50;

            await AddPlayerIfMissing(leaderPersistentPlayerId, $"Leader{leaderPersistentPlayerId}");
            await AddPlayerIfMissing(followerPersistentPlayerId, $"Follower{followerPersistentPlayerId}");

            Context.Replays.Add(new Replay
            {
                ReplayId = replayId,
                FileName = $"TeamReplay-{replayId}.SC2Replay",
                Title = $"Team Replay {replayId}",
                Version = "1.0",
                GameMode = GameMode.Standard,
                RegionId = 1,
                TE = te,
                PlayerCount = 6,
                Gametime = gametime,
                Duration = 900,
                WinnerTeam = won ? 1 : 2,
                ReplayHash = $"team-hash-{replayId}",
                CompatHash = $"team-compat-{replayId}",
                Imported = gametime.AddMinutes(1),
                Uploaded = true
            });

            Context.ReplayPlayers.AddRange(
                new ReplayPlayer
                {
                    ReplayPlayerId = leaderReplayPlayerId,
                    ReplayId = replayId,
                    PlayerId = leaderPersistentPlayerId,
                    Name = $"Leader{replayId}",
                    Race = Commander.Protoss,
                    SelectedRace = Commander.Protoss,
                    TeamId = 1,
                    GamePos = 1,
                    Duration = 900,
                    Result = won ? PlayerResult.Win : PlayerResult.Los
                },
                new ReplayPlayer
                {
                    ReplayPlayerId = followerReplayPlayerId,
                    ReplayId = replayId,
                    PlayerId = followerPersistentPlayerId,
                    Name = $"Follower{replayId}",
                    Race = Commander.Terran,
                    SelectedRace = Commander.Terran,
                    TeamId = 1,
                    GamePos = 2,
                    Duration = 900,
                    Result = won ? PlayerResult.Win : PlayerResult.Los
                });

            Context.ReplayRatings.Add(new ReplayRating
            {
                ReplayRatingId = replayRatingId,
                RatingType = ratingType,
                LeaverType = leaverType,
                ExpectedWinProbability = 0.5,
                AvgRating = 1800 + replayId,
                ReplayId = replayId
            });

            Context.ReplayPlayerRatings.AddRange(
                new ReplayPlayerRating
                {
                    ReplayPlayerRatingId = replayId * 100000 + 1,
                    RatingType = ratingType,
                    RatingBefore = 1800,
                    RatingDelta = leaderDelta,
                    ExpectedDelta = 0,
                    Games = 10,
                    ReplayRatingId = replayRatingId,
                    ReplayPlayerId = leaderReplayPlayerId,
                    PlayerId = leaderPersistentPlayerId
                },
                new ReplayPlayerRating
                {
                    ReplayPlayerRatingId = replayId * 100000 + 2,
                    RatingType = ratingType,
                    RatingBefore = 1800,
                    RatingDelta = followerDelta,
                    ExpectedDelta = 0,
                    Games = 10,
                    ReplayRatingId = replayRatingId,
                    ReplayPlayerId = followerReplayPlayerId,
                    PlayerId = followerPersistentPlayerId
                });

            Context.ReplayBuildDetails.Add(new ReplayBuildDetail
            {
                ReplayBuildDetailId = detailId,
                ReplayId = replayId,
                DetectionVersion = 1,
                Status = ReplayBuildDetailStatus.Detected,
                CreatedAt = gametime,
                UpdatedAt = gametime,
                PlayerBuilds =
                [
                    new ReplayPlayerBuildDetail
                    {
                        GamePos = 1,
                        TeamId = 1,
                        Commander = Commander.Protoss,
                        Build = (int)ProtossBuild.Stalker,
                        GasFirst = false,
                        Lane = 1,
                        Won = won,
                        ReplayPlayerId = leaderReplayPlayerId,
                        OppReplayPlayerId = followerReplayPlayerId
                    },
                    new ReplayPlayerBuildDetail
                    {
                        GamePos = 2,
                        TeamId = 1,
                        Commander = Commander.Terran,
                        Build = (int)TerranBuild.Bio,
                        GasFirst = true,
                        Lane = 2,
                        Won = won,
                        ReplayPlayerId = followerReplayPlayerId,
                        OppReplayPlayerId = leaderReplayPlayerId
                    }
                ],
                TeamBuilds =
                [
                    new ReplayTeamBuildDetail
                    {
                        TeamId = 1,
                        TeamBuild = TeamBuild.PTStack,
                        LeaderReplayPlayerId = leaderReplayPlayerId,
                        FollowerReplayPlayerId = followerReplayPlayerId
                    }
                ]
            });

            await Context.SaveChangesAsync();
        }

        public async Task SeedRaceRosterReplayAsync(
            int replayId,
            Commander[]? team1Races = null,
            Commander[]? team2Races = null,
            int winnerTeam = 1,
            double[]? team1Deltas = null,
            double[]? team2Deltas = null,
            double[]? team1Ratings = null,
            double[]? team2Ratings = null,
            int[]? team1PlayerIds = null,
            int[]? team2PlayerIds = null,
            LeaverType leaverType = LeaverType.None,
            bool te = true,
            int playerCount = 6,
            GameMode gameMode = GameMode.Standard)
        {
            team1Races ??= [Commander.Protoss, Commander.Terran, Commander.Zerg];
            team2Races ??= [Commander.Terran, Commander.Terran, Commander.Terran];
            team1Deltas ??= [6.0, 6.0, 6.0];
            team2Deltas ??= [-6.0, -6.0, -6.0];
            team1Ratings ??= [1800.0, 1800.0, 1800.0];
            team2Ratings ??= [1800.0, 1800.0, 1800.0];
            team1PlayerIds ??= [replayId * 1000 + 1, replayId * 1000 + 2, replayId * 1000 + 3];
            team2PlayerIds ??= [replayId * 1000 + 4, replayId * 1000 + 5, replayId * 1000 + 6];

            var ratingType = te ? RatingType.StandardTE : RatingType.Standard;
            var gametime = new DateTime(2026, 3, 1).AddDays(replayId);
            var replayRatingId = replayId * 1000 + 500;

            for (var i = 0; i < 3; i++)
            {
                await AddPlayerIfMissing(team1PlayerIds[i], $"RaceTeam1Player{team1PlayerIds[i]}");
                await AddPlayerIfMissing(team2PlayerIds[i], $"RaceTeam2Player{team2PlayerIds[i]}");
            }

            Context.Replays.Add(new Replay
            {
                ReplayId = replayId,
                FileName = $"RaceReplay-{replayId}.SC2Replay",
                Title = $"Race Replay {replayId}",
                Version = "1.0",
                GameMode = gameMode,
                RegionId = 1,
                TE = te,
                PlayerCount = playerCount,
                Gametime = gametime,
                Duration = 900,
                WinnerTeam = winnerTeam,
                ReplayHash = $"race-hash-{replayId}",
                CompatHash = $"race-compat-{replayId}",
                Imported = gametime.AddMinutes(1),
                Uploaded = true
            });

            for (var i = 0; i < 3; i++)
            {
                var team1ReplayPlayerId = replayId * 10000 + i + 1;
                var team2ReplayPlayerId = replayId * 10000 + i + 4;

                Context.ReplayPlayers.AddRange(
                    new ReplayPlayer
                    {
                        ReplayPlayerId = team1ReplayPlayerId,
                        ReplayId = replayId,
                        PlayerId = team1PlayerIds[i],
                        Name = $"RaceTeam1Player{replayId}-{i}",
                        Race = team1Races[i],
                        SelectedRace = team1Races[i],
                        TeamId = 1,
                        GamePos = i + 1,
                        Duration = 900,
                        Result = winnerTeam == 1 ? PlayerResult.Win : PlayerResult.Los
                    },
                    new ReplayPlayer
                    {
                        ReplayPlayerId = team2ReplayPlayerId,
                        ReplayId = replayId,
                        PlayerId = team2PlayerIds[i],
                        Name = $"RaceTeam2Player{replayId}-{i}",
                        Race = team2Races[i],
                        SelectedRace = team2Races[i],
                        TeamId = 2,
                        GamePos = i + 4,
                        Duration = 900,
                        Result = winnerTeam == 2 ? PlayerResult.Win : PlayerResult.Los
                    });

                Context.ReplayPlayerRatings.AddRange(
                    new ReplayPlayerRating
                    {
                        ReplayPlayerRatingId = replayId * 100000 + i + 1,
                        RatingType = ratingType,
                        RatingBefore = team1Ratings[i],
                        RatingDelta = team1Deltas[i],
                        ExpectedDelta = 0,
                        Games = 10,
                        ReplayRatingId = replayRatingId,
                        ReplayPlayerId = team1ReplayPlayerId,
                        PlayerId = team1PlayerIds[i]
                    },
                    new ReplayPlayerRating
                    {
                        ReplayPlayerRatingId = replayId * 100000 + i + 4,
                        RatingType = ratingType,
                        RatingBefore = team2Ratings[i],
                        RatingDelta = team2Deltas[i],
                        ExpectedDelta = 0,
                        Games = 10,
                        ReplayRatingId = replayRatingId,
                        ReplayPlayerId = team2ReplayPlayerId,
                        PlayerId = team2PlayerIds[i]
                    });
            }

            Context.ReplayRatings.Add(new ReplayRating
            {
                ReplayRatingId = replayRatingId,
                RatingType = ratingType,
                LeaverType = leaverType,
                ExpectedWinProbability = 0.5,
                AvgRating = 1800 + replayId,
                ReplayId = replayId
            });

            await Context.SaveChangesAsync();
        }

        private async Task AddPlayerIfMissing(int playerId, string name)
        {
            if (Context.Players.Local.Any(x => x.PlayerId == playerId)
                || await Context.Players.AnyAsync(x => x.PlayerId == playerId))
            {
                return;
            }

            Context.Players.Add(new Player
            {
                PlayerId = playerId,
                Name = name,
                ToonId = new ToonId { Region = 1, Realm = 1, Id = playerId }
            });
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
            (MemoryCache as MemoryCache)?.Dispose();
        }
    }
}
