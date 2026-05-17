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

        // GamePos - Build
        // 1 => Terran.Bio
        // 2 => Zerg.QueenLurker
        // 3 => Terran.Bio
        // 4 => Terran.BC
        // 5 => Protoss.Stalker
        // 6 => Terran.Bio


    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (1914).SC2Replay")]
    public async Task CanDetectBuildDetails3()
    {
        string replayPath = "Direct Strike TE (1914).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);

        var details = DetailBuilds.DetectStandardBuild(replayDto);
        Assert.IsNotNull(details);

        // GamePos - Build
        // 1 => Terran.Bio
        // 2 => Protoss.Stalker
        // 3 => Terran.Bio
        // 4 => Protoss.Templar
        // 5 => Terran.Bio
        // 6 => Terran.Bio


    }

    private static async Task<ReplayDto> GetReplayDto(string replayPath)
    {
        var sc2Replay = await DsstatsParser.GetSc2Replay(replayPath);
        Assert.IsNotNull(sc2Replay);
        var replayDto = DsstatsParser.ParseReplay(sc2Replay);
        Assert.IsNotNull(replayDto);
        return replayDto;
    }
}


