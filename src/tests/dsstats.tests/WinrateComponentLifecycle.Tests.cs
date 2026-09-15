using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.weblib.Stats;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace dsstats.tests;

[TestClass]
public class WinrateComponentLifecycleTests
{
    [TestMethod]
    public async Task LoadFailureReportsRequestAndReplacementCanRecover()
    {
        var first = new StatsRequest { Type = StatsType.Winrate };
        var second = new StatsRequest { Type = StatsType.Winrate, Interest = Commander.Nova };
        var stats = new Mock<IStatsService>();
        stats.Setup(x => x.GetStatsAsync<WinrateResponse>(StatsType.Winrate, first, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Offline"));
        stats.Setup(x => x.GetStatsAsync<WinrateResponse>(StatsType.Winrate, second, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WinrateResponse());
        using var services = new ServiceCollection().AddLogging().AddSingleton(stats.Object)
            .AddSingleton(Mock.Of<IPatchNotesService>()).BuildServiceProvider();
        await using var renderer = new LifecycleRenderer(services);
        var failed = new TaskCompletionSource<StatsRequest>();
        var recovered = new TaskCompletionSource<StatsRequest>();
        ParameterView Parameters(StatsRequest request) => ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(WinrateComponent.StatsRequest)] = request,
            [nameof(WinrateComponent.OnLoadFailed)] = EventCallback.Factory.Create<StatsRequest>(this, r => failed.TrySetResult(r)),
            [nameof(WinrateComponent.OnResponseLoaded)] = EventCallback.Factory.Create<(StatsRequest Request, WinrateResponse Response)>(this,
                r => recovered.TrySetResult(r.Request))
        });
        await renderer.Dispatcher.InvokeAsync(() => renderer.Render(Parameters(first)));
        Assert.AreSame(first, await failed.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        await renderer.Dispatcher.InvokeAsync(() => renderer.Render(Parameters(second)));
        Assert.AreSame(second, await recovered.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.IsNull(renderer.Error);
    }

    [TestMethod]
    public async Task ReplacementCancelsOldLoadAndOnlyReportsCurrentResponse()
    {
        var first = new StatsRequest { Type = StatsType.Winrate };
        var second = new StatsRequest { Type = StatsType.Winrate, Interest = Commander.Kerrigan };
        var firstLoad = new TaskCompletionSource<WinrateResponse>();
        var secondLoad = new TaskCompletionSource<WinrateResponse>();
        CancellationToken oldToken = default;
        var stats = new Mock<IStatsService>();
        stats.Setup(x => x.GetStatsAsync<WinrateResponse>(StatsType.Winrate, first, It.IsAny<CancellationToken>()))
            .Returns((StatsType _, StatsRequest _, CancellationToken token) => { oldToken = token; return firstLoad.Task; });
        stats.Setup(x => x.GetStatsAsync<WinrateResponse>(StatsType.Winrate, second, It.IsAny<CancellationToken>()))
            .Returns(secondLoad.Task);
        using var services = new ServiceCollection().AddLogging().AddSingleton(stats.Object)
            .AddSingleton(Mock.Of<IPatchNotesService>()).BuildServiceProvider();
        await using var renderer = new LifecycleRenderer(services);
        var reported = new List<StatsRequest>();
        var completed = new TaskCompletionSource();
        var callback = EventCallback.Factory.Create<(StatsRequest Request, WinrateResponse Response)>(this,
            result => { reported.Add(result.Request); completed.TrySetResult(); });
        ParameterView Parameters(StatsRequest request) => ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(WinrateComponent.StatsRequest)] = request,
            [nameof(WinrateComponent.OnResponseLoaded)] = callback
        });
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.Render(Parameters(first));
            await renderer.Render(Parameters(second));
            Assert.IsTrue(oldToken.IsCancellationRequested);
            firstLoad.SetResult(new());
            secondLoad.SetResult(new());
        });
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.Render(Parameters(second));
            Assert.HasCount(1, reported);
            Assert.AreSame(second, reported[0]);
        });
        stats.Verify(x => x.GetStatsAsync<WinrateResponse>(StatsType.Winrate, second, It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsNull(renderer.Error);
    }

    // Exercise the real load/parameter lifecycle without instantiating charts or making JS calls.
    public sealed class HeadlessWinrate : WinrateComponent
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

#pragma warning disable BL0006 // A minimal renderer is intentionally used for lifecycle tests.
    private sealed class LifecycleRenderer : Renderer
    {
        private readonly int componentId;
        public Exception? Error { get; private set; }
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public LifecycleRenderer(IServiceProvider services) : base(services, services.GetRequiredService<ILoggerFactory>())
            => componentId = AssignRootComponentId(InstantiateComponent(typeof(HeadlessWinrate)));
        public Task Render(ParameterView parameters) => RenderRootComponentAsync(componentId, parameters);
        protected override void HandleException(Exception exception) => Error = exception;
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
#pragma warning restore BL0006
}
