using dsstats.db;
using dsstats.parser;
using dsstats.shared;

namespace dsstats.tests;

[TestClass]
public sealed class MvpTests
{
    [TestMethod]
    public void ParserSetsMvpWhenReplayHasNoLeaver()
    {
        ReplayDto replay = CreateReplay(secondPlayerDuration: 510);

        DirectStrikeReplayDtoMapper.SetMvp(replay);

        Assert.IsFalse(replay.Players[0].IsMvp);
        Assert.IsTrue(replay.Players[1].IsMvp);
    }

    [TestMethod]
    public void ParserDoesNotSetMvpWhenReplayHasLeaver()
    {
        ReplayDto replay = CreateReplay(secondPlayerDuration: 509);

        DirectStrikeReplayDtoMapper.SetMvp(replay);

        Assert.IsTrue(replay.Players.All(player => !player.IsMvp));
    }

    [TestMethod]
    public void LegacyMapperDoesNotSetMvpWhenReplayHasLeaver()
    {
        ReplayV2Dto replay = new()
        {
            Duration = 600,
            Maxkillsum = 200,
            ReplayPlayers =
            [
                new() { Duration = 600, Kills = 100 },
                new() { Duration = 509, Kills = 200 },
            ],
        };

        ReplayDto mapped = replay.ToV3Dto();

        Assert.IsTrue(mapped.Players.All(player => !player.IsMvp));
    }

    [TestMethod]
    public void EntityMapperClearsMvpWhenReplayHasLeaver()
    {
        ReplayDto replay = CreateReplay(secondPlayerDuration: 509);
        replay.Players[1].IsMvp = true;

        Replay entity = replay.ToEntity();

        Assert.IsTrue(entity.Players.All(player => !player.IsMvp));
    }

    private static ReplayDto CreateReplay(int secondPlayerDuration)
    {
        return new()
        {
            Duration = 600,
            Players =
            [
                new()
                {
                    Duration = 600,
                    Spawns = [new() { Breakpoint = Breakpoint.All, KilledValue = 100 }],
                },
                new()
                {
                    Duration = secondPlayerDuration,
                    Spawns = [new() { Breakpoint = Breakpoint.All, KilledValue = 200 }],
                },
            ],
        };
    }
}
