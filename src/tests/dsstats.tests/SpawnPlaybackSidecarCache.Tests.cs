using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.weblib.Replays;

namespace dsstats.tests;

[TestClass]
public class SpawnPlaybackSidecarCacheTests
{
    [TestMethod]
    public async Task GetSidecar_DecodesOnceForRepeatedReplay()
    {
        var payload = SpawnPlaybackSidecarCodec.Encode(CreateSidecar());
        var repository = new TestReplayRepository(hash => Task.FromResult<byte[]?>(payload));
        var cache = new SpawnPlaybackSidecarCache(new DotNetSpawnPlaybackSidecarDecoder());

        var first = await cache.GetSidecar("replay-1", SpawnPlaybackCompression.Brotli, repository);
        var second = await cache.GetSidecar("replay-1", SpawnPlaybackCompression.Brotli, repository);

        Assert.IsNotNull(first);
        Assert.AreSame(first, second);
        Assert.AreEqual(1, repository.GetLoadCount("replay-1"));
    }

    [TestMethod]
    public async Task GetSidecar_SharesConcurrentLoads()
    {
        var payload = SpawnPlaybackSidecarCodec.Encode(CreateSidecar());
        var loadCompletion = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new TestReplayRepository(_ => loadCompletion.Task);
        var cache = new SpawnPlaybackSidecarCache(new DotNetSpawnPlaybackSidecarDecoder());

        var firstTask = cache.GetSidecar("replay-1", SpawnPlaybackCompression.Brotli, repository);
        var secondTask = cache.GetSidecar("replay-1", SpawnPlaybackCompression.Brotli, repository);

        Assert.AreSame(firstTask, secondTask);
        Assert.AreEqual(1, repository.GetLoadCount("replay-1"));

        loadCompletion.SetResult(payload);
        var sidecar = await firstTask;

        Assert.IsNotNull(sidecar);
    }

    [TestMethod]
    public async Task GetSidecar_EvictsOldestEntryAfterLimit()
    {
        var payload = SpawnPlaybackSidecarCodec.Encode(CreateSidecar());
        var repository = new TestReplayRepository(_ => Task.FromResult<byte[]?>(payload));
        var cache = new SpawnPlaybackSidecarCache(new DotNetSpawnPlaybackSidecarDecoder());

        for (int i = 0; i < 9; i++)
        {
            var sidecar = await cache.GetSidecar($"replay-{i}", SpawnPlaybackCompression.Brotli, repository);
            Assert.IsNotNull(sidecar);
        }

        var reloaded = await cache.GetSidecar("replay-0", SpawnPlaybackCompression.Brotli, repository);

        Assert.IsNotNull(reloaded);
        Assert.AreEqual(2, repository.GetLoadCount("replay-0"));
        Assert.AreEqual(1, repository.GetLoadCount("replay-1"));
    }

    [TestMethod]
    public async Task GetSidecar_InvalidPayloadReturnsNull()
    {
        var repository = new TestReplayRepository(_ => Task.FromResult<byte[]?>([1, 2, 3]));
        var cache = new SpawnPlaybackSidecarCache(new DotNetSpawnPlaybackSidecarDecoder());

        var sidecar = await cache.GetSidecar("replay-1", SpawnPlaybackCompression.Brotli, repository);

        Assert.IsNull(sidecar);
        Assert.AreEqual(1, repository.GetLoadCount("replay-1"));
    }

    private static SpawnPlaybackSidecarDto CreateSidecar()
    {
        return new(
            DurationGameloop: 2_240,
            StepGameloops: 112,
            Players:
            [
                new(1,
                [
                    new(1, "Marine", 1, 112, 165, 174, null, null, null, [])
                ])
            ],
            Snapshots:
            [
                new(1, 112, 224)
            ]);
    }

    private sealed class TestReplayRepository(Func<string, Task<byte[]?>> getPayload) : IReplayRepository
    {
        private readonly Dictionary<string, int> loadCounts = [];

        public int GetLoadCount(string replayHash)
        {
            return loadCounts.GetValueOrDefault(replayHash);
        }

        public Task<byte[]?> GetReplaySpawnPlayback(string replayHash, CancellationToken token = default)
        {
            loadCounts[replayHash] = GetLoadCount(replayHash) + 1;
            return getPayload(replayHash);
        }

        public Task<ReplayDetails?> GetReplayDetails(string replayHash) => throw new NotImplementedException();
        public Task<ReplaySpawnPositionsDto?> GetReplaySpawnPositions(string replayHash, CancellationToken token = default) => throw new NotImplementedException();
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
