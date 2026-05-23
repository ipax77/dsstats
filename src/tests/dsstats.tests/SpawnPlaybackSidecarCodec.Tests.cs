using dsstats.shared;
using System.Text.Json;

namespace dsstats.tests;

[TestClass]
public class SpawnPlaybackSidecarCodecTests
{
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
                    new(11, "Marauder", 3, 1_121, 166, 174, null, null, null, [])
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
        Assert.AreEqual("Zergling", decoded.Players[1].Units[0].Name);
        Assert.AreEqual(2, decoded.Snapshots.Count);
        Assert.AreEqual(3_400, decoded.Snapshots[1].EndGameloop);
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
}
