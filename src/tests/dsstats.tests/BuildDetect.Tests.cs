using dsstats.dbServices;
using dsstats.parser;
using dsstats.shared;
using dsstats.shared.DetailBuild;

namespace dsstats.tests;

[TestClass]
public sealed class BuildDetectTests
{
    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (1915).SC2Replay")]
    public async Task CanDetectBuildDetails1()
    {
        string replayPath = "Direct Strike TE (1915).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);

        var details = DetailBuilds.DetectStandardBuild(replayDto);
        Assert.IsNull(details); // replay without result
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (1912).SC2Replay")]
    public async Task CanDetectBuildDetails2()
    {
        string replayPath = "Direct Strike TE (1912).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);

        var details = DetailBuilds.DetectStandardBuild(replayDto);
        Assert.IsNotNull(details);
        Assert.HasCount(3, details.MatchupInfos);

        AssertBuild(details, 1, TerranBuild.Bio);
        AssertBuild(details, 2, ZergBuild.QueenLurker);
        AssertBuild(details, 3, TerranBuild.Bio);
        AssertBuild(details, 4, TerranBuild.BC);
        AssertBuild(details, 5, ProtossBuild.Stalker);
        AssertBuild(details, 6, TerranBuild.Bio);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (1914).SC2Replay")]
    public async Task CanDetectBuildDetails3()
    {
        string replayPath = "Direct Strike TE (1914).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);

        var details = DetailBuilds.DetectStandardBuild(replayDto);
        Assert.IsNotNull(details);
        Assert.HasCount(3, details.MatchupInfos);

        AssertBuild(details, 1, TerranBuild.Bio);
        AssertBuild(details, 2, ProtossBuild.AdeptStalker);
        AssertBuild(details, 3, TerranBuild.Bio);
        AssertBuild(details, 4, ProtossBuild.Templar);
        AssertBuild(details, 5, TerranBuild.Bio);
        AssertBuild(details, 6, TerranBuild.Bio);
    }

    [TestMethod]
    public void CanDetectTerranBioFromNormalizedAlias()
    {
        AssertTerranBuild(TerranBuild.Bio, new UnitDto { Name = "MarineLightweight", Count = 2 });
    }

    [TestMethod]
    public void CanDetectTerranMechFromNormalizedAliases()
    {
        AssertTerranBuild(
            TerranBuild.Mech,
            new UnitDto { Name = "SiegeTank", Count = 1 },
            new UnitDto { Name = "ThorAP", Count = 1 });
    }

    [TestMethod]
    public void CanDetectTerranBioWithSupportTech()
    {
        AssertTerranBuild(
            TerranBuild.Bio,
            new UnitDto { Name = "MarineLightweight", Count = 36 },
            new UnitDto { Name = "Raven", Count = 1 },
            new UnitDto { Name = "Viking", Count = 1 });

        AssertTerranBuild(
            TerranBuild.Bio,
            new UnitDto { Name = "MarineLightweight", Count = 26 },
            new UnitDto { Name = "Marauder", Count = 2 },
            new UnitDto { Name = "Reaper", Count = 2 },
            new UnitDto { Name = "Raven", Count = 1 },
            new UnitDto { Name = "WidowMine", Count = 4 });

        AssertTerranBuild(
            TerranBuild.Bio,
            new UnitDto { Name = "MarineLightweight", Count = 28 },
            new UnitDto { Name = "Raven", Count = 2 },
            new UnitDto { Name = "Banshee", Count = 2 });

        AssertTerranBuild(
            TerranBuild.Bio,
            new UnitDto { Name = "MarineLightweight", Count = 30 },
            new UnitDto { Name = "Marauder", Count = 2 },
            new UnitDto { Name = "SiegeTank", Count = 1 });

        AssertTerranBuild(
            TerranBuild.Bio,
            new UnitDto { Name = "MarineLightweight", Count = 27 },
            new UnitDto { Name = "Cyclone", Count = 4 },
            new UnitDto { Name = "Raven", Count = 2 });

        AssertTerranBuild(
            TerranBuild.Bio,
            new UnitDto { Name = "MarineLightweight", Count = 25 },
            new UnitDto { Name = "Marauder", Count = 3 },
            new UnitDto { Name = "Hellion", Count = 2 },
            new UnitDto { Name = "Raven", Count = 1 });

        AssertTerranBuild(
            TerranBuild.Bio,
            new UnitDto { Name = "MarineLightweight", Count = 26 },
            new UnitDto { Name = "Marauder", Count = 3 },
            new UnitDto { Name = "Cyclone", Count = 1 },
            new UnitDto { Name = "Hellion", Count = 3 });

        AssertTerranBuild(
            TerranBuild.Bio,
            new UnitDto { Name = "MarineLightweight", Count = 29 },
            new UnitDto { Name = "ThorAP", Count = 1 });

        AssertTerranBuild(
            TerranBuild.Bio,
            new UnitDto { Name = "MarineLightweight", Count = 27 },
            new UnitDto { Name = "Raven", Count = 1 },
            new UnitDto { Name = "ThorAP", Count = 1 });
    }

    [TestMethod]
    public void CanDetectTerranBanshees()
    {
        AssertTerranBuild(TerranBuild.Banshees, new UnitDto { Name = "Banshee", Count = 3 });

        AssertTerranBuild(
            TerranBuild.Banshees,
            new UnitDto { Name = "Banshee", Count = 4 },
            new UnitDto { Name = "Cyclone", Count = 3 },
            new UnitDto { Name = "Raven", Count = 2 },
            new UnitDto { Name = "WidowMine", Count = 5 });

        AssertTerranBuild(
            TerranBuild.Banshees,
            new UnitDto { Name = "Banshee", Count = 7 },
            new UnitDto { Name = "MarineLightweight", Count = 8 },
            new UnitDto { Name = "Raven", Count = 2 });

        AssertTerranBuild(
            TerranBuild.Banshees,
            new UnitDto { Name = "Banshee", Count = 6 },
            new UnitDto { Name = "Viking", Count = 5 });
    }

    [TestMethod]
    public void CanDetectTerranRavenViking()
    {
        AssertTerranBuild(
            TerranBuild.RavenViking,
            new UnitDto { Name = "Raven", Count = 5 },
            new UnitDto { Name = "Viking", Count = 7 });

        AssertTerranBuild(
            TerranBuild.RavenViking,
            new UnitDto { Name = "MarineLightweight", Count = 7 },
            new UnitDto { Name = "Raven", Count = 4 },
            new UnitDto { Name = "Viking", Count = 4 });

        AssertTerranBuild(
            TerranBuild.RavenViking,
            new UnitDto { Name = "Marauder", Count = 2 },
            new UnitDto { Name = "Raven", Count = 2 },
            new UnitDto { Name = "Viking", Count = 7 });
    }

    [TestMethod]
    public void HardTerranMechUnitsStillClassifyAsMech()
    {
        AssertTerranBuild(TerranBuild.Mech, new UnitDto { Name = "SiegeTank", Count = 1 });
        AssertTerranBuild(TerranBuild.Mech, new UnitDto { Name = "ThorAP", Count = 1 });
        AssertTerranBuild(TerranBuild.Mech, new UnitDto { Name = "Hellion", Count = 1 });
        AssertTerranBuild(TerranBuild.Mech, new UnitDto { Name = "Hellbat", Count = 1 });
        AssertTerranBuild(TerranBuild.Mech, new UnitDto { Name = "Cyclone", Count = 1 });

        AssertTerranBuild(
            TerranBuild.Mech,
            new UnitDto { Name = "Cyclone", Count = 8 },
            new UnitDto { Name = "Hellion", Count = 8 });

        AssertTerranBuild(
            TerranBuild.Mech,
            new UnitDto { Name = "MarineLightweight", Count = 10 },
            new UnitDto { Name = "Cyclone", Count = 8 },
            new UnitDto { Name = "Hellion", Count = 8 });
    }

    [TestMethod]
    public void CanDetectTerranLibs()
    {
        AssertTerranBuild(TerranBuild.Libs, new UnitDto { Name = "Liberator", Count = 1 });
    }

    [TestMethod]
    public void CanDetectTerranLibBio()
    {
        AssertTerranBuild(
            TerranBuild.LibBio,
            new UnitDto { Name = "MarineLightweight", Count = 12 },
            new UnitDto { Name = "Liberator", Count = 1 });

        AssertTerranBuild(
            TerranBuild.LibBio,
            new UnitDto { Name = "MarineLightweight", Count = 26 },
            new UnitDto { Name = "Marauder", Count = 3 },
            new UnitDto { Name = "Liberator", Count = 1 },
            new UnitDto { Name = "Hellion", Count = 3 });
    }

    [TestMethod]
    public void CanDetectTerranBattlecruisers()
    {
        AssertTerranBuild(TerranBuild.BC, new UnitDto { Name = "Battlecruiser", Count = 1 });
    }

    [TestMethod]
    public void CanDetectTerranMixedBuildPriority()
    {
        AssertTerranBuild(
            TerranBuild.BC,
            new UnitDto { Name = "Battlecruiser", Count = 1 },
            new UnitDto { Name = "Liberator", Count = 1 },
            new UnitDto { Name = "MarineLightweight", Count = 12 });

        AssertTerranBuild(
            TerranBuild.LibBio,
            new UnitDto { Name = "Liberator", Count = 1 },
            new UnitDto { Name = "SiegeTank", Count = 1 },
            new UnitDto { Name = "MarineLightweight", Count = 12 });

        AssertTerranBuild(
            TerranBuild.Banshees,
            new UnitDto { Name = "Banshee", Count = 6 },
            new UnitDto { Name = "SiegeTank", Count = 1 },
            new UnitDto { Name = "MarineLightweight", Count = 2 });

        AssertTerranBuild(
            TerranBuild.RavenViking,
            new UnitDto { Name = "Raven", Count = 4 },
            new UnitDto { Name = "Viking", Count = 4 },
            new UnitDto { Name = "SiegeTank", Count = 1 },
            new UnitDto { Name = "MarineLightweight", Count = 4 });

        AssertTerranBuild(
            TerranBuild.Bio,
            new UnitDto { Name = "SiegeTank", Count = 1 },
            new UnitDto { Name = "MarineLightweight", Count = 12 });
    }

    [TestMethod]
    public void ZeroCountTerranUnitsDoNotClassify()
    {
        AssertTerranBuild(
            TerranBuild.None,
            new UnitDto { Name = "Battlecruiser", Count = 0 },
            new UnitDto { Name = "Liberator", Count = 0 },
            new UnitDto { Name = "SiegeTank", Count = 0 },
            new UnitDto { Name = "MarineLightweight", Count = 0 });
    }

    [TestMethod]
    public void CanFlagGasFirstFromMin5SpawnGasCount()
    {
        var replayDto = CreateStandardReplayWithUnits(
            Commander.Terran,
            [new UnitDto { Name = "MarineLightweight", Count = 2 }]);

        replayDto.Players[0].Spawns[0].GasCount = 1;

        var details = DetailBuilds.DetectStandardBuild(replayDto);

        Assert.IsNotNull(details);
        var buildInfo = GetBuildInfo(details, 1);
        Assert.AreEqual(TerranBuild.Bio.ToString(), buildInfo.BuildName);
        Assert.IsTrue(buildInfo.GasFirst);
    }

    [TestMethod]
    public void NoGasInMin5SpawnDoesNotFlagGasFirst()
    {
        var replayDto = CreateStandardReplayWithUnits(
            Commander.Terran,
            [new UnitDto { Name = "MarineLightweight", Count = 2 }]);

        replayDto.Players[0].Refineries.Add(30);

        var details = DetailBuilds.DetectStandardBuild(replayDto);

        Assert.IsNotNull(details);
        var buildInfo = GetBuildInfo(details, 1);
        Assert.AreEqual(TerranBuild.Bio.ToString(), buildInfo.BuildName);
        Assert.IsFalse(buildInfo.GasFirst);
    }

    [TestMethod]
    public void CanDetectProtossBuilds()
    {
        AssertProtossBuild(ProtossBuild.Zealots, new UnitDto { Name = "Zealot", Count = 2 });
        AssertProtossBuild(ProtossBuild.Stalker, new UnitDto { Name = "Stalker", Count = 2 });
        AssertProtossBuild(
            ProtossBuild.AdeptStalker,
            new UnitDto { Name = "Adept", Count = 2 },
            new UnitDto { Name = "Stalker", Count = 2 });
        AssertProtossBuild(
            ProtossBuild.ZealotStalker,
            new UnitDto { Name = "Zealot", Count = 2 },
            new UnitDto { Name = "Stalker", Count = 2 });
        AssertProtossBuild(ProtossBuild.Archons, new UnitDto { Name = "Archon", Count = 1 });
        AssertProtossBuild(ProtossBuild.Immortals, new UnitDto { Name = "Immortal", Count = 1 });
        AssertProtossBuild(
            ProtossBuild.ArchonsImmortals,
            new UnitDto { Name = "Archon", Count = 1 },
            new UnitDto { Name = "Immortal", Count = 1 });
        AssertProtossBuild(ProtossBuild.Templar, new UnitDto { Name = "HighTemplar", Count = 1 });
        AssertProtossBuild(ProtossBuild.Carriers, new UnitDto { Name = "Carrier", Count = 1 });
        AssertProtossBuild(
            ProtossBuild.AirDisruptor,
            new UnitDto { Name = "VoidRay", Count = 1 },
            new UnitDto { Name = "Disruptor", Count = 1 });
        AssertProtossBuild(ProtossBuild.Adepts, new UnitDto { Name = "Adept", Count = 2 });
        AssertProtossBuild(ProtossBuild.Voidrays, new UnitDto { Name = "VoidRay", Count = 2 });
    }

    [TestMethod]
    public void CanDetectProtossMixedBuildPriority()
    {
        AssertProtossBuild(
            ProtossBuild.Carriers,
            new UnitDto { Name = "Carrier", Count = 1 },
            new UnitDto { Name = "HighTemplar", Count = 1 },
            new UnitDto { Name = "Immortal", Count = 1 });

        AssertProtossBuild(
            ProtossBuild.Templar,
            new UnitDto { Name = "HighTemplar", Count = 1 },
            new UnitDto { Name = "Archon", Count = 1 },
            new UnitDto { Name = "Immortal", Count = 1 });

        AssertProtossBuild(
            ProtossBuild.ArchonsImmortals,
            new UnitDto { Name = "Archon", Count = 1 },
            new UnitDto { Name = "Immortal", Count = 1 },
            new UnitDto { Name = "Stalker", Count = 1 });

        AssertProtossBuild(
            ProtossBuild.AirDisruptor,
            new UnitDto { Name = "Disruptor", Count = 2 },
            new UnitDto { Name = "VoidRay", Count = 5 });

        AssertProtossBuild(
            ProtossBuild.AdeptStalker,
            new UnitDto { Name = "Adept", Count = 2 },
            new UnitDto { Name = "Zealot", Count = 2 },
            new UnitDto { Name = "Stalker", Count = 2 });
    }

    [TestMethod]
    public void SingleOracleDoesNotQualifyAsProtossAirDisruptor()
    {
        AssertProtossBuild(
            ProtossBuild.None,
            new UnitDto { Name = "Oracle", Count = 1 },
            new UnitDto { Name = "Disruptor", Count = 1 });

        AssertProtossBuild(
            ProtossBuild.None,
            new UnitDto { Name = "Disruptor", Count = 1 },
            new UnitDto { Name = "Oracle", Count = 1 },
            new UnitDto { Name = "Adept", Count = 6 });

        AssertProtossBuild(
            ProtossBuild.None,
            new UnitDto { Name = "Oracle", Count = 1 },
            new UnitDto { Name = "Disruptor", Count = 2 });
    }

    [TestMethod]
    public void MultipleOraclesQualifyAsProtossAirDisruptor()
    {
        AssertProtossBuild(
            ProtossBuild.AirDisruptor,
            new UnitDto { Name = "Oracle", Count = 2 },
            new UnitDto { Name = "Disruptor", Count = 1 });
    }

    [TestMethod]
    public void ProtossAirUnitsStillQualifyAsAirDisruptor()
    {
        AssertProtossBuild(
            ProtossBuild.AirDisruptor,
            new UnitDto { Name = "Phoenix", Count = 1 },
            new UnitDto { Name = "Oracle", Count = 1 },
            new UnitDto { Name = "Disruptor", Count = 1 });

        AssertProtossBuild(
            ProtossBuild.AirDisruptor,
            new UnitDto { Name = "Tempest", Count = 1 },
            new UnitDto { Name = "Disruptor", Count = 1 });
    }

    [TestMethod]
    public void ProtossCheapBuildsRequireWeightedShare()
    {
        AssertProtossBuild(
            ProtossBuild.None,
            new UnitDto { Name = "Stalker", Count = 2 },
            new UnitDto { Name = "Disruptor", Count = 2 });

        AssertProtossBuild(
            ProtossBuild.Adepts,
            new UnitDto { Name = "Adept", Count = 12 },
            new UnitDto { Name = "Disruptor", Count = 2 });

        AssertProtossBuild(
            ProtossBuild.Voidrays,
            new UnitDto { Name = "Adept", Count = 5 },
            new UnitDto { Name = "Oracle", Count = 2 },
            new UnitDto { Name = "Phoenix", Count = 1 },
            new UnitDto { Name = "VoidRay", Count = 5 });
    }

    [TestMethod]
    public void ZeroCountProtossUnitsDoNotClassify()
    {
        AssertProtossBuild(
            ProtossBuild.None,
            new UnitDto { Name = "Carrier", Count = 0 },
            new UnitDto { Name = "HighTemplar", Count = 0 },
            new UnitDto { Name = "Archon", Count = 0 },
            new UnitDto { Name = "Immortal", Count = 0 },
            new UnitDto { Name = "Stalker", Count = 0 },
            new UnitDto { Name = "Zealot", Count = 0 });
    }

    [TestMethod]
    public void CanDetectZergBuilds()
    {
        AssertZergBuild(ZergBuild.Zerglings, new UnitDto { Name = "ZerglingLightweight", Count = 2 });
        AssertZergBuild(
            ZergBuild.LingBanes,
            new UnitDto { Name = "ZerglingLightweight", Count = 2 },
            new UnitDto { Name = "Baneling", Count = 2 });
        AssertZergBuild(ZergBuild.Queens, new UnitDto { Name = "Queen", Count = 2 });
        AssertZergBuild(ZergBuild.Roaches, new UnitDto { Name = "Roach", Count = 2 });
        AssertZergBuild(
            ZergBuild.RoachQueen,
            new UnitDto { Name = "Roach", Count = 2 },
            new UnitDto { Name = "Queen", Count = 2 });
        AssertZergBuild(ZergBuild.Mutas, new UnitDto { Name = "Mutalisk", Count = 1 });
        AssertZergBuild(ZergBuild.Hydras, new UnitDto { Name = "Hydralisk", Count = 2 });
        AssertZergBuild(
            ZergBuild.RoachQueenLurker,
            new UnitDto { Name = "Roach", Count = 2 },
            new UnitDto { Name = "Queen", Count = 2 },
            new UnitDto { Name = "LurkerMP", Count = 1 });
        AssertZergBuild(
            ZergBuild.QueenLurker,
            new UnitDto { Name = "Queen", Count = 2 },
            new UnitDto { Name = "LurkerMP", Count = 1 });
        AssertZergBuild(ZergBuild.Ultras, new UnitDto { Name = "Ultralisk", Count = 1 });
        AssertZergBuild(ZergBuild.Ravagers, new UnitDto { Name = "Ravager", Count = 2 });
        AssertZergBuild(ZergBuild.SwarmHosts, new UnitDto { Name = "SwarmHostMP", Count = 1 });
        AssertZergBuild(
            ZergBuild.HydraliskLings,
            new UnitDto { Name = "ZerglingLightweight", Count = 23 },
            new UnitDto { Name = "Hydralisk", Count = 16 });
    }

    [TestMethod]
    public void CanDetectZergMixedBuildPriority()
    {
        AssertZergBuild(
            ZergBuild.Ultras,
            new UnitDto { Name = "Ultralisk", Count = 1 },
            new UnitDto { Name = "Mutalisk", Count = 1 },
            new UnitDto { Name = "Hydralisk", Count = 1 });

        AssertZergBuild(
            ZergBuild.Mutas,
            new UnitDto { Name = "Mutalisk", Count = 1 },
            new UnitDto { Name = "Hydralisk", Count = 1 },
            new UnitDto { Name = "Roach", Count = 1 });

        AssertZergBuild(
            ZergBuild.SwarmHosts,
            new UnitDto { Name = "SwarmHostMP", Count = 1 },
            new UnitDto { Name = "Ravager", Count = 12 },
            new UnitDto { Name = "Hydralisk", Count = 2 });

        AssertZergBuild(
            ZergBuild.RoachQueenLurker,
            new UnitDto { Name = "Hydralisk", Count = 1 },
            new UnitDto { Name = "Roach", Count = 2 },
            new UnitDto { Name = "Queen", Count = 2 },
            new UnitDto { Name = "LurkerMP", Count = 1 });

        AssertZergBuild(
            ZergBuild.RoachQueenLurker,
            new UnitDto { Name = "Roach", Count = 2 },
            new UnitDto { Name = "Queen", Count = 2 },
            new UnitDto { Name = "LurkerMP", Count = 1 },
            new UnitDto { Name = "Baneling", Count = 1 });

        AssertZergBuild(
            ZergBuild.Ravagers,
            new UnitDto { Name = "Ravager", Count = 12 },
            new UnitDto { Name = "Hydralisk", Count = 1 },
            new UnitDto { Name = "Roach", Count = 1 });
    }

    [TestMethod]
    public void ZergCheapBuildsRequireWeightedShare()
    {
        AssertZergBuild(
            ZergBuild.Roaches,
            new UnitDto { Name = "ZerglingLightweight", Count = 2 },
            new UnitDto { Name = "Roach", Count = 4 });

        AssertZergBuild(
            ZergBuild.Roaches,
            new UnitDto { Name = "Hydralisk", Count = 2 },
            new UnitDto { Name = "Roach", Count = 4 });

        AssertZergBuild(
            ZergBuild.Hydras,
            new UnitDto { Name = "Hydralisk", Count = 8 },
            new UnitDto { Name = "Roach", Count = 2 });
    }

    [TestMethod]
    public void SingleCheapUnitsDoNotClassify()
    {
        AssertTerranBuild(TerranBuild.None, new UnitDto { Name = "MarineLightweight", Count = 1 });
        AssertProtossBuild(ProtossBuild.None, new UnitDto { Name = "Stalker", Count = 1 });
        AssertProtossBuild(ProtossBuild.None, new UnitDto { Name = "Zealot", Count = 1 });
        AssertZergBuild(ZergBuild.None, new UnitDto { Name = "ZerglingLightweight", Count = 1 });
        AssertZergBuild(ZergBuild.None, new UnitDto { Name = "Queen", Count = 1 });
        AssertZergBuild(ZergBuild.None, new UnitDto { Name = "Roach", Count = 1 });
        AssertZergBuild(ZergBuild.None, new UnitDto { Name = "Hydralisk", Count = 1 });
        AssertZergBuild(ZergBuild.None, new UnitDto { Name = "Ravager", Count = 1 });
    }

    [TestMethod]
    public void ZeroCountZergUnitsDoNotClassify()
    {
        AssertZergBuild(
            ZergBuild.None,
            new UnitDto { Name = "Ultralisk", Count = 0 },
            new UnitDto { Name = "Mutalisk", Count = 0 },
            new UnitDto { Name = "Hydralisk", Count = 0 },
            new UnitDto { Name = "Roach", Count = 0 },
            new UnitDto { Name = "Queen", Count = 0 },
            new UnitDto { Name = "LurkerMP", Count = 0 },
            new UnitDto { Name = "Ravager", Count = 0 },
            new UnitDto { Name = "SwarmHostMP", Count = 0 },
            new UnitDto { Name = "ZerglingLightweight", Count = 0 });
    }

    [TestMethod]
    public void CanDetectTeamBuildPatterns()
    {
        AssertSingleTeamBuild(
            TeamBuild.PTStack,
            1,
            1,
            2,
            Player(Commander.Protoss, new UnitDto { Name = "Stalker", Count = 2 }),
            Player(Commander.Terran, new UnitDto { Name = "MarineLightweight", Count = 2 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }));

        AssertSingleTeamBuild(
            TeamBuild.ZZStack,
            1,
            1,
            2,
            Player(Commander.Zerg, new UnitDto { Name = "Roach", Count = 2 }, new UnitDto { Name = "Queen", Count = 2 }),
            Player(Commander.Zerg, new UnitDto { Name = "Hydralisk", Count = 2 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }));

        AssertSingleTeamBuild(
            TeamBuild.PZStack,
            1,
            1,
            2,
            Player(Commander.Protoss, new UnitDto { Name = "Stalker", Count = 2 }),
            Player(Commander.Zerg, new UnitDto { Name = "ZerglingLightweight", Count = 2 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }));

        AssertSingleTeamBuild(
            TeamBuild.TZStack,
            1,
            1,
            2,
            Player(Commander.Terran, new UnitDto { Name = "Liberator", Count = 1 }),
            Player(Commander.Zerg, new UnitDto { Name = "Mutalisk", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }));
    }

    [TestMethod]
    public void CanDetectTeamBuildCircularWraps()
    {
        var replayDto = CreateStandardReplayWithPlayers(
            Player(Commander.Terran, new UnitDto { Name = "MarineLightweight", Count = 2 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Protoss, new UnitDto { Name = "Stalker", Count = 2 }),
            Player(Commander.Zerg, new UnitDto { Name = "Mutalisk", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "Liberator", Count = 1 }));

        var details = DetailBuilds.DetectStandardBuild(replayDto);

        Assert.IsNotNull(details);
        Assert.HasCount(2, details.TeamBuildInfos);
        AssertTeamBuild(details, 1, 3, 1, TeamBuild.PTStack);
        AssertTeamBuild(details, 2, 6, 4, TeamBuild.TZStack);
    }

    [TestMethod]
    public void DetectsFirstTeamBuildInCircularScanOrder()
    {
        AssertSingleTeamBuild(
            TeamBuild.PZStack,
            1,
            2,
            3,
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Protoss, new UnitDto { Name = "Stalker", Count = 2 }),
            Player(Commander.Zerg, new UnitDto { Name = "ZerglingLightweight", Count = 2 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }));
    }

    [TestMethod]
    public void NoTeamBuildMatchReturnsEmptyCollection()
    {
        var replayDto = CreateStandardReplayWithPlayers(
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Protoss, new UnitDto { Name = "Zealot", Count = 1 }),
            Player(Commander.Zerg, new UnitDto { Name = "Queen", Count = 1 }),
            Player(Commander.Terran, new UnitDto { Name = "Battlecruiser", Count = 1 }));

        var details = DetailBuilds.DetectStandardBuild(replayDto);

        Assert.IsNotNull(details);
        Assert.HasCount(0, details.TeamBuildInfos);
    }

    [TestMethod]
    public void CanDetectTeamBuildsForBothTeams()
    {
        var replayDto = CreateStandardReplayWithPlayers(
            Player(Commander.Protoss, new UnitDto { Name = "Stalker", Count = 2 }),
            Player(Commander.Terran, new UnitDto { Name = "MarineLightweight", Count = 2 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }),
            Player(Commander.Zerg, new UnitDto { Name = "Roach", Count = 2 }, new UnitDto { Name = "Queen", Count = 2 }),
            Player(Commander.Zerg, new UnitDto { Name = "Hydralisk", Count = 2 }),
            Player(Commander.Terran, new UnitDto { Name = "SiegeTank", Count = 1 }));

        var details = DetailBuilds.DetectStandardBuild(replayDto);

        Assert.IsNotNull(details);
        Assert.HasCount(2, details.TeamBuildInfos);
        AssertTeamBuild(details, 1, 1, 2, TeamBuild.PTStack);
        AssertTeamBuild(details, 2, 4, 5, TeamBuild.ZZStack);
    }

    private static async Task<ReplayDto> GetReplayDto(string replayPath)
    {
        var sc2Replay = await DsstatsParser.GetSc2Replay(replayPath);
        Assert.IsNotNull(sc2Replay);
        var replayDto = DsstatsParser.ParseReplay(sc2Replay);
        Assert.IsNotNull(replayDto);
        return replayDto;
    }

    private static void AssertTerranBuild(TerranBuild expectedBuild, params UnitDto[] units)
    {
        AssertRaceBuild(Commander.Terran, expectedBuild, units);
    }

    private static void AssertProtossBuild(ProtossBuild expectedBuild, params UnitDto[] units)
    {
        AssertRaceBuild(Commander.Protoss, expectedBuild, units);
    }

    private static void AssertZergBuild(ZergBuild expectedBuild, params UnitDto[] units)
    {
        AssertRaceBuild(Commander.Zerg, expectedBuild, units);
    }

    private static void AssertRaceBuild(Commander commander, Enum expectedBuild, UnitDto[] units)
    {
        var replayDto = CreateStandardReplayWithUnits(commander, units);
        var details = DetailBuilds.DetectStandardBuild(replayDto);

        Assert.IsNotNull(details);
        AssertBuild(details, 1, expectedBuild);
    }

    private static ReplayDto CreateStandardReplayWithUnits(Commander commander, UnitDto[] units)
    {
        var replayDto = new ReplayDto
        {
            GameMode = GameMode.Standard,
            WinnerTeam = 1,
        };
        var fallbackUnit = GetFallbackUnit(commander);

        for (int gamePos = 1; gamePos <= 6; gamePos++)
        {
            replayDto.Players.Add(new ReplayPlayerDto
            {
                Race = commander,
                TeamId = gamePos <= 3 ? 1 : 2,
                GamePos = gamePos,
                Result = gamePos <= 3 ? PlayerResult.Win : PlayerResult.Los,
                Duration = 300,
                Spawns =
                [
                    new SpawnDto
                    {
                        Breakpoint = Breakpoint.Min5,
                        Units = gamePos == 1
                            ? units.ToList()
                            : [fallbackUnit]
                    }
                ]
            });
        }

        return replayDto;
    }

    private static UnitDto GetFallbackUnit(Commander commander) =>
        commander switch
        {
            Commander.Protoss => new UnitDto { Name = "Zealot", Count = 1 },
            Commander.Zerg => new UnitDto { Name = "ZerglingLightweight", Count = 1 },
            _ => new UnitDto { Name = "MarineLightweight", Count = 1 },
        };

    private static (Commander Commander, UnitDto[] Units) Player(Commander commander, params UnitDto[] units) =>
        (commander, units);

    private static ReplayDto CreateStandardReplayWithPlayers(params (Commander Commander, UnitDto[] Units)[] players)
    {
        Assert.HasCount(6, players);

        var replayDto = new ReplayDto
        {
            GameMode = GameMode.Standard,
            WinnerTeam = 1,
        };

        for (int i = 0; i < players.Length; i++)
        {
            var gamePos = i + 1;
            replayDto.Players.Add(new ReplayPlayerDto
            {
                Race = players[i].Commander,
                TeamId = gamePos <= 3 ? 1 : 2,
                GamePos = gamePos,
                Result = gamePos <= 3 ? PlayerResult.Win : PlayerResult.Los,
                Duration = 300,
                Spawns =
                [
                    new SpawnDto
                    {
                        Breakpoint = Breakpoint.Min5,
                        Units = players[i].Units.ToList()
                    }
                ]
            });
        }

        return replayDto;
    }

    private static void AssertSingleTeamBuild(
        TeamBuild expectedBuild,
        int expectedTeamId,
        int expectedLeaderGamePos,
        int expectedFollowerGamePos,
        params (Commander Commander, UnitDto[] Units)[] players)
    {
        var replayDto = CreateStandardReplayWithPlayers(players);
        var details = DetailBuilds.DetectStandardBuild(replayDto);

        Assert.IsNotNull(details);
        Assert.HasCount(1, details.TeamBuildInfos);
        AssertTeamBuild(
            details,
            expectedTeamId,
            expectedLeaderGamePos,
            expectedFollowerGamePos,
            expectedBuild);
    }

    private static void AssertTeamBuild(
        ReplayBuildDetails details,
        int expectedTeamId,
        int expectedLeaderGamePos,
        int expectedFollowerGamePos,
        TeamBuild expectedBuild)
    {
        TeamBuildInfo? teamBuildInfo = null;

        foreach (var info in details.TeamBuildInfos)
        {
            if (info.TeamId == expectedTeamId
                && info.LeaderGamePos == expectedLeaderGamePos
                && info.FollowerGamePos == expectedFollowerGamePos)
            {
                teamBuildInfo = info;
                break;
            }
        }

        Assert.IsNotNull(teamBuildInfo);
        Assert.AreEqual(expectedBuild, teamBuildInfo.TeamBuild);
        Assert.AreEqual(expectedBuild.ToString(), teamBuildInfo.TeamBuildName);
    }

    private static void AssertBuild(ReplayBuildDetails details, int gamePos, Enum expectedBuild)
    {
        var buildInfo = GetBuildInfo(details, gamePos);
        Assert.AreEqual(Convert.ToInt32(expectedBuild), buildInfo.Build);
        Assert.AreEqual(expectedBuild.ToString(), buildInfo.BuildName);
    }

    private static PlayerBuildInfo GetBuildInfo(ReplayBuildDetails details, int gamePos)
    {
        foreach (var matchup in details.MatchupInfos)
        {
            if (matchup.P1.GamePos == gamePos)
            {
                return matchup.P1;
            }

            if (matchup.P2.GamePos == gamePos)
            {
                return matchup.P2;
            }
        }

        Assert.Fail($"Missing build info for game position {gamePos}.");
        throw new InvalidOperationException();
    }
}
