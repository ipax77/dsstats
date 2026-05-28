using dsstats.parser;
using dsstats.play;
using dsstats.shared;
using System.Buffers.Binary;

namespace dsstats.play.tests;

[TestClass]
public sealed class SpawnPlaybackFactoryNgTests
{
    private const string ReplayFile = "Direct Strike (10253).SC2Replay";
    private const int UnitRowStride = 11;
    private const int PathRowStride = 3;
    private const int PathPointStride = 3;

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

    [TestMethod]
    public void Create_PathCanHoldOnTwoClusters()
    {
        SpawnPlaybackReplayNg playback = CreateSyntheticPlayback(
            probeLifetimeGameloops: null,
            (92, 88),
            (129, 121));

        var points = GetPathPointsForUnit(playback, unitIndex: 1);

        Assert.AreEqual(6, points.Count);
        Assert.AreEqual(2, CountConsecutiveStationarySegments(points));
        Assert.AreEqual(2096, points[^1].GameloopOffset);
    }

    [TestMethod]
    public void Create_PathKeepsSingleClusterHoldWhenOnlyOneClusterIsEligible()
    {
        SpawnPlaybackReplayNg playback = CreateSyntheticPlayback(
            probeLifetimeGameloops: null,
            (92, 88));

        var points = GetPathPointsForUnit(playback, unitIndex: 1);

        Assert.AreEqual(4, points.Count);
        Assert.AreEqual(1, CountConsecutiveStationarySegments(points));
        Assert.AreEqual(2096, points[^1].GameloopOffset);
    }

    [TestMethod]
    public void Create_PathWithExcessClustersDoesNotExceedFixedPointBudget()
    {
        const int probeLifetimeGameloops = 1500;
        SpawnPlaybackReplayNg playback = CreateSyntheticPlayback(
            probeLifetimeGameloops,
            (92, 88),
            (129, 121),
            (166, 154));

        var points = GetPathPointsForUnit(playback, unitIndex: 1);

        Assert.IsLessThanOrEqualTo(6, points.Count);
        Assert.AreEqual(probeLifetimeGameloops, points[^1].GameloopOffset);
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

    private static SpawnPlaybackReplayNg CreateSyntheticPlayback(
        int? probeLifetimeGameloops,
        params (int X, int Y)[] clusterCenters)
    {
        var units = new List<SpawnPlaybackUnitSidecarDto>
        {
            new(
                UnitIndex: 1,
                Name: "Marine",
                SpawnNumber: 1,
                SpawnGameloop: 0,
                SpawnX: 60,
                SpawnY: 60,
                DiedGameloop: probeLifetimeGameloops,
                DiedX: null,
                DiedY: null,
                KillGameloops: [])
        };

        int unitIndex = 2;
        for (int clusterIndex = 0; clusterIndex < clusterCenters.Length; clusterIndex++)
        {
            var center = clusterCenters[clusterIndex];
            int diedGameloop = 40 + clusterIndex * 10;
            for (int i = 0; i < 4; i++)
            {
                units.Add(new(
                    UnitIndex: unitIndex++,
                    Name: "Marine",
                    SpawnNumber: 1,
                    SpawnGameloop: 0,
                    SpawnX: 60,
                    SpawnY: 60,
                    DiedGameloop: diedGameloop + i,
                    DiedX: center.X,
                    DiedY: center.Y,
                    KillGameloops: []));
            }
        }

        var replay = new ReplayDto
        {
            Duration = 120,
            Players =
            [
                new()
                {
                    Name = "Player 1",
                    Race = Commander.Terran,
                    TeamId = 1,
                    GamePos = 1
                }
            ]
        };

        var sidecar = new SpawnPlaybackSidecarDto(
            DurationGameloop: 2096,
            StepGameloops: 112,
            Players: [new(GamePos: 1, Units: units)],
            Snapshots: []);

        return SpawnPlaybackFactoryNg.Create(replay, sidecar);
    }

    private static List<PathPointData> GetPathPointsForUnit(SpawnPlaybackReplayNg playback, int unitIndex)
    {
        var unitRows = GetBinaryPayload(playback, "unitRows");
        var pathRows = GetBinaryPayload(playback, "pathRows");
        var pathPoints = GetBinaryPayload(playback, "pathPoints");

        int pathIndex = -1;
        for (int rowIndex = 0; rowIndex < unitRows.Count; rowIndex++)
        {
            if (ReadRowValue(unitRows.Bytes, rowIndex, UnitRowStride, 0) == unitIndex)
            {
                pathIndex = ReadRowValue(unitRows.Bytes, rowIndex, UnitRowStride, 6);
                break;
            }
        }

        Assert.AreNotEqual(-1, pathIndex, $"Expected a unit row for unit index {unitIndex}.");
        int pointOffset = ReadRowValue(pathRows.Bytes, pathIndex, PathRowStride, 0);
        int pointCount = ReadRowValue(pathRows.Bytes, pathIndex, PathRowStride, 1);
        var points = new List<PathPointData>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            int pointIndex = pointOffset + i;
            points.Add(new(
                ReadRowValue(pathPoints.Bytes, pointIndex, PathPointStride, 0),
                ReadRowValue(pathPoints.Bytes, pointIndex, PathPointStride, 1),
                ReadRowValue(pathPoints.Bytes, pointIndex, PathPointStride, 2)));
        }

        return points;
    }

    private static int CountConsecutiveStationarySegments(IReadOnlyList<PathPointData> points)
    {
        int count = 0;
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].X == points[i - 1].X && points[i].Y == points[i - 1].Y)
            {
                count++;
            }
        }

        return count;
    }

    private sealed record PathPointData(int X, int Y, int GameloopOffset);
}
