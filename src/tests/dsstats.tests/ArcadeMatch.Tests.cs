

using dsstats.ratings;
using dsstats.shared;

namespace dsstats.tests;

[TestClass]
public class ArcadeMatch
{
    [TestMethod]
    public void CanProduceMatchScore()
    {
        var replay1 = GetReplayDto();
        var replay2 = GetReplayDto();
        var score = RatingService.GetMatchScore(replay1, replay2);
        Assert.AreEqual(1.0, score);
    }

    [TestMethod]
    public void CanProduceMatchScore2()
    {
        var replay1 = GetReplayDto();
        var replay2 = GetReplayDto();
        replay2 = replay2 with { Gametime = replay2.Gametime.AddMinutes(30) };
        var score = RatingService.GetMatchScore(replay1, replay2);
        Assert.IsLessThan(1.0, score);
    }

    private static ReplayMatchDto GetReplayDto()
    {
        return new()
        {
            ReplayId = 1,
            Gametime = new DateTime(2021, 2, 1),
            GameMode = shared.GameMode.Standard,
            PlayerCount = 6,
            WinnerTeam = 1,
            Players = new()
            {
                new()
                {
                    ReplayPlayerId = 1,
                    Team = 1,
                    ToonId = new()
                    {
                        Region = 1,
                        Realm = 1,
                        Id = 1,
                    }
                },
                new()
                {
                    ReplayPlayerId = 2,
                    Team = 1,
                    ToonId = new()
                    {
                        Region = 1,
                        Realm = 1,
                        Id = 2,
                    }
                },
                new()
                {
                    ReplayPlayerId = 3,
                    Team = 1,
                    ToonId = new()
                    {
                        Region = 1,
                        Realm = 1,
                        Id = 3,
                    }
                },
                new()
                {
                    ReplayPlayerId = 4,
                    Team = 2,
                    ToonId = new()
                    {
                        Region = 1,
                        Realm = 1,
                        Id = 4,
                    }
                },
                new()
                {
                    ReplayPlayerId = 5,
                    Team = 2,
                    ToonId = new()
                    {
                        Region = 1,
                        Realm = 1,
                        Id = 5,
                    }
                },
                new()
                {
                    ReplayPlayerId = 6,
                    Team = 2,
                    ToonId = new()
                    {
                        Region = 1,
                        Realm = 1,
                        Id = 6,
                    }
                },
            }
        };
    }
}
