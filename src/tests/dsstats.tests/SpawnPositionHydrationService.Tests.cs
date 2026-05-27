using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.weblib.Replays;

namespace dsstats.tests;

[TestClass]
public sealed class SpawnPositionHydrationServiceTests
{
    [TestMethod]
    public async Task EnsureChartSpawnAsync_SidecarFillsRequestedPositions()
    {
        var repository = new TestReplayRepository
        {
            SpawnPlaybackPayload = SpawnPlaybackSidecarCodec.Encode(CreateSidecar("Marine"))
        };
        var service = CreateService();
        var replayDetails = CreateReplayDetails();

        var spawn = await service.EnsureChartSpawnAsync(replayDetails, 1, Breakpoint.Min5, repository);

        Assert.IsNotNull(spawn);
        CollectionAssert.AreEqual(new[] { 165, 174, 166, 173 }, spawn.Units[0].Positions);
        Assert.AreEqual(1, repository.SpawnPlaybackCalls);
        Assert.AreEqual(0, repository.SpawnPositionCalls);
    }

    [TestMethod]
    public async Task EnsureChartSpawnAsync_IncompleteSidecarFallsBackToStoredPositions()
    {
        var repository = new TestReplayRepository
        {
            SpawnPlaybackPayload = SpawnPlaybackSidecarCodec.Encode(CreateSidecar("Marauder")),
            SpawnPositions = CreateStoredPositions()
        };
        var service = CreateService();
        var replayDetails = CreateReplayDetails();

        var spawn = await service.EnsureChartSpawnAsync(replayDetails, 1, Breakpoint.Min5, repository);

        Assert.IsNotNull(spawn);
        CollectionAssert.AreEqual(new[] { 170, 171, 172, 173 }, spawn.Units[0].Positions);
        Assert.AreEqual(1, repository.SpawnPlaybackCalls);
        Assert.AreEqual(1, repository.SpawnPositionCalls);
    }

    [TestMethod]
    public async Task EnsureChartSpawnAsync_RepeatedReplayRequestsReuseCachedWork()
    {
        var repository = new TestReplayRepository
        {
            SpawnPlaybackPayload = SpawnPlaybackSidecarCodec.Encode(CreateSidecar("Marauder")),
            SpawnPositions = CreateStoredPositions()
        };
        var service = CreateService();

        var first = await service.EnsureChartSpawnAsync(CreateReplayDetails(), 1, Breakpoint.Min5, repository);
        var second = await service.EnsureChartSpawnAsync(CreateReplayDetails(), 1, Breakpoint.Min5, repository);

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        CollectionAssert.AreEqual(new[] { 170, 171, 172, 173 }, second.Units[0].Positions);
        Assert.AreEqual(1, repository.SpawnPlaybackCalls);
        Assert.AreEqual(1, repository.SpawnPositionCalls);
    }

    [TestMethod]
    public async Task EnsureChartSpawnAsync_CreatesChartSpawnWhenBreakpointSpawnIsMissing()
    {
        var repository = new TestReplayRepository
        {
            SpawnPlaybackPayload = SpawnPlaybackSidecarCodec.Encode(CreateMin15Sidecar())
        };
        var service = CreateService();
        var replayDetails = CreateReplayDetails();

        var spawn = await service.EnsureChartSpawnAsync(replayDetails, 1, Breakpoint.Min15, repository);

        Assert.IsNotNull(spawn);
        Assert.AreEqual(Breakpoint.Min15, spawn.Breakpoint);
        Assert.AreEqual("Stalker", spawn.Units[0].Name);
        Assert.AreEqual(2, spawn.Units[0].Count);
        CollectionAssert.AreEqual(new[] { 165, 174, 166, 173 }, spawn.Units[0].Positions);
        Assert.AreEqual(1, repository.SpawnPlaybackCalls);
        Assert.AreEqual(0, repository.SpawnPositionCalls);
    }

    [TestMethod]
    public void ProjectSpawnWaves_MultipleSpawnsAreSortedAndUnitsAreGrouped()
    {
        var waves = SpawnPositionHydrationService.ProjectSpawnWaves(CreateWaveSidecar(), 1);

        Assert.AreEqual(2, waves.Count);
        Assert.AreEqual(1, waves[0].SpawnNumber);
        Assert.AreEqual(120, waves[0].StartGameloop);
        Assert.AreEqual(120, waves[0].EndGameloop);
        Assert.AreEqual(2, waves[1].SpawnNumber);
        Assert.AreEqual(300, waves[1].StartGameloop);
        Assert.AreEqual(302, waves[1].EndGameloop);

        var marine = waves[1].Spawn.Units.Single(unit => unit.Name == "Marine");
        Assert.AreEqual(2, marine.Count);
        CollectionAssert.AreEqual(new[] { 165, 174, 166, 173 }, marine.Positions);

        var marauder = waves[1].Spawn.Units.Single(unit => unit.Name == "Marauder");
        Assert.AreEqual(1, marauder.Count);
        CollectionAssert.AreEqual(new[] { 167, 172 }, marauder.Positions);
    }

    [TestMethod]
    public void ProjectSpawnWaves_MissingPlayerReturnsEmptyList()
    {
        var waves = SpawnPositionHydrationService.ProjectSpawnWaves(CreateWaveSidecar(), 3);

        Assert.AreEqual(0, waves.Count);
    }

    [TestMethod]
    public void ProjectSpawnWaves_UsesPlayerSpecificUnitTimesWhenSpawnNumbersOverlap()
    {
        var sidecar = CreateWaveSidecar();

        var playerOneWaves = SpawnPositionHydrationService.ProjectSpawnWaves(sidecar, 1);
        var playerTwoWaves = SpawnPositionHydrationService.ProjectSpawnWaves(sidecar, 2);

        Assert.AreEqual(120, playerOneWaves[0].StartGameloop);
        Assert.AreEqual(110, playerTwoWaves[0].StartGameloop);
    }

    [TestMethod]
    public async Task GetSpawnWavesAsync_RepeatedReplayPlayerRequestsReuseCachedSidecar()
    {
        var repository = new TestReplayRepository
        {
            SpawnPlaybackPayload = SpawnPlaybackSidecarCodec.Encode(CreateWaveSidecar())
        };
        var service = CreateService();
        var replayDetails = CreateReplayDetails();

        var first = await service.GetSpawnWavesAsync(replayDetails, 1, repository);
        var second = await service.GetSpawnWavesAsync(replayDetails, 1, repository);

        Assert.AreEqual(2, first.Count);
        Assert.AreEqual(2, second.Count);
        Assert.AreEqual(1, repository.SpawnPlaybackCalls);
        Assert.AreEqual(0, repository.SpawnPositionCalls);
    }

    private static SpawnPositionHydrationService CreateService()
    {
        return new(new SpawnPlaybackSidecarCache(new DotNetSpawnPlaybackSidecarDecoder()));
    }

    private static ReplayDetails CreateReplayDetails()
    {
        return new()
        {
            ReplayHash = "replay-1",
            Replay = new()
            {
                SpawnPlayback = new()
                {
                    Available = true,
                    Compression = SpawnPlaybackCompression.Brotli
                },
                Players =
                [
                    new()
                    {
                        GamePos = 1,
                        TeamId = 1,
                        Spawns =
                        [
                            new()
                            {
                                Breakpoint = Breakpoint.Min5,
                                Units =
                                [
                                    new() { Name = "Marine", Count = 2 }
                                ]
                            }
                        ]
                    }
                ]
            }
        };
    }

    private static SpawnPlaybackSidecarDto CreateSidecar(string unitName)
    {
        return new(
            10_000,
            112,
            [
                new(1,
                [
                    new(1, unitName, 1, 6_600, 165, 174, null, null, null, []),
                    new(2, unitName, 1, 6_601, 166, 173, null, null, null, [])
                ])
            ],
            [
                new(1, 6_500, 6_700)
            ]);
    }

    private static SpawnPlaybackSidecarDto CreateMin15Sidecar()
    {
        return new(
            22_000,
            112,
            [
                new(1,
                [
                    new(1, "Stalker", 1, 19_900, 165, 174, null, null, null, []),
                    new(2, "Stalker", 1, 19_901, 166, 173, null, null, null, [])
                ])
            ],
            [
                new(1, 19_900, 20_160)
            ]);
    }

    private static SpawnPlaybackSidecarDto CreateWaveSidecar()
    {
        return new(
            1_000,
            112,
            [
                new(1,
                [
                    new(1, "Marine", 2, 300, 165, 174, null, null, null, []),
                    new(2, "Marine", 2, 301, 166, 173, null, null, null, []),
                    new(3, "Marauder", 2, 302, 167, 172, null, null, null, []),
                    new(4, "Reaper", 1, 120, 168, 171, null, null, null, [])
                ]),
                new(2,
                [
                    new(5, "Zealot", 1, 110, 90, 85, null, null, null, [])
                ])
            ],
            [
                new(1, 100, 180),
                new(2, 280, 360)
            ]);
    }

    private static ReplaySpawnPositionsDto CreateStoredPositions()
    {
        return new()
        {
            Players =
            [
                new()
                {
                    GamePos = 1,
                    Spawns =
                    [
                        new()
                        {
                            Breakpoint = Breakpoint.Min5,
                            Units =
                            [
                                new() { Name = "Marine", Positions = [170, 171, 172, 173] }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    private sealed class TestReplayRepository : IReplayRepository
    {
        public byte[]? SpawnPlaybackPayload { get; init; }
        public ReplaySpawnPositionsDto? SpawnPositions { get; init; }
        public int SpawnPlaybackCalls { get; private set; }
        public int SpawnPositionCalls { get; private set; }

        public Task<byte[]?> GetReplaySpawnPlayback(string replayHash, CancellationToken token = default)
        {
            SpawnPlaybackCalls++;
            return Task.FromResult(SpawnPlaybackPayload);
        }

        public Task<ReplaySpawnPositionsDto?> GetReplaySpawnPositions(string replayHash, CancellationToken token = default)
        {
            SpawnPositionCalls++;
            return Task.FromResult(SpawnPositions);
        }

        public Task<ReplayDetails?> GetReplayDetails(string replayHash) => throw new NotImplementedException();
        public Task<ReplayRatingDto?> GetReplayRating(string replayHash) => throw new NotImplementedException();
        public Task SaveReplayRatingAll(string replayHash, ReplayRatingDto rating) => throw new NotImplementedException();
        public Task<ReplayDetails?> GetLatestReplay() => throw new NotImplementedException();
        public Task<ReplayDetails?> GetNextReplay(bool after, string replayHash) => throw new NotImplementedException();
        public Task<List<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default) => throw new NotImplementedException();
        public Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default) => throw new NotImplementedException();
        public Task<ReplayDetails?> GetArcadeReplayDetails(string replayHash) => throw new NotImplementedException();
        public Task<List<ReplayListDto>> GetArcadeReplays(ArcadeReplaysRequest request, CancellationToken token = default) => throw new NotImplementedException();
        public Task<int> GetArcadeReplaysCount(ArcadeReplaysRequest request, CancellationToken token = default) => throw new NotImplementedException();
    }
}
