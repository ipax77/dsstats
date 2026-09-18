using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.weblib.Players.Profile;
using Moq;

namespace dsstats.tests;

[TestClass]
public class PlayerProfileSessionTests
{
    [TestMethod]
    public async Task InitialViewIsOverviewOnly_ExpansionAndTabsLoadOnDemandAndCache()
    {
        var (profiles, stats) = Services();
        using var session = new PlayerProfileSession(profiles.Object, stats.Object);
        await session.Navigate(PlayerProfileTests.Request());
        profiles.Verify(p => p.GetDetails(It.IsAny<PlayerProfileRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.HasCount(0, stats.Invocations);
        await session.Expand();
        await session.Expand();
        profiles.Verify(p => p.GetDetails(It.IsAny<PlayerProfileRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        stats.Verify(s => s.GetUserStatsAsync<WinrateResponse>(It.IsAny<StatsType>(), It.IsAny<StatsRequest>(), It.IsAny<ToonIdDto>(), It.IsAny<CancellationToken>()), Times.Once);
        await session.SelectChart(StatsType.Count, TimePeriod.Last90Days);
        await session.SelectChart(StatsType.Winrate, TimePeriod.Last90Days);
        Assert.HasCount(2, stats.Invocations);
        await session.SelectSection("Breakdown");
        profiles.Verify(p => p.GetCommanderPerformance(It.IsAny<PlayerStatsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        await session.LoadGains(TimePeriod.AllTime);
        await session.LoadGains(TimePeriod.AllTime);
        profiles.Verify(p => p.GetCommanderPerformance(It.IsAny<PlayerStatsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RatingChangesKeepExpanded_AndBackRestoresSelectionsAndScroll()
    {
        var (profiles, stats) = Services();
        using var session = new PlayerProfileSession(profiles.Object, stats.Object);
        await session.Navigate(PlayerProfileTests.Request());
        await session.Expand();
        await session.SwitchRating(RatingType.Standard);
        Assert.IsTrue(session.Current!.Expanded);
        Assert.IsNotNull(session.Current.Details);
        session.Current.ScrollTop = 450;
        await session.SelectSection("Players");
        await session.Navigate(PlayerProfileTests.Request(2));
        await session.Back();
        Assert.AreEqual(RatingType.Standard, session.Current!.Request.RatingType);
        Assert.AreEqual("Players", session.Current.Section);
        Assert.AreEqual(450d, session.Current.ScrollTop);
        Assert.IsFalse(session.CanGoBack);
        profiles.Verify(p => p.GetOverview(It.IsAny<PlayerProfileRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [TestMethod]
    public async Task LateOverviewCannotReplaceNewPlayerOrStartTheirDetails()
    {
        var (profiles, stats) = Services();
        var pending = new TaskCompletionSource<PlayerOverview?>();
        CancellationToken oldToken = default;
        profiles.Setup(p => p.GetOverview(It.Is<PlayerProfileRequest>(r => r.ToonId.Id == 1), It.IsAny<CancellationToken>()))
            .Callback((PlayerProfileRequest _, CancellationToken token) => oldToken = token).Returns(pending.Task);
        using var session = new PlayerProfileSession(profiles.Object, stats.Object);
        var first = session.Navigate(PlayerProfileTests.Request());
        await session.Navigate(PlayerProfileTests.Request(2));
        pending.SetResult(new() { Name = "Old player" });
        await first;
        Assert.IsTrue(oldToken.IsCancellationRequested);
        Assert.AreEqual("Player2", session.Current!.Overview!.Name);
        profiles.Verify(p => p.GetDetails(It.IsAny<PlayerProfileRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task DetailFailureLeavesOverviewAndCanBeRetried()
    {
        var (profiles, stats) = Services();
        profiles.SetupSequence(p => p.GetDetails(It.IsAny<PlayerProfileRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException()).ReturnsAsync(new PlayerProfileDetails());
        using var session = new PlayerProfileSession(profiles.Object, stats.Object);
        await session.Navigate(PlayerProfileTests.Request());
        await session.Expand();
        Assert.IsNotNull(session.Current!.Overview);
        Assert.IsNotNull(session.Current.DetailsError);
        await session.Expand();
        Assert.IsNull(session.Current.DetailsError);
        Assert.IsNotNull(session.Current.Details);
    }

    [TestMethod]
    public async Task SupersededChartIsCancelledAndCannotOverwriteNewSelection()
    {
        var (profiles, stats) = Services();
        var pending = new TaskCompletionSource<WinrateResponse>();
        CancellationToken chartToken = default;
        stats.Setup(s => s.GetUserStatsAsync<WinrateResponse>(It.IsAny<StatsType>(), It.IsAny<StatsRequest>(), It.IsAny<ToonIdDto>(), It.IsAny<CancellationToken>()))
            .Callback((StatsType _, StatsRequest _, ToonIdDto _, CancellationToken token) => chartToken = token)
            .Returns(pending.Task);
        using var session = new PlayerProfileSession(profiles.Object, stats.Object);
        await session.Navigate(PlayerProfileTests.Request());
        var expanding = session.Expand();
        await session.SelectChart(StatsType.Count, TimePeriod.AllTime);
        pending.SetResult(new());
        await expanding;
        Assert.IsTrue(chartToken.IsCancellationRequested);
        Assert.AreEqual(StatsType.Count, session.Current!.ChartType);
        Assert.IsFalse(session.Current.ChartLoading);
        Assert.IsTrue(session.Current.Charts.ContainsKey((StatsType.Count, TimePeriod.AllTime)));
        Assert.IsFalse(session.Current.Charts.ContainsKey((StatsType.Winrate, TimePeriod.Last90Days)));
    }

    private static (Mock<IPlayerProfileService>, Mock<IStatsService>) Services()
    {
        var profiles = new Mock<IPlayerProfileService>();
        profiles.Setup(p => p.GetOverview(It.IsAny<PlayerProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfileRequest request, CancellationToken _) => new PlayerOverview { Name = $"Player{request.ToonId.Id}", ToonId = request.ToonId, RatingType = request.RatingType });
        profiles.Setup(p => p.GetDetails(It.IsAny<PlayerProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerProfileDetails { CommanderPerformance = new() { TimePeriod = TimePeriod.Last90Days } });
        profiles.Setup(p => p.GetCommanderPerformance(It.IsAny<PlayerStatsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerStatsRequest request, CancellationToken _) => new CmdrAvgGainResponse { TimePeriod = request.TimePeriod });
        var stats = new Mock<IStatsService>();
        stats.Setup(s => s.GetUserStatsAsync<WinrateResponse>(It.IsAny<StatsType>(), It.IsAny<StatsRequest>(), It.IsAny<ToonIdDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(new WinrateResponse());
        stats.Setup(s => s.GetUserStatsAsync<CountResponse>(It.IsAny<StatsType>(), It.IsAny<StatsRequest>(), It.IsAny<ToonIdDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CountResponse());
        return (profiles, stats);
    }
}
