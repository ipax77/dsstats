using pax.dsstats.parser;
using s2protocol.NET;

namespace dsstats.decode.tests;

[TestClass]
public sealed class ParseTests
{
    [TestMethod]
    public async Task CanDecodeReplay()
    {
        var path = "/data/ds/testreplays/Direct Strike TE (4545).SC2Replay";
        var decoder = new ReplayDecoder();
        var options = new ReplayDecoderOptions()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            TrackerEvents = true,
        };
        var sc2Replay = await decoder.DecodeAsync(path, options);
        Assert.IsNotNull(sc2Replay);
        var dsReplay = Parse.GetDsReplay(sc2Replay);
        Assert.IsNotNull(dsReplay);
        using var md5 = System.Security.Cryptography.MD5.Create();
        var replay = Parse.GetReplayDto(dsReplay, md5);
        Assert.IsNotNull(replay);
    }

    [TestMethod]
    public async Task CanDecodeReplay2()
    {
        List<string> paths = ["/data/ds/testreplays/Direct Strike TE (4545).SC2Replay"];
        var decoder = new ReplayDecoder();
        var options = new ReplayDecoderOptions()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            TrackerEvents = true,
        };
        await foreach (var result in decoder.DecodeParallelWithErrorReport(paths, 1, options))
        {
            Assert.IsNotNull(result.Sc2Replay);
            var metaData = dsstats.decode.DecodeService.GetMetaData(result.Sc2Replay);
            var dsReplay = Parse.GetDsReplay(result.Sc2Replay);
            Assert.IsNotNull(dsReplay);
            using var md5 = System.Security.Cryptography.MD5.Create();
            var replay = Parse.GetReplayDto(dsReplay, md5);
            Assert.IsNotNull(replay);
        }
    }
}
