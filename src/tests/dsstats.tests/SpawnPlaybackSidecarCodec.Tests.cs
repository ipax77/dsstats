using dsstats.shared;
using System.Text.Json;

namespace dsstats.tests;

[TestClass]
public class SpawnPlaybackSidecarCodecTests
{
    [TestMethod]
    public void SpawnPositionCells_RoundTripAllTeamCells()
    {
        int count = 0;
        for (int v = 317; v <= 339; v++)
        {
            for (int u = -9; u <= 25; u++)
            {
                if (((u + v) & 1) != 0)
                {
                    continue;
                }

                int x = (u + v) / 2;
                int y = (v - u) / 2;
                Assert.IsTrue(SpawnPlaybackSidecarCodec.TryGetSpawnPositionCellId(1, x, y, out int team1Cell));
                Assert.IsTrue(SpawnPlaybackSidecarCodec.TryGetSpawnPositionCellId(4, x - 81, y - 81, out int team2Cell));
                Assert.AreEqual(team1Cell, team2Cell);
                Assert.AreEqual((x, y), SpawnPlaybackSidecarCodec.GetSpawnPositionFromCellId(1, team1Cell));
                Assert.AreEqual((x - 81, y - 81), SpawnPlaybackSidecarCodec.GetSpawnPositionFromCellId(4, team2Cell));
                count++;
            }
        }

        Assert.AreEqual(403, count);
    }

    [TestMethod]
    public void EncodeDecode_RoundTripsSidecar()
    {
        var source = new SpawnPlaybackSidecarDto(
            DurationGameloop: 22_400,
            StepGameloops: 112,
            Players:
            [
                new(1,
                [
                    new(10, "Marine", 3, 1_120, 165, 174, 2_240, 130, 130, [1_500, 1_620]),
                    new(11, "Marauder", 3, 1_121, 166, 174, null, null, null, []),
                    new(12, "Tank", 3, 1_122, 10, 10, 2_500, 300, 300, [])
                ]),
                new(4,
                [
                    new(20, "Zergling", 3, 1_130, 84, 93, 1_900, null, null, [1_450])
                ])
            ],
            Snapshots:
            [
                new(3, 1_120, 2_240),
                new(4, 2_500, 3_400)
            ]);

        byte[] payload = SpawnPlaybackSidecarCodec.Encode(source);
        var decoded = SpawnPlaybackSidecarCodec.Decode(payload);

        Assert.AreEqual(source.DurationGameloop, decoded.DurationGameloop);
        Assert.AreEqual(source.StepGameloops, decoded.StepGameloops);
        Assert.AreEqual(2, decoded.Players.Count);
        Assert.AreEqual(1, decoded.Players[0].GamePos);
        Assert.AreEqual("Marine", decoded.Players[0].Units[0].Name);
        Assert.AreEqual(2_240, decoded.Players[0].Units[0].DiedGameloop);
        Assert.AreEqual(130, decoded.Players[0].Units[0].DiedX);
        CollectionAssert.AreEqual(new[] { 1_500, 1_620 }, decoded.Players[0].Units[0].KillGameloops.ToArray());
        Assert.IsNull(decoded.Players[0].Units[1].DiedGameloop);
        Assert.AreEqual(10, decoded.Players[0].Units[2].SpawnX);
        Assert.AreEqual(300, decoded.Players[0].Units[2].DiedX);
        Assert.AreEqual("Zergling", decoded.Players[1].Units[0].Name);
        Assert.AreEqual(2, decoded.Snapshots.Count);
        Assert.AreEqual(3_400, decoded.Snapshots[1].EndGameloop);
    }

    [TestMethod]
    public void EncodeDecode_ReusesPreviousSpawnPositions()
    {
        var units = Enumerable.Range(1, 40)
            .Select(spawnNumber => new SpawnPlaybackUnitSidecarDto(
                UnitIndex: spawnNumber,
                Name: "Marine",
                SpawnNumber: spawnNumber,
                SpawnGameloop: 1_000 + spawnNumber * 100,
                SpawnX: 165,
                SpawnY: 174,
                DiedGameloop: null,
                DiedX: null,
                DiedY: null,
                KillGameloops: []))
            .ToArray();
        var source = new SpawnPlaybackSidecarDto(10_000, 112, [new(1, units)], []);

        var encoded = SpawnPlaybackSidecarCodec.EncodeWithMetadata(source);
        var decoded = SpawnPlaybackSidecarCodec.Decode(encoded.Payload);

        Assert.AreEqual(1, GetPlayerCandidateCount(encoded.CodecStats));
        Assert.AreEqual(1, GetWrittenCellCount(encoded.CodecStats));
        Assert.AreEqual(39, encoded.CodecStats?.ReusedSpawnPositionCount);
        CollectionAssert.AreEqual(
            units.Select(unit => (unit.SpawnX, unit.SpawnY)).ToArray(),
            decoded.Players[0].Units.Select(unit => (unit.SpawnX, unit.SpawnY)).ToArray());
    }

    [TestMethod]
    public void EncodeDecode_PreviousSpawnDiffHandlesAddedAndSoldUnits()
    {
        SpawnPlaybackUnitSidecarDto Unit(int unitIndex, string name, int spawnNumber, int x, int y)
        {
            return new(unitIndex, name, spawnNumber, 1_000 + spawnNumber * 100 + unitIndex, x, y, null, null, null, []);
        }

        var source = new SpawnPlaybackSidecarDto(
            10_000,
            112,
            [
                new(1,
                [
                    Unit(1, "Marine", 1, 165, 174),
                    Unit(2, "Marine", 1, 166, 173),
                    Unit(3, "Marauder", 1, 167, 172),
                    Unit(4, "Marine", 2, 165, 174),
                    Unit(5, "Marine", 2, 168, 171),
                    Unit(6, "Marauder", 2, 167, 172),
                    Unit(7, "Marine", 3, 168, 171),
                    Unit(8, "Marauder", 3, 167, 172)
                ])
            ],
            []);

        var encoded = SpawnPlaybackSidecarCodec.EncodeWithMetadata(source);
        var decoded = SpawnPlaybackSidecarCodec.Decode(encoded.Payload);

        Assert.AreEqual(1, GetPlayerCandidateCount(encoded.CodecStats));
        Assert.IsTrue(GetWrittenCellCount(encoded.CodecStats) >= 5);
        Assert.IsTrue(encoded.CodecStats?.ReusedSpawnPositionCount >= 2);
        CollectionAssert.AreEqual(
            source.Players[0].Units.Select(unit => (unit.Name, unit.SpawnNumber, unit.SpawnX, unit.SpawnY)).ToArray(),
            decoded.Players[0].Units.Select(unit => (unit.Name, unit.SpawnNumber, unit.SpawnX, unit.SpawnY)).ToArray());
    }

    [TestMethod]
    public void Encode_UsesAbsolutePlayerModeForTinyTie()
    {
        var source = new SpawnPlaybackSidecarDto(
            1_000,
            112,
            [new(1, [new(1, "Marine", 1, 1_000, 165, 174, null, null, null, [])])],
            []);

        var encoded = SpawnPlaybackSidecarCodec.EncodeWithMetadata(source);

        Assert.AreEqual(1, GetPlayerCandidateCount(encoded.CodecStats));
        Assert.AreEqual(1, GetWrittenCellCount(encoded.CodecStats));
        Assert.AreEqual(0, encoded.CodecStats?.ReusedSpawnPositionCount);
    }

    [TestMethod]
    public void EncodeDecode_CanMixAbsoluteAndRepeatPlayerModes()
    {
        var repeatedUnits = Enumerable.Range(1, 20)
            .Select(spawnNumber => new SpawnPlaybackUnitSidecarDto(
                100 + spawnNumber,
                "Zergling",
                spawnNumber,
                2_000 + spawnNumber,
                84,
                93,
                null,
                null,
                null,
                []))
            .ToArray();
        var source = new SpawnPlaybackSidecarDto(
            10_000,
            112,
            [
                new(1, [new(1, "Marine", 1, 1_000, 165, 174, null, null, null, [])]),
                new(4, repeatedUnits)
            ],
            []);

        var encoded = SpawnPlaybackSidecarCodec.EncodeWithMetadata(source);
        var decoded = SpawnPlaybackSidecarCodec.Decode(encoded.Payload);

        Assert.AreEqual(2, GetPlayerCandidateCount(encoded.CodecStats));
        Assert.AreEqual(2, decoded.Players.Count);
        Assert.AreEqual(84, decoded.Players[1].Units[0].SpawnX);
        Assert.AreEqual(93, decoded.Players[1].Units[^1].SpawnY);
    }

    [TestMethod]
    public void Encode_UsesCompactPayloadForRepeatedUnits()
    {
        var units = Enumerable.Range(0, 250)
            .Select(index => new SpawnPlaybackUnitSidecarDto(
                index,
                index % 2 == 0 ? "Marine" : "Marauder",
                index / 10,
                1_000 + index,
                165 + index % 4,
                174 + index % 3,
                2_000 + index,
                130,
                130,
                [1_200 + index, 1_400 + index]))
            .ToArray();

        var source = new SpawnPlaybackSidecarDto(
            10_000,
            112,
            [new(1, units)],
            [new(1, 1_000, 2_100)]);

        byte[] payload = SpawnPlaybackSidecarCodec.Encode(source);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(source);

        Assert.IsTrue(payload.Length < json.Length);
        Assert.AreEqual(units.Length, SpawnPlaybackSidecarCodec.GetUnitCount(source));
        Assert.IsTrue(SpawnPlaybackSidecarCodec.GetUncompressedLength(source) > payload.Length);
    }

    private static int GetPlayerCandidateCount(SpawnPlaybackCodecStats? stats)
    {
        Assert.IsNotNull(stats);
        return stats.AbsolutePlayerCount + stats.RepeatPlayerCount;
    }

    private static int GetWrittenCellCount(SpawnPlaybackCodecStats? stats)
    {
        Assert.IsNotNull(stats);
        return stats.ChangedSpawnPositionCount;
    }
}
