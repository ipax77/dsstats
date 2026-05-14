using dsstats.shared;
using dsstats.shared.Maui;

namespace dsstats.tests;

[TestClass]
public class MauiSessionProgressCalculatorTests
{
    [TestMethod]
    public void NormalizeSettings_UsesSupportedDefaults()
    {
        var config = new MauiConfigDto
        {
            SessionWindowMode = (MauiSessionWindowMode)999,
            SessionWindowHours = 5,
            SessionWindowReplayCount = 42,
            SessionWindowGameMode = (GameMode)999,
        };

        var normalized = MauiSessionProgressCalculator.NormalizeSettings(config);

        Assert.AreEqual(MauiSessionWindowMode.Time, normalized.SessionWindowMode);
        Assert.AreEqual(6, normalized.SessionWindowHours);
        Assert.AreEqual(10, normalized.SessionWindowReplayCount);
        Assert.AreEqual(GameMode.None, normalized.SessionWindowGameMode);
    }

    [TestMethod]
    public void NormalizeSettings_PreservesExplicitGameMode()
    {
        var config = new MauiConfigDto
        {
            SessionWindowMode = MauiSessionWindowMode.Time,
            SessionWindowHours = 6,
            SessionWindowReplayCount = 10,
            SessionWindowGameMode = GameMode.Sabotage,
        };

        var normalized = MauiSessionProgressCalculator.NormalizeSettings(config);

        Assert.AreEqual(GameMode.Sabotage, normalized.SessionWindowGameMode);
    }

    [TestMethod]
    public void GetRatingGain_ReturnsMatchingProfileDelta()
    {
        var profile = Toon(1, 2, 3);
        var rating = new ReplayRatingDto
        {
            ReplayPlayerRatings =
            [
                new() { ToonId = Toon(1, 2, 4), RatingDelta = -12.5 },
                new() { ToonId = profile, RatingDelta = 17.25 },
            ],
        };

        var gain = MauiSessionProgressCalculator.GetRatingGain(profile, rating);

        Assert.AreEqual(17.25, gain);
        Assert.AreEqual(0, MauiSessionProgressCalculator.GetRatingGain(Toon(9, 9, 9), rating));
        Assert.AreEqual(0, MauiSessionProgressCalculator.GetRatingGain(profile, null));
    }

    [TestMethod]
    public void IsWin_UsesTrackedPlayersTeam()
    {
        var winner = Toon(1, 1, 1);
        var loser = Toon(1, 1, 2);
        var replay = new ReplayDto
        {
            WinnerTeam = 1,
            Players =
            [
                Player(winner, teamId: 1),
                Player(loser, teamId: 2),
            ],
        };

        Assert.IsTrue(MauiSessionProgressCalculator.IsWin(replay, winner));
        Assert.IsFalse(MauiSessionProgressCalculator.IsWin(replay, loser));
        Assert.IsFalse(MauiSessionProgressCalculator.IsWin(replay, Toon(9, 9, 9)));
    }

    [TestMethod]
    public void MatchesToonId_RequiresAllParts()
    {
        var toon = Toon(1, 2, 3);

        Assert.IsTrue(MauiSessionProgressCalculator.MatchesToonId(toon, Toon(1, 2, 3)));
        Assert.IsFalse(MauiSessionProgressCalculator.MatchesToonId(toon, Toon(2, 2, 3)));
        Assert.IsFalse(MauiSessionProgressCalculator.MatchesToonId(toon, Toon(1, 3, 3)));
        Assert.IsFalse(MauiSessionProgressCalculator.MatchesToonId(toon, Toon(1, 2, 4)));
    }

    private static ToonIdDto Toon(int region, int realm, int id)
        => new()
        {
            Region = region,
            Realm = realm,
            Id = id,
        };

    private static ReplayPlayerDto Player(ToonIdDto toonId, int teamId)
        => new()
        {
            TeamId = teamId,
            Player = new PlayerDto
            {
                ToonId = toonId,
            },
        };
}
