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
        AssertBuild(details, 2, ProtossBuild.Stalker);
        AssertBuild(details, 3, TerranBuild.Bio);
        AssertBuild(details, 4, ProtossBuild.Templar);
        AssertBuild(details, 5, TerranBuild.Bio);
        AssertBuild(details, 6, TerranBuild.Bio);
    }

    [TestMethod]
    public void CanDetectTerranBioFromNormalizedAlias()
    {
        AssertTerranBuild(TerranBuild.Bio, new UnitDto { Name = "MarineLightweight", Count = 1 });
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
    public void CanDetectTerranLibs()
    {
        AssertTerranBuild(TerranBuild.Libs, new UnitDto { Name = "Liberator", Count = 1 });
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
            TerranBuild.Libs,
            new UnitDto { Name = "Liberator", Count = 1 },
            new UnitDto { Name = "SiegeTank", Count = 1 },
            new UnitDto { Name = "MarineLightweight", Count = 12 });

        AssertTerranBuild(
            TerranBuild.Mech,
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
    public void CanDetectProtossBuilds()
    {
        AssertProtossBuild(ProtossBuild.Zealots, new UnitDto { Name = "Zealot", Count = 1 });
        AssertProtossBuild(ProtossBuild.Stalker, new UnitDto { Name = "Stalker", Count = 1 });
        AssertProtossBuild(
            ProtossBuild.AdeptStalker,
            new UnitDto { Name = "Adept", Count = 2 },
            new UnitDto { Name = "Stalker", Count = 1 });
        AssertProtossBuild(
            ProtossBuild.ZealotStalker,
            new UnitDto { Name = "Zealot", Count = 2 },
            new UnitDto { Name = "Stalker", Count = 1 });
        AssertProtossBuild(ProtossBuild.Archons, new UnitDto { Name = "Archon", Count = 1 });
        AssertProtossBuild(ProtossBuild.Immortals, new UnitDto { Name = "Immortal", Count = 1 });
        AssertProtossBuild(
            ProtossBuild.ArchonsImmortals,
            new UnitDto { Name = "Archon", Count = 1 },
            new UnitDto { Name = "Immortal", Count = 1 });
        AssertProtossBuild(ProtossBuild.Templar, new UnitDto { Name = "HighTemplar", Count = 1 });
        AssertProtossBuild(ProtossBuild.Carriers, new UnitDto { Name = "Carrier", Count = 1 });
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
            ProtossBuild.AdeptStalker,
            new UnitDto { Name = "Adept", Count = 2 },
            new UnitDto { Name = "Zealot", Count = 2 },
            new UnitDto { Name = "Stalker", Count = 1 });
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
        AssertZergBuild(ZergBuild.Zerglings, new UnitDto { Name = "ZerglingLightweight", Count = 1 });
        AssertZergBuild(
            ZergBuild.LingBanes,
            new UnitDto { Name = "ZerglingLightweight", Count = 1 },
            new UnitDto { Name = "Baneling", Count = 1 });
        AssertZergBuild(ZergBuild.Queens, new UnitDto { Name = "Queen", Count = 1 });
        AssertZergBuild(ZergBuild.Roaches, new UnitDto { Name = "Roach", Count = 1 });
        AssertZergBuild(
            ZergBuild.RoachQueen,
            new UnitDto { Name = "Roach", Count = 1 },
            new UnitDto { Name = "Queen", Count = 1 });
        AssertZergBuild(ZergBuild.Mutas, new UnitDto { Name = "Mutalisk", Count = 1 });
        AssertZergBuild(ZergBuild.Hydras, new UnitDto { Name = "Hydralisk", Count = 1 });
        AssertZergBuild(
            ZergBuild.RoachQueenLurker,
            new UnitDto { Name = "Roach", Count = 1 },
            new UnitDto { Name = "Queen", Count = 1 },
            new UnitDto { Name = "LurkerMP", Count = 1 });
        AssertZergBuild(
            ZergBuild.QueenLurker,
            new UnitDto { Name = "Queen", Count = 1 },
            new UnitDto { Name = "LurkerMP", Count = 1 });
        AssertZergBuild(ZergBuild.Ultras, new UnitDto { Name = "Ultralisk", Count = 1 });
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
            ZergBuild.RoachQueenLurker,
            new UnitDto { Name = "Hydralisk", Count = 1 },
            new UnitDto { Name = "Roach", Count = 1 },
            new UnitDto { Name = "Queen", Count = 1 },
            new UnitDto { Name = "LurkerMP", Count = 1 });

        AssertZergBuild(
            ZergBuild.RoachQueenLurker,
            new UnitDto { Name = "Roach", Count = 1 },
            new UnitDto { Name = "Queen", Count = 1 },
            new UnitDto { Name = "LurkerMP", Count = 1 },
            new UnitDto { Name = "Baneling", Count = 1 });
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
            new UnitDto { Name = "ZerglingLightweight", Count = 0 });
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

    private static void AssertBuild(ReplayBuildDetails details, int gamePos, Enum expectedBuild)
    {
        PlayerBuildInfo? buildInfo = null;

        foreach (var matchup in details.MatchupInfos)
        {
            if (matchup.P1.GamePos == gamePos)
            {
                buildInfo = matchup.P1;
                break;
            }

            if (matchup.P2.GamePos == gamePos)
            {
                buildInfo = matchup.P2;
                break;
            }
        }

        Assert.IsNotNull(buildInfo);
        Assert.AreEqual(Convert.ToInt32(expectedBuild), buildInfo.Build);
        Assert.AreEqual(expectedBuild.ToString(), buildInfo.BuildName);
    }
}
