using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.weblib.Replays;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace dsstats.tests;

[TestClass]
public class ReplayPaginationTests
{
    [TestMethod]
    public async Task PageChangeSurvivesReloadAndLoadsDifferentReplays()
    {
        await using var fixture = new Fixture();
        await fixture.Render();
        var first = await fixture.Load(0, 50);

        fixture.Request.Page = 2;
        await fixture.Reload();
        var second = await fixture.Load(0, 50);

        Assert.AreEqual(2, fixture.Request.Page);
        Assert.AreEqual(10, second.TotalItemCount);
        Assert.HasCount(10, second.Items);
        Assert.AreEqual("replay-0", first.Items.First().ReplayHash);
        Assert.AreEqual("replay-10", second.Items.First().ReplayHash);
        Assert.AreEqual((2, 0, 10), fixture.Reads.Last());

        fixture.Request.Page = 1;
        await fixture.Reload();
        Assert.AreEqual("replay-0", (await fixture.Load(0, 50)).Items.First().ReplayHash);
    }

    [TestMethod]
    public async Task LastPageUsesRemainingCountAndDoesNotReadPastItsEnd()
    {
        await using var fixture = new Fixture();
        await fixture.Render();
        fixture.Request.Page = 3;
        await fixture.Reload();

        var remaining = await fixture.Load(1, 50);
        Assert.AreEqual(3, remaining.TotalItemCount);
        CollectionAssert.AreEqual(new[] { "replay-21", "replay-22" },
            remaining.Items.Select(x => x.ReplayHash).ToArray());
        Assert.AreEqual((3, 1, 2), fixture.Reads.Single());

        var pastEnd = await fixture.Load(10, 50);
        Assert.AreEqual(3, pastEnd.TotalItemCount);
        Assert.IsEmpty(pastEnd.Items);
        Assert.HasCount(1, fixture.Reads);
    }

    [TestMethod]
    [DataRow(12, 2)]
    [DataRow(0, 1)]
    public async Task ReloadClampsPageWhenReplaysAreRemoved(int count, int expectedPage)
    {
        await using var fixture = new Fixture();
        await fixture.Render();
        fixture.Request.Page = 3;
        fixture.Count = count;

        await fixture.Reload();

        Assert.AreEqual(expectedPage, fixture.Request.Page);
    }

#pragma warning disable BL0006 // Inspect the real callbacks while omitting modal/JS rendering, as in the lifecycle tests.
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider services;
        private readonly PaginationRenderer renderer;
        public ReplaysRequest Request { get; } = new() { PageSize = 10 };
        public int Count { get; set; } = 23;
        public List<(int Page, int Skip, int Take)> Reads { get; } = [];

        public Fixture()
        {
            var repository = new Mock<IReplayRepository>();
            repository.Setup(x => x.GetReplaysCount(It.IsAny<ReplaysRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Count);
            repository.Setup(x => x.GetReplays(It.IsAny<ReplaysRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReplaysRequest request, CancellationToken _) =>
                {
                    Reads.Add((request.Page, request.Skip, request.Take));
                    int offset = (request.Page - 1) * request.PageSize + request.Skip;
                    return Enumerable.Range(offset, Math.Max(0, Math.Min(request.Take, Count - offset)))
                        .Select(i => new ReplayListDto { ReplayHash = $"replay-{i}" }).ToList();
                });
            services = new ServiceCollection().AddLogging().AddOptions()
                .AddSingleton(repository.Object).AddSingleton(Mock.Of<IJSRuntime>()).BuildServiceProvider();
            renderer = new PaginationRenderer(services);
        }

        public Task Render() => renderer.Dispatcher.InvokeAsync(() => renderer.Render(Request));
        public Task Reload() => renderer.Dispatcher.InvokeAsync(() => renderer.Component.Reload.InvokeAsync());
        public Task<ItemsProviderResult<ReplayListDto>> Load(int start, int count) =>
            renderer.Dispatcher.InvokeAsync(() => renderer.Component.Provider!(new(start, count, default)).AsTask());

        public async ValueTask DisposeAsync()
        {
            await renderer.DisposeAsync();
            await services.DisposeAsync();
        }
    }

    public sealed class HeadlessReplays : ReplaysComponent
    {
        public EventCallback Reload { get; private set; }
        public ItemsProviderDelegate<ReplayListDto>? Provider { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            using var tree = new RenderTreeBuilder();
            base.BuildRenderTree(tree);
            var frames = tree.GetFrames();
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames.Array[i];
                if (frame.FrameType != RenderTreeFrameType.Attribute) continue;
                if (frame.AttributeName == nameof(ReplaysRequestComponent.OnRequestChanged))
                    Reload = (EventCallback)frame.AttributeValue;
                if (frame.AttributeName == "ItemsProvider")
                    Provider = (ItemsProviderDelegate<ReplayListDto>)frame.AttributeValue;
            }
        }
    }

    private sealed class PaginationRenderer : Renderer
    {
        private readonly int componentId;
        public HeadlessReplays Component { get; }
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public PaginationRenderer(IServiceProvider services) : base(services, services.GetRequiredService<ILoggerFactory>())
        {
            Component = (HeadlessReplays)InstantiateComponent(typeof(HeadlessReplays));
            componentId = AssignRootComponentId(Component);
        }

        public Task Render(ReplaysRequest request) => RenderRootComponentAsync(componentId,
            ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(ReplaysComponent.ReplaysRequest)] = request }));
        protected override void HandleException(Exception exception) => throw new InvalidOperationException("Replay rendering failed.", exception);
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
#pragma warning restore BL0006
}
