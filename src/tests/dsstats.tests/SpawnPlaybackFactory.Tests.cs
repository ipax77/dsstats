using System.Collections.ObjectModel;
using dsstats.play;
using Sc2DirectStrike.Parser;

namespace dsstats.tests;

[TestClass]
public sealed class SpawnPlaybackFactoryTests
{
    [TestMethod]
    public void Create_PreservesPlayersAndUnitLifeCycle()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(60),
            Players = new ReadOnlyCollection<DirectStrikePlayer>(
            [
                new DirectStrikePlayer
                {
                    Name = "Player One",
                    TeamId = 1,
                    GamePos = 1,
                    Commander = Commander.Terran,
                    Spawns = new ReadOnlyCollection<DirectStrikePlayerSpawn>(
                    [
                        new DirectStrikePlayerSpawn
                        {
                            Number = 1,
                            StartGameloop = 100,
                            EndGameloop = 220,
                            Units = new ReadOnlyCollection<DirectStrikeSpawnUnit>(
                            [
                                new DirectStrikeSpawnUnit
                                {
                                    UnitIndex = 42,
                                    Name = "Marine",
                                    Gameloop = 112,
                                    X = 170,
                                    Y = 160,
                                    DiedGameloop = 190,
                                    DiedX = 130,
                                    DiedY = 120,
                                }
                            ])
                        }
                    ])
                }
            ])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        Assert.AreEqual(1, playback.Players.Count);
        Assert.AreEqual(1, playback.Stats.PlayerCount);
        Assert.AreEqual(1, playback.Stats.SpawnCount);
        Assert.AreEqual(1, playback.Stats.UnitCount);
        Assert.AreEqual(1, playback.Stats.UnitsWithDiedEvent);
        Assert.AreEqual(1, playback.Stats.UnitsWithDiedPosition);
        Assert.AreEqual(1, playback.Players[0].TeamId);
        Assert.AreEqual("Player One", playback.Players[0].Name);
        Assert.AreEqual("Terran", playback.Players[0].Commander);
        Assert.AreEqual(0, playback.Players[0].RefineryGameloops.Count);
        Assert.AreEqual(0, playback.Players[0].TierUpgradeGameloops.Count);
        CollectionAssert.AreEquivalent(
            new[] { "Nexus", "Cannon", "Bunker", "Planetary" },
            playback.Landmarks.Select(landmark => landmark.Name).ToArray());
        Assert.IsTrue(playback.Landmarks.All(landmark => landmark.Kills == 0));
        Assert.IsTrue(playback.Landmarks.All(landmark => landmark.DiedGameloop is null));
        Assert.AreEqual(0, playback.MiddleControl.FirstTeamId);
        Assert.AreEqual(0, playback.MiddleControl.ChangeGameloops.Count);
        Assert.AreEqual(1, playback.Snapshots.Count);
        Assert.AreEqual(1, playback.Snapshots[0].SpawnNumber);
        Assert.AreEqual(100, playback.Snapshots[0].StartGameloop);
        Assert.AreEqual(220, playback.Snapshots[0].EndGameloop);

        var unit = playback.Players[0].Units.Single();
        Assert.AreEqual(42, unit.UnitIndex);
        Assert.AreEqual("Marine", unit.Name);
        Assert.AreEqual(1, unit.SpawnNumber);
        Assert.AreEqual(112, unit.SpawnGameloop);
        Assert.AreEqual(170, unit.SpawnX);
        Assert.AreEqual(160, unit.SpawnY);
        Assert.AreEqual(190, unit.DiedGameloop);
        Assert.AreEqual(130, unit.DiedX);
        Assert.AreEqual(120, unit.DiedY);
        Assert.AreEqual(130, unit.TargetX);
        Assert.AreEqual(120, unit.TargetY);
        Assert.AreEqual(190, unit.ExpiresGameloop);
        Assert.AreEqual(0, unit.KillGameloops.Count);
        Assert.IsTrue(unit.Radius > 0);
        StringAssert.StartsWith(unit.Color, "#");
    }

    [TestMethod]
    public void Create_MirrorsTargetForUnitWithoutDeathPosition()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(10),
            Players = new ReadOnlyCollection<DirectStrikePlayer>(
            [
                new DirectStrikePlayer
                {
                    Name = "Player Two",
                    TeamId = 2,
                    GamePos = 4,
                    Commander = Commander.Protoss,
                    Spawns = new ReadOnlyCollection<DirectStrikePlayerSpawn>(
                    [
                        new DirectStrikePlayerSpawn
                        {
                            Number = 1,
                            StartGameloop = 0,
                            EndGameloop = 112,
                            Units = new ReadOnlyCollection<DirectStrikeSpawnUnit>(
                            [
                                new DirectStrikeSpawnUnit
                                {
                                    UnitIndex = 7,
                                    Name = "Zealot",
                                    Gameloop = 20,
                                    X = 40,
                                    Y = 50,
                                }
                            ])
                        }
                    ])
                }
            ])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        var unit = playback.Players[0].Units.Single();
        Assert.IsNull(unit.DiedGameloop);
        Assert.AreEqual(20 + SpawnPlaybackFactory.MaxUnitLifetimeGameloops, unit.ExpiresGameloop);
        Assert.AreEqual(0, playback.Stats.UnitsWithDiedEvent);
        Assert.AreEqual(0, playback.Stats.UnitsWithDiedPosition);
        Assert.AreEqual(SpawnPlaybackFactory.MapWidth - 40, unit.TargetX);
        Assert.AreEqual(SpawnPlaybackFactory.MapHeight - 50, unit.TargetY);
    }

    [TestMethod]
    public void Create_ProducesStableBoundsAndStepMetadata()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(1),
            Players = new ReadOnlyCollection<DirectStrikePlayer>(
            [
                new DirectStrikePlayer
                {
                    Name = "Player Three",
                    TeamId = 1,
                    GamePos = 2,
                    Commander = Commander.Zerg,
                    Spawns = new ReadOnlyCollection<DirectStrikePlayerSpawn>(
                    [
                        new DirectStrikePlayerSpawn
                        {
                            Number = 1,
                            StartGameloop = 0,
                            EndGameloop = 500,
                            Units = new ReadOnlyCollection<DirectStrikeSpawnUnit>(
                            [
                                new DirectStrikeSpawnUnit
                                {
                                    UnitIndex = 1,
                                    Name = "Zergling",
                                    Gameloop = 25,
                                    X = 10,
                                    Y = 20,
                                    DiedGameloop = 600,
                                    DiedX = 240,
                                    DiedY = 210,
                                }
                            ])
                        }
                    ])
                }
            ])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        Assert.AreEqual(112, playback.StepGameloops);
        Assert.AreEqual(600, playback.DurationGameloop);
        Assert.AreEqual(1, playback.Stats.MaxSimultaneousActiveSpawns);
        Assert.IsTrue(playback.Bounds.MinX <= 10);
        Assert.IsTrue(playback.Bounds.MinY <= 20);
        Assert.IsTrue(playback.Bounds.MaxX >= 240);
        Assert.IsTrue(playback.Bounds.MaxY >= 210);
    }

    [TestMethod]
    public void Create_UsesProvidedObjectiveLandmarksInBounds()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(1),
            Players = new ReadOnlyCollection<DirectStrikePlayer>([])
        };
        SpawnPlaybackLandmark[] landmarks =
        [
            new("Nexus", "Base", 1, 250, 235, 14, "#5DADEC", 3),
            new("Planetary", "Base", 2, 6, 5, 14, "#F87171", 5),
        ];

        var playback = SpawnPlaybackFactory.Create(replay, landmarks);

        Assert.AreSame(landmarks, playback.Landmarks);
        Assert.AreEqual(3, playback.Landmarks[0].Kills);
        Assert.AreEqual(5, playback.Landmarks[1].Kills);
        Assert.AreEqual(0, playback.Stats.UnitCount);
        Assert.IsTrue(playback.Bounds.MinX <= 6);
        Assert.IsTrue(playback.Bounds.MinY <= 5);
        Assert.IsTrue(playback.Bounds.MaxX >= 250);
        Assert.IsTrue(playback.Bounds.MaxY >= 235);
    }

    [TestMethod]
    public void Create_PlacesDefaultLandmarksAtFixedObjectivePositions()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(1),
            Players = new ReadOnlyCollection<DirectStrikePlayer>([])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        AssertLandmarksAreAtFixedObjectivePositions(playback.Landmarks);
    }

    [TestMethod]
    public void CreateFromReplayDto_PlacesLandmarksAtFixedObjectivePositions()
    {
        var replay = new dsstats.shared.ReplayDto
        {
            Players =
            [
                new()
                {
                    Name = "Team 1",
                    GamePos = 1,
                    TeamId = 1,
                    Race = dsstats.shared.Commander.Terran
                },
                new()
                {
                    Name = "Team 2",
                    GamePos = 4,
                    TeamId = 2,
                    Race = dsstats.shared.Commander.Terran
                }
            ]
        };
        var sidecar = new dsstats.shared.SpawnPlaybackSidecarDto(1, 112, [], []);

        var playback = SpawnPlaybackFactory.Create(replay, sidecar);

        AssertLandmarksAreAtFixedObjectivePositions(playback.Landmarks);
    }

    [TestMethod]
    public void CreateFromReplayDto_PreservesPlayerTierUpgradesAsGameloops()
    {
        var replay = new dsstats.shared.ReplayDto
        {
            Players =
            [
                new()
                {
                    Name = "Tier Player",
                    GamePos = 1,
                    TeamId = 1,
                    Race = dsstats.shared.Commander.Terran,
                    TierUpgrades = [45, 120]
                }
            ]
        };
        var sidecar = new dsstats.shared.SpawnPlaybackSidecarDto(
            1,
            112,
            [
                new(
                    1,
                    [
                        new(42, "Marine", 1, 112, 170, 160, null, null, null, [])
                    ])
            ],
            []);

        var playback = SpawnPlaybackFactory.Create(replay, sidecar);

        CollectionAssert.AreEqual(
            new[]
            {
                (int)Math.Round(45 * SpawnPlaybackFactory.GameloopsPerSecond),
                (int)Math.Round(120 * SpawnPlaybackFactory.GameloopsPerSecond),
            },
            playback.Players[0].TierUpgradeGameloops.ToArray());
    }

    [TestMethod]
    public void Create_PreservesProvidedUnitKillGameloops()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(10),
            Players = new ReadOnlyCollection<DirectStrikePlayer>(
            [
                new DirectStrikePlayer
                {
                    Name = "Player One",
                    TeamId = 1,
                    GamePos = 1,
                    Commander = Commander.Terran,
                    Spawns = new ReadOnlyCollection<DirectStrikePlayerSpawn>(
                    [
                        new DirectStrikePlayerSpawn
                        {
                            Number = 1,
                            StartGameloop = 100,
                            EndGameloop = 112,
                            Units = new ReadOnlyCollection<DirectStrikeSpawnUnit>(
                            [
                                new DirectStrikeSpawnUnit
                                {
                                    UnitIndex = 42,
                                    Name = "Marine",
                                    Gameloop = 112,
                                    X = 170,
                                    Y = 160,
                                }
                            ])
                        }
                    ])
                }
            ])
        };
        var unitKey = SpawnPlaybackFactory.GetUnitKey(42, 112, 170, 160, "Marine");
        Dictionary<SpawnPlaybackUnitKey, IReadOnlyList<int>> unitKillGameloops = new()
        {
            [unitKey] = [180, 140, 160]
        };

        var playback = SpawnPlaybackFactory.Create(replay, unitKillGameloops: unitKillGameloops);

        CollectionAssert.AreEqual(
            new[] { 140, 160, 180 },
            playback.Players[0].Units.Single().KillGameloops.ToArray());
    }

    [TestMethod]
    public void Create_PreservesPlayerRefineryTimesAsGameloops()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(30),
            Players = new ReadOnlyCollection<DirectStrikePlayer>(
            [
                new DirectStrikePlayer
                {
                    Name = "Gas Player",
                    TeamId = 1,
                    GamePos = 2,
                    Commander = Commander.Terran,
                    RefineryTimes =
                    [
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(12),
                    ],
                    Spawns = new ReadOnlyCollection<DirectStrikePlayerSpawn>([])
                }
            ])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        CollectionAssert.AreEqual(
            new[]
            {
                (int)Math.Round(5 * SpawnPlaybackFactory.GameloopsPerSecond),
                (int)Math.Round(12 * SpawnPlaybackFactory.GameloopsPerSecond),
            },
            playback.Players[0].RefineryGameloops.ToArray());
    }

    [TestMethod]
    public void Create_PreservesMiddleControlChangesAsGameloops()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(10),
            FirstMiddleControlTeam = 2,
            MiddleChanges =
            [
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(8),
            ],
            Players = new ReadOnlyCollection<DirectStrikePlayer>([])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        Assert.AreEqual(2, playback.MiddleControl.FirstTeamId);
        CollectionAssert.AreEqual(
            new[]
            {
                (int)Math.Round(3 * SpawnPlaybackFactory.GameloopsPerSecond),
                (int)Math.Round(8 * SpawnPlaybackFactory.GameloopsPerSecond),
            },
            playback.MiddleControl.ChangeGameloops.ToArray());
    }

    [TestMethod]
    public void Create_IgnoresInvalidMiddleControl()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(10),
            FirstMiddleControlTeam = 3,
            MiddleChanges = [TimeSpan.FromSeconds(3)],
            Players = new ReadOnlyCollection<DirectStrikePlayer>([])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        Assert.AreEqual(0, playback.MiddleControl.FirstTeamId);
        Assert.AreEqual(0, playback.MiddleControl.ChangeGameloops.Count);

        replay.FirstMiddleControlTeam = 1;
        replay.MiddleChanges = [];

        playback = SpawnPlaybackFactory.Create(replay);

        Assert.AreEqual(0, playback.MiddleControl.FirstTeamId);
        Assert.AreEqual(0, playback.MiddleControl.ChangeGameloops.Count);
    }

    [TestMethod]
    public void Create_CreatesPairedSnapshotsAtCompletedSpawnEnd()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(10),
            Players = new ReadOnlyCollection<DirectStrikePlayer>(
            [
                CreatePlayer("Team 1", 1, 1, CreateSpawn(1, 100, 140, "Zergling", 112)),
                CreatePlayer("Team 2", 2, 4, CreateSpawn(1, 104, 170, "Marine", 126)),
            ])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        Assert.AreEqual(1, playback.Snapshots.Count);
        Assert.AreEqual(1, playback.Snapshots[0].SpawnNumber);
        Assert.AreEqual(100, playback.Snapshots[0].StartGameloop);
        Assert.AreEqual(170, playback.Snapshots[0].EndGameloop);
        Assert.AreEqual(112, playback.Players.Single(player => player.TeamId == 1).Units.Single().SpawnGameloop);
        Assert.AreEqual(126, playback.Players.Single(player => player.TeamId == 2).Units.Single().SpawnGameloop);
    }

    [TestMethod]
    public void Create_KeepsUnpairedSnapshots()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(10),
            Players = new ReadOnlyCollection<DirectStrikePlayer>(
            [
                CreatePlayer("Team 1", 1, 1, CreateSpawn(1, 200, 260, "Marine", 212)),
            ])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        Assert.AreEqual(1, playback.Snapshots.Count);
        Assert.AreEqual(1, playback.Snapshots[0].SpawnNumber);
        Assert.AreEqual(200, playback.Snapshots[0].StartGameloop);
        Assert.AreEqual(260, playback.Snapshots[0].EndGameloop);
    }

    [TestMethod]
    public void Create_DoesNotPairSpawnsWithLargeStartOffset()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(120),
            Players = new ReadOnlyCollection<DirectStrikePlayer>(
            [
                CreatePlayer("Team 1", 1, 1, CreateSpawn(1, 2400, 2400, "Zergling", 2400)),
                CreatePlayer("Team 2", 2, 4, CreateSpawn(1, 960, 960, "Marine", 960)),
            ])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        Assert.AreEqual(2, playback.Snapshots.Count);
        CollectionAssert.AreEqual(
            new[] { 960, 2400 },
            playback.Snapshots.Select(snapshot => snapshot.StartGameloop).ToArray());
        CollectionAssert.AreEqual(
            new[] { 960, 2400 },
            playback.Snapshots.Select(snapshot => snapshot.EndGameloop).ToArray());
    }

    [TestMethod]
    public void Create_SortsSnapshotsByStartThenEndThenSpawnNumber()
    {
        var replay = new DirectStrikeReplay
        {
            Duration = TimeSpan.FromSeconds(10),
            Players = new ReadOnlyCollection<DirectStrikePlayer>(
            [
                CreatePlayer(
                    "Team 1",
                    1,
                    1,
                    CreateSpawn(1, 300, 340, "Marine", 320),
                    CreateSpawn(2, 100, 220, "Marauder", 140),
                    CreateSpawn(3, 100, 180, "Reaper", 150)),
            ])
        };

        var playback = SpawnPlaybackFactory.Create(replay);

        CollectionAssert.AreEqual(
            new[] { 3, 2, 1 },
            playback.Snapshots.Select(snapshot => snapshot.SpawnNumber).ToArray());
        CollectionAssert.AreEqual(
            new[] { 100, 100, 300 },
            playback.Snapshots.Select(snapshot => snapshot.StartGameloop).ToArray());
        CollectionAssert.AreEqual(
            new[] { 180, 220, 340 },
            playback.Snapshots.Select(snapshot => snapshot.EndGameloop).ToArray());
    }

    private static DirectStrikePlayer CreatePlayer(
        string name,
        int teamId,
        int gamePos,
        params DirectStrikePlayerSpawn[] spawns)
    {
        return new()
        {
            Name = name,
            TeamId = teamId,
            GamePos = gamePos,
            Commander = Commander.Terran,
            Spawns = new ReadOnlyCollection<DirectStrikePlayerSpawn>(spawns)
        };
    }

    private static DirectStrikePlayerSpawn CreateSpawn(
        int number,
        int startGameloop,
        int endGameloop,
        string unitName,
        int unitGameloop)
    {
        return new()
        {
            Number = number,
            StartGameloop = startGameloop,
            EndGameloop = endGameloop,
            Units = new ReadOnlyCollection<DirectStrikeSpawnUnit>(
            [
                new()
                {
                    UnitIndex = number,
                    Name = unitName,
                    Gameloop = unitGameloop,
                    X = 100 + number,
                    Y = 100 + number,
                }
            ])
        };
    }

    private static void AssertLandmarksAreAtFixedObjectivePositions(IReadOnlyList<SpawnPlaybackLandmark> landmarks)
    {
        var nexus = landmarks.Single(landmark => landmark.Name == "Nexus");
        var planetary = landmarks.Single(landmark => landmark.Name == "Planetary");
        var bunker = landmarks.Single(landmark => landmark.Name == "Bunker");
        var cannon = landmarks.Single(landmark => landmark.Name == "Cannon");

        Assert.AreEqual(1, planetary.TeamId);
        Assert.AreEqual(160, planetary.X);
        Assert.AreEqual(152, planetary.Y);
        Assert.AreEqual(1, bunker.TeamId);
        Assert.AreEqual(146, bunker.X);
        Assert.AreEqual(138, bunker.Y);
        Assert.AreEqual(2, nexus.TeamId);
        Assert.AreEqual(96, nexus.X);
        Assert.AreEqual(88, nexus.Y);
        Assert.AreEqual(2, cannon.TeamId);
        Assert.AreEqual(110, cannon.X);
        Assert.AreEqual(102, cannon.Y);
        Assert.AreEqual(SpawnPlaybackFactory.MapWidth - planetary.X, nexus.X);
        Assert.AreEqual(SpawnPlaybackFactory.MapHeight - planetary.Y, nexus.Y);
        Assert.AreEqual(SpawnPlaybackFactory.MapWidth - bunker.X, cannon.X);
        Assert.AreEqual(SpawnPlaybackFactory.MapHeight - bunker.Y, cannon.Y);
    }
}
