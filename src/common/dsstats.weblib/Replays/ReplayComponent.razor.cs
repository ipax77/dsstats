using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using pax.BlazorChartJs;

namespace dsstats.weblib.Replays;

public partial class ReplayComponent : ComponentBase, IAsyncDisposable
{
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public IPlayerService PlayerService { get; set; } = null!;

    [Parameter, EditorRequired]
    public ReplayDetails ReplayDetails { get; set; } = null!;

    [Parameter]
    public bool IsScrollable { get; set; }

    [Parameter]
    public bool IsCloseable { get; set; }
    [Parameter]
    public EventCallback<PlayerStatsResponse> OnPlayerRequest { get; set; }

    [Parameter]
    public EventCallback OnRatingUpdateRequest { get; set; }

    [Parameter]
    public EventCallback<bool> OnScrollRequest { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    private ReplayHelper _replayHelper = null!;
    private ReplayMiddleChart? replayMiddleChart;
    bool showTierUps;
    bool showRefineries;
    bool showLeavers;
    private Lazy<Task<IJSObjectReference>> moduleTask = null!;

    protected override void OnInitialized()
    {
        moduleTask = new(() => JSRuntime.InvokeAsync<IJSObjectReference>(
       "import", "./_content/dsstats.weblib/js/annotationChart.js?v=0.6").AsTask());
        _replayHelper = new ReplayHelper(ReplayDetails);
        base.OnInitialized();
    }

    public void Update(ReplayDetails replayDetails)
    {
        ReplayDetails = replayDetails;
        _replayHelper = new ReplayHelper(ReplayDetails);
    }

    public void Scroll(bool left)
    {
        OnScrollRequest.InvokeAsync(left);
    }

    private async Task RequestPlayerStats(PlayerDto player)
    {
        PlayerStatsRequest request = new()
        {
            ToonId = player.ToonId,
            Player = player,
            RatingType = _replayHelper.RatingType,
        };
        var stats = await PlayerService.GetPlayerStats(request);
        await OnPlayerRequest.InvokeAsync(stats);
    }

    public void Close()
    {
        OnClose.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value.ConfigureAwait(false);
            await module.DisposeAsync();
        }
    }
}


