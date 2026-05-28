using dsstats.parser;
using dsstats.play;
using dsstats.shared;
using System.Buffers.Binary;

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

        var unitRows = GetBinaryPayload(playback, "unitRows");
        var pathRows = GetBinaryPayload(playback, "pathRows");
        var pathPoints = GetBinaryPayload(playback, "pathPoints");
        Assert.AreEqual(SpawnPlaybackBinaryDataFormatNg.Int32Rows, pathPoints.Format);

        int firstUnitSpawnGameloop = ReadRowValue(unitRows.Bytes, 0, 11, 4);
        int firstUnitExpiresGameloop = ReadRowValue(unitRows.Bytes, 0, 11, 5);
        int firstUnitPathIndex = ReadRowValue(unitRows.Bytes, 0, 11, 6);
        int pointOffset = ReadRowValue(pathRows.Bytes, firstUnitPathIndex, 3, 0);
        int pointCount = ReadRowValue(pathRows.Bytes, firstUnitPathIndex, 3, 1);
        int finalPointGameloopOffset = ReadRowValue(pathPoints.Bytes, pointOffset + pointCount - 1, 3, 2);

        Assert.IsGreaterThanOrEqualTo(2, pointCount);
        Assert.AreEqual(firstUnitExpiresGameloop - firstUnitSpawnGameloop, finalPointGameloopOffset);
    }

    private static SpawnPlaybackBinaryPayloadNg GetBinaryPayload(SpawnPlaybackReplayNg playback, string datasetId)
    {
        var payload = playback.BinaryPayloads.FirstOrDefault(payload => payload.DatasetId == datasetId);
        Assert.IsNotNull(payload, $"Expected binary payload '{datasetId}'.");
        return payload;
    }

    private static int ReadRowValue(byte[] bytes, int rowIndex, int rowStride, int valueOffset)
    {
        int offset = checked((rowIndex * rowStride + valueOffset) * sizeof(int));
        return BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, sizeof(int)));
    }
}
