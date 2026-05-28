using System.Buffers.Binary;
using dsstats.play;

namespace dsstats.play.tests;

[TestClass]
public sealed class SpawnPlaybackHelperTests
{
    [TestMethod]
    public void BuildPlaybackStops_UsesSnapshotEndsAndRemovesDuplicates()
    {
        var replay = CreateReplay(
            durationGameloop: 20,
            snapshots:
            [
                new(1, 0, 10),
                new(2, 10, 20),
                new(3, 20, 20)
            ]);
        List<int> stops = [];

        SpawnPlaybackTimeline.BuildPlaybackStops(replay, stops);

        CollectionAssert.AreEqual(new[] { 10, 20 }, stops);
        Assert.AreEqual(1, SpawnPlaybackTimeline.FindPlaybackStopIndexAtOrBefore(stops, 25));
    }

    [TestMethod]
    public void BuildPlaybackStops_UsesFallbackStepsWhenSnapshotsAreMissing()
    {
        var replay = CreateReplay(
            durationGameloop: 25,
            stepGameloops: 10,
            payloads: [CreateUnitRowsPayload((PlayerIndex: 0, UnitKindIndex: 0, SpawnNumber: 1, SpawnGameloop: 5, ExpiresGameloop: 20, KillOffset: 0, KillCount: 0))]);
        List<int> stops = [999];

        SpawnPlaybackTimeline.BuildPlaybackStops(replay, stops);

        CollectionAssert.AreEqual(new[] { 5, 15, 25 }, stops);
        SpawnPlaybackTimeline.BuildPlaybackStops(null, stops);
        Assert.AreEqual(0, stops.Count);
    }

    [TestMethod]
    public void ResolveRenderGameloop_ExtendsIntoActiveSnapshotRanges()
    {
        var replay = CreateReplay(
            snapshots:
            [
                new(1, 10, 20),
                new(2, 18, 30)
            ]);

        Assert.AreEqual(5, SpawnPlaybackTimeline.ResolveRenderGameloop(replay, 5));
        Assert.AreEqual(20, SpawnPlaybackTimeline.ResolveRenderGameloop(replay, 15));
        Assert.AreEqual(30, SpawnPlaybackTimeline.ResolveRenderGameloop(replay, 19));
        Assert.AreEqual(31, SpawnPlaybackTimeline.ResolveRenderGameloop(replay, 31));
    }

    [TestMethod]
    public void BinaryPayloads_ReadRowsAndHandleMissingPayloads()
    {
        var unitRows = CreateUnitRowsPayload((PlayerIndex: 1, UnitKindIndex: 2, SpawnNumber: 3, SpawnGameloop: 40, ExpiresGameloop: 80, KillOffset: 4, KillCount: 5));
        var replay = CreateReplay(payloads: [unitRows]);

        Assert.AreSame(unitRows, SpawnPlaybackBinaryPayloads.GetPayload(replay, SpawnPlaybackBinaryPayloads.UnitRowsDatasetId));
        Assert.AreEqual(0, SpawnPlaybackBinaryPayloads.GetPayloadBytes(replay, "missing").Length);
        Assert.AreEqual(40, SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitRows.Bytes, 0, SpawnPlaybackBinaryPayloads.UnitRowSpawnGameloopOffset));
        Assert.AreEqual(0, SpawnPlaybackBinaryPayloads.ReadInt32(unitRows.Bytes, unitRows.Bytes.Length));
    }

    [TestMethod]
    public void UpdateRows_CountsAliveUnitsAndBuildsSortedRows()
    {
        var replay = CreateReplay(
            players:
            [
                new("Player 1", 1, 1, "Raynor", [], []),
                new("Player 2", 2, 2, "Artanis", [], [])
            ],
            unitKinds:
            [
                new("Marine", "Raynor", 0.5, "#111111"),
                new("Adept", "Raynor", 0.5, "#222222"),
                new("Zealot", "Artanis", 0.5, "#333333")
            ],
            payloads:
            [
                CreateUnitRowsPayload(
                    (PlayerIndex: 0, UnitKindIndex: 0, SpawnNumber: 1, SpawnGameloop: 10, ExpiresGameloop: 50, KillOffset: 0, KillCount: 2),
                    (PlayerIndex: 0, UnitKindIndex: 1, SpawnNumber: 1, SpawnGameloop: 10, ExpiresGameloop: 50, KillOffset: 2, KillCount: 1),
                    (PlayerIndex: 1, UnitKindIndex: 2, SpawnNumber: 2, SpawnGameloop: 20, ExpiresGameloop: 70, KillOffset: 3, KillCount: 1),
                    (PlayerIndex: 0, UnitKindIndex: 0, SpawnNumber: 2, SpawnGameloop: 15, ExpiresGameloop: 30, KillOffset: 4, KillCount: 1),
                    (PlayerIndex: 99, UnitKindIndex: 99, SpawnNumber: 3, SpawnGameloop: 45, ExpiresGameloop: 90, KillOffset: 0, KillCount: 0)),
                CreateKillGameloopsPayload(10, 35, 45, 5, 20)
            ]);
        List<AliveUnitRow> team1Rows = [];
        List<AliveUnitRow> team2Rows = [];
        Dictionary<AliveUnitKey, AliveUnitAccumulator> accumulators = [];

        var summary = SpawnPlaybackAliveUnits.UpdateRows(
            replay,
            renderGameloop: 40,
            showAliveUnits: true,
            team1Rows,
            team2Rows,
            accumulators);

        Assert.AreEqual(new(3, 2, 1, 2), summary);
        Assert.AreEqual(2, team1Rows.Count);
        Assert.AreEqual("Adept", team1Rows[0].UnitName);
        Assert.AreEqual(0, team1Rows[0].CurrentKills);
        Assert.AreEqual("Marine", team1Rows[1].UnitName);
        Assert.AreEqual(2, team1Rows[1].CurrentKills);
        Assert.AreEqual(1, team2Rows.Count);
        Assert.AreEqual("Zealot", team2Rows[0].UnitName);
        Assert.AreEqual(1, team2Rows[0].CurrentKills);
        Assert.AreEqual("1|6:Raynor|6:Marine", team1Rows[1].HighlightKey);
    }

    [TestMethod]
    public void UpdateRows_SkipsRowObjectsWhenAlivePanelIsCollapsed()
    {
        var replay = CreateReplay(
            players: [new("Player 1", 1, 1, "Raynor", [], [])],
            unitKinds: [new("Marine", "Raynor", 0.5, "#111111")],
            payloads: [CreateUnitRowsPayload((PlayerIndex: 0, UnitKindIndex: 0, SpawnNumber: 1, SpawnGameloop: 10, ExpiresGameloop: 50, KillOffset: 0, KillCount: 0))]);
        List<AliveUnitRow> team1Rows = [new(1, "Old", "Old", "Old", "Old", null, 1, 1)];
        List<AliveUnitRow> team2Rows = [new(2, "Old", "Old", "Old", "Old", null, 1, 1)];
        Dictionary<AliveUnitKey, AliveUnitAccumulator> accumulators = new()
        {
            [new(1, "Old", "Old")] = new(1, 1, null)
        };

        var summary = SpawnPlaybackAliveUnits.UpdateRows(
            replay,
            renderGameloop: 40,
            showAliveUnits: false,
            team1Rows,
            team2Rows,
            accumulators);

        Assert.AreEqual(new(1, 1, 0, 1), summary);
        Assert.AreEqual(0, team1Rows.Count);
        Assert.AreEqual(0, team2Rows.Count);
        Assert.AreEqual(0, accumulators.Count);
    }

    private static SpawnPlaybackReplayNg CreateReplay(
        int durationGameloop = 100,
        int stepGameloops = 10,
        IReadOnlyList<SpawnPlaybackSnapshot>? snapshots = null,
        IReadOnlyList<SpawnPlaybackPlayerNg>? players = null,
        IReadOnlyList<SpawnPlaybackUnitKindNg>? unitKinds = null,
        IReadOnlyList<SpawnPlaybackBinaryPayloadNg>? payloads = null)
    {
        return new(
            durationGameloop,
            stepGameloops,
            new(0, 0, 1, 1),
            new(0, 0, 0, 0, 0, 0),
            new(0, [], []),
            new(0, []),
            [],
            snapshots ?? [],
            players ?? [],
            unitKinds ?? [],
            payloads ?? []);
    }

    private static SpawnPlaybackBinaryPayloadNg CreateKillGameloopsPayload(params int[] gameloops)
    {
        byte[] bytes = new byte[gameloops.Length * sizeof(int)];
        for (int i = 0; i < gameloops.Length; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * sizeof(int), sizeof(int)), gameloops[i]);
        }

        return new(SpawnPlaybackBinaryPayloads.KillGameloopsDatasetId, bytes, gameloops.Length, SpawnPlaybackBinaryDataFormatNg.Int32Y);
    }

    private static SpawnPlaybackBinaryPayloadNg CreateUnitRowsPayload(
        params (int PlayerIndex, int UnitKindIndex, int SpawnNumber, int SpawnGameloop, int ExpiresGameloop, int KillOffset, int KillCount)[] rows)
    {
        byte[] bytes = new byte[rows.Length * SpawnPlaybackBinaryPayloads.UnitRowByteStride];
        for (int i = 0; i < rows.Length; i++)
        {
            WriteUnitRow(bytes, i, rows[i]);
        }

        return new(
            SpawnPlaybackBinaryPayloads.UnitRowsDatasetId,
            bytes,
            rows.Length,
            SpawnPlaybackBinaryDataFormatNg.Int32Rows,
            ByteStride: SpawnPlaybackBinaryPayloads.UnitRowByteStride);
    }

    private static void WriteUnitRow(
        byte[] bytes,
        int rowIndex,
        (int PlayerIndex, int UnitKindIndex, int SpawnNumber, int SpawnGameloop, int ExpiresGameloop, int KillOffset, int KillCount) row)
    {
        int offset = rowIndex * SpawnPlaybackBinaryPayloads.UnitRowByteStride;
        WriteInt32(bytes, offset + SpawnPlaybackBinaryPayloads.UnitRowPlayerIndexOffset * sizeof(int), row.PlayerIndex);
        WriteInt32(bytes, offset + SpawnPlaybackBinaryPayloads.UnitRowUnitKindIndexOffset * sizeof(int), row.UnitKindIndex);
        WriteInt32(bytes, offset + SpawnPlaybackBinaryPayloads.UnitRowSpawnNumberOffset * sizeof(int), row.SpawnNumber);
        WriteInt32(bytes, offset + SpawnPlaybackBinaryPayloads.UnitRowSpawnGameloopOffset * sizeof(int), row.SpawnGameloop);
        WriteInt32(bytes, offset + SpawnPlaybackBinaryPayloads.UnitRowExpiresGameloopOffset * sizeof(int), row.ExpiresGameloop);
        WriteInt32(bytes, offset + SpawnPlaybackBinaryPayloads.UnitRowKillOffsetOffset * sizeof(int), row.KillOffset);
        WriteInt32(bytes, offset + SpawnPlaybackBinaryPayloads.UnitRowKillCountOffset * sizeof(int), row.KillCount);
    }

    private static void WriteInt32(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset, sizeof(int)), value);
    }
}
