using dsstats.parser;
using dsstats.play;
using dsstats.shared;

namespace dsstats.play.tests;

[TestClass]
public sealed class SpawnPlaybackFactoryNgTests
{
    private const string ReplayFile = "Direct Strike (10253).SC2Replay";

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (10253).SC2Replay")]
    public async Task Create_FromDecodedReplayBuildsNgReplay()
    {
        var sc2Replay = await DsstatsParser.GetSc2Replay(ReplayFile);
        Assert.IsNotNull(sc2Replay);

        ReplayDto replayDto = DsstatsParser.ParseReplay(sc2Replay);
        var directStrikeReplay = DsstatsParser.ParseDirectStrikeReplay(sc2Replay);
        SpawnPlaybackSidecarDto? sidecar = SpawnPlaybackSidecarFactory.Create(sc2Replay, directStrikeReplay);
        Assert.IsNotNull(sidecar);

        SpawnPlaybackReplayNg playback = SpawnPlaybackFactoryNg.Create(replayDto, sidecar);

        Assert.IsGreaterThan(0, playback.DurationGameloop);
        Assert.IsGreaterThan(0, playback.Stats.UnitCount);
        Assert.AreEqual(playback.Stats.UnitCount, GetBinaryPayload(playback, "unitRows").Count);
        Assert.IsGreaterThan(0, playback.Players.Count);
        Assert.IsGreaterThan(0, playback.UnitKinds.Count);
        Assert.IsGreaterThan(0, GetBinaryPayload(playback, "pathRows").Count);
        Assert.IsGreaterThan(0, GetBinaryPayload(playback, "pathPoints").Count);
    }

    private static SpawnPlaybackBinaryPayloadNg GetBinaryPayload(SpawnPlaybackReplayNg playback, string datasetId)
    {
        var payload = playback.BinaryPayloads.FirstOrDefault(payload => payload.DatasetId == datasetId);
        Assert.IsNotNull(payload, $"Expected binary payload '{datasetId}'.");
        return payload;
    }
}
