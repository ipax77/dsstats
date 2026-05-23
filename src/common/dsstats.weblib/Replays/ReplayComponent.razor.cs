using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using pax.BlazorChartJs;

namespace dsstats.weblib.Replays;

public partial class ReplayComponent : ComponentBase, IAsyncDisposable
{
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public IPlayerService PlayerService { get; set; } = null!;

    [Inject]
    public IReplayRepository ReplayRepository { get; set; } = null!;

    [Inject]
    public IOptions<HostOptions> HostOptions { get; set; } = null!;

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
    private string? currentReplayHash;
    private ReplayDto? currentReplay;
    private bool showSpawnPlayback;
    private bool loadingSpawnPlayback;
    private bool spawnPlaybackLoadFailed;
    private SpawnPlaybackSidecarDto? spawnPlaybackSidecar;

    protected override void OnInitialized()
    {
        moduleTask = new(() => JSRuntime.InvokeAsync<IJSObjectReference>(
       "import", "./_content/dsstats.weblib/js/annotationChart.js?v=0.6").AsTask());
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (_replayHelper is null || currentReplayHash != ReplayDetails.ReplayHash || ReplayHashIsMissingAndReplayChanged())
        {
            SetReplayDetails(ReplayDetails);
        }
    }

    public void Update(ReplayDetails replayDetails)
    {
        SetReplayDetails(replayDetails);
    }

    private void SetReplayDetails(ReplayDetails replayDetails)
    {
        ReplayDetails = replayDetails;
        currentReplayHash = replayDetails.ReplayHash;
        currentReplay = replayDetails.Replay;
        _replayHelper = new ReplayHelper(ReplayDetails);
        showSpawnPlayback = false;
        loadingSpawnPlayback = false;
        spawnPlaybackLoadFailed = false;
        spawnPlaybackSidecar = null;
    }

    private bool ReplayHashIsMissingAndReplayChanged()
    {
        return string.IsNullOrEmpty(ReplayDetails.ReplayHash)
            && !ReferenceEquals(currentReplay, ReplayDetails.Replay);
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

    private bool HasSpawnPlayback => ReplayDetails.Replay.SpawnPlayback?.Available == true;

    private async Task ToggleSpawnPlayback()
    {
        if (!HasSpawnPlayback)
        {
            return;
        }

        if (showSpawnPlayback)
        {
            showSpawnPlayback = false;
            return;
        }

        showSpawnPlayback = true;
        spawnPlaybackLoadFailed = false;
        if (spawnPlaybackSidecar is not null)
        {
            return;
        }

        loadingSpawnPlayback = true;
        try
        {
            spawnPlaybackSidecar = await _replayHelper.GetSpawnPlaybackSidecarAsync(ReplayRepository);
            spawnPlaybackLoadFailed = spawnPlaybackSidecar is null;
        }
        finally
        {
            loadingSpawnPlayback = false;
        }
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
            try
            {
                var module = await moduleTask.Value.ConfigureAwait(false);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected (e.g. page closed) — JS interop is no longer available.
            }
            catch (TaskCanceledException) { }
        }
    }
}


