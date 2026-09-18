using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;

namespace dsstats.tests;

[TestClass]
public class PlayerRatingDetailsCalculatorTests
{
    private static readonly DateTime Today = new(2026, 9, 17);

    [TestMethod]
    public void EmptyHistory_ReturnsEmptyStatistics()
    {
        var result = Calculate([]);
        Assert.IsEmpty(result.Replays);
        Assert.IsEmpty(result.Ratings);
        Assert.IsEmpty(result.Commanders);
        Assert.IsEmpty(result.AvgGainResponses.Single().AvgGains);
        Assert.AreEqual(0, result.CurrentStreak!.Count);
        Assert.AreEqual(0, result.AvgOpponentRating);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(11)]
    [DataRow(12)]
    [DataRow(13)]
    [DataRow(30)]
    public void RecentReplays_ReturnUpToTwelveInReverseChronologicalOrder(int count)
    {
        var replays = Enumerable.Range(1, count).Select(i => Replay(i, Today.AddMinutes(i))).ToList();
        var result = Calculate(replays);
        CollectionAssert.AreEqual(
            Enumerable.Range(1, count).Reverse().Take(12).Select(i => i.ToString()).ToArray(),
            result.Replays.Select(r => r.ReplayHash).ToArray());
    }

    [TestMethod]
    public void Streaks_SkipMissingRatingsAndEndBeforeTheBreakingGame()
    {
        var replays = Enumerable.Range(1, 7)
            .Select(i => Replay(i, Today.AddDays(i), i is 2 or 3 or 7 ? 1 : 2)).ToList();
        var ratings = replays.Skip(1).ToDictionary(r => r.ReplayId, r => Rating(r.ReplayId));
        // First replay has no rating; another has no rating for the selected player.
        ratings[4] = new() { ReplayId = 4 };
        var result = PlayerRatingDetailsCalculator.Calculate(replays, ratings, 1, Today);
        Assert.AreEqual(2, result.LongestWinStreak.Count);
        Assert.AreEqual(replays[1].Gametime, result.LongestWinStreak.StartDate);
        Assert.AreEqual(replays[2].Gametime, result.LongestWinStreak.EndDate);
        Assert.AreEqual(2, result.LongestLoseStreak.Count);
        Assert.AreEqual(replays[4].Gametime, result.LongestLoseStreak.StartDate);
        Assert.AreEqual(replays[5].Gametime, result.LongestLoseStreak.EndDate);
        Assert.AreEqual(1, result.CurrentStreak!.Count);
        Assert.AreEqual(replays[6].Gametime, result.CurrentStreak.StartDate);
    }

    [TestMethod]
    public void InitialLossAfterUnratedReplay_HasValidDatesAndNegativeCurrentCount()
    {
        var replays = new List<PlayerReplayData> { Replay(1, Today), Replay(2, Today.AddDays(1), 2) };
        var result = PlayerRatingDetailsCalculator.Calculate(replays, new() { [2] = Rating(2) }, 1, Today);
        Assert.AreEqual(-1, result.CurrentStreak!.Count);
        Assert.AreEqual(replays[1].Gametime, result.CurrentStreak.StartDate);
        Assert.AreEqual(replays[1].Gametime, result.CurrentStreak.EndDate);
    }

    [TestMethod]
    public void WeeklyHistory_UsesIsoYearAndLastRatingIncludingTiedTimestamps()
    {
        List<PlayerReplayData> replays =
        [
            Replay(1, new(2021, 1, 1)),
            Replay(2, new(2021, 1, 3)),
            Replay(3, new(2021, 1, 4)),
            Replay(4, new(2021, 1, 4))
        ];
        var ratings = replays.ToDictionary(r => r.ReplayId, r => Rating(r.ReplayId, r.ReplayId));
        var result = PlayerRatingDetailsCalculator.Calculate(replays, ratings, 1, Today);
        Assert.HasCount(2, result.Ratings);
        Assert.AreEqual((2020, 53, 1002f, 2),
            (result.Ratings[0].Year, result.Ratings[0].Week, result.Ratings[0].Rating, result.Ratings[0].Games));
        Assert.AreEqual((2021, 1, 1004f, 4),
            (result.Ratings[1].Year, result.Ratings[1].Week, result.Ratings[1].Rating, result.Ratings[1].Games));
        Assert.AreEqual(1004d, result.TopRating.Rating);
        Assert.AreEqual("4", result.Replays[0].ReplayHash);
    }

    [TestMethod]
    public void CommanderGains_UseInclusiveNinetyDayBoundaryAndNormalize()
    {
        List<PlayerReplayData> replays =
        [
            Replay(1, Today.AddDays(-91)),
            Replay(2, Today.AddDays(-90)),
            Replay(3, Today, 2)
        ];
        var ratings = new Dictionary<int, PlayerReplayRatingData>
        {
            [1] = Rating(1, 90), [2] = Rating(2, 8), [3] = Rating(3, -3)
        };
        var result = PlayerRatingDetailsCalculator.Calculate(replays, ratings, 1, Today);
        var response = result.AvgGainResponses.Single();
        Assert.AreEqual(TimePeriod.Last90Days, response.TimePeriod);
        var gain = response.AvgGains.Single();
        Assert.AreEqual((2, 1, 2.5), (gain.Count, gain.Wins, gain.AvgGain));
        Assert.AreEqual(3, result.Commanders.Single().Count);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(11)]
    public void OtherPlayers_KeepTheirWinrateAndSelectedPlayersAverageGain(int count)
    {
        var replays = Enumerable.Range(1, count).Select(i => Replay(i, Today.AddDays(-i)))
            .OrderBy(r => r.Gametime).ToList();
        var result = Calculate(replays);
        Assert.AreEqual(1200, result.AvgTeammateRating);
        Assert.AreEqual(1400, result.AvgOpponentRating);
        if (count == 10)
        {
            Assert.IsEmpty(result.TeammateStats);
            Assert.IsEmpty(result.OpponentStats);
            return;
        }
        Assert.AreEqual(count, result.TeammateStats.Single().Wins);
        Assert.AreEqual(0, result.OpponentStats.Single().Wins);
        Assert.AreEqual(5f, result.TeammateStats.Single().AvgGain);
        Assert.AreEqual(5f, result.OpponentStats.Single().AvgGain);
    }

    private static RatingDetails Calculate(List<PlayerReplayData> replays) =>
        PlayerRatingDetailsCalculator.Calculate(replays,
            replays.ToDictionary(r => r.ReplayId, r => Rating(r.ReplayId)), 1, Today);

    private static PlayerReplayData Replay(int id, DateTime date, int winner = 1) => new()
    {
        ReplayId = id, ReplayHash = id.ToString(), Gametime = date, WinnerTeam = winner,
        Players = Enumerable.Range(1, 3).Select(i => new PlayerReplayParticipantData
        {
            PlayerId = i, TeamId = i == 3 ? 2 : 1, GamePos = i, Race = Commander.Abathur,
            ToonId = new ToonId { Id = i, Region = 1, Realm = 1 }
        }).ToList()
    };

    private static PlayerReplayRatingData Rating(int id, double delta = 5) => new()
    {
        ReplayId = id,
        PlayerRatings =
        [
            new() { PlayerId = 1, RatingBefore = 1000, RatingDelta = delta, Games = id },
            new() { PlayerId = 2, RatingBefore = 1200, RatingDelta = 99 },
            new() { PlayerId = 3, RatingBefore = 1400, RatingDelta = -99 }
        ]
    };
}
