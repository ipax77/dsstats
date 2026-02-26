using dsstats.parser;
using dsstats.shared;

namespace dsstats.tests;

[TestClass]
public sealed class TourneyInfoTests
{
    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (3).SC2Replay")]
    public async Task ShouldMapPlayer()
    {
        string replayPath = "Direct Strike TE (3).SC2Replay";
        var info = await GetReplayTourneyInfo(replayPath);
        Assert.IsNotNull(info);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (4545).SC2Replay")]
    public async Task ShouldDecodeTest1()
    {
        string replayPath = "Direct Strike TE (4545).SC2Replay";
        var info = await GetReplayTourneyInfo(replayPath);
        Assert.IsNotNull(info);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (26).SC2Replay")]
    public async Task CanSetGameModeForOldReplays()
    {
        string replayPath = "Direct Strike (26).SC2Replay";
        var info = await GetReplayTourneyInfo(replayPath);
    }

    private static async Task<ReplayTourneyInfoDto> GetReplayTourneyInfo(string replayPath)
    {
        var sc2Replay = await DsstatsParser.GetSc2Replay(replayPath);
        Assert.IsNotNull(sc2Replay);
        var tourneyInfo = DsstatsParser.GetMetaData(sc2Replay);
        Assert.IsNotNull(tourneyInfo);
        return tourneyInfo;
    }
}

