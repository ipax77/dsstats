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
    public SpawnPlaybackSidecarCache SpawnPlaybackSidecarCache { get; set; } = null!;

    [Inject]
    public IOptions<HostOptions> HostOptions { get; set; } = null!;

    [Inject]
    public IOptions<ReplayUserRatingClientOptions> ReplayUserRatingClientOptions { get; set; } = null!;

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
    private Lazy<Task<IJSObjectReference>> replayUserRatingModuleTask = null!;
    private string? currentReplayHash;
    private ReplayDto? currentReplay;
    private bool showSpawnPlayback;
    private bool loadingSpawnPlayback;
    private bool spawnPlaybackLoadFailed;
    private SpawnPlaybackSidecarDto? spawnPlaybackSidecar;
    private List<ReplayPlayerDto> team1Players = [];
    private List<ReplayPlayerDto> team2Players = [];
    private ReplayUserRatingDto? replayUserRating;
    private int replayUserRatingHoverScore;
    private bool replayUserRatingLoadQueued;
    private bool replayUserRatingLoading;
    private bool replayUserRatingSubmitting;
    private string? replayUserRatingError;

    protected override void OnInitialized()
    {
        moduleTask = new(() => JSRuntime.InvokeAsync<IJSObjectReference>(
       "import", "./_content/dsstats.weblib/js/annotationChart.js?v=0.6").AsTask());
        replayUserRatingModuleTask = new(() => JSRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/dsstats.weblib/js/replayUserRating.js?v=0.1").AsTask());
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (replayUserRatingLoadQueued && IsReplayUserRatingEnabled)
        {
            replayUserRatingLoadQueued = false;
            await LoadReplayUserRatingAsync();
        }
    }

    private void SetReplayDetails(ReplayDetails replayDetails)
    {
        ReplayDetails = replayDetails;
        currentReplayHash = replayDetails.ReplayHash;
        currentReplay = replayDetails.Replay;
        _replayHelper = new ReplayHelper(ReplayDetails, SpawnPlaybackSidecarCache);
        team1Players = ReplayDetails.Replay.Players
            .Where(player => player.TeamId == 1)
            .OrderBy(player => player.GamePos)
            .ToList();
        team2Players = ReplayDetails.Replay.Players
            .Where(player => player.TeamId == 2)
            .OrderBy(player => player.GamePos)
            .ToList();
        showSpawnPlayback = false;
        loadingSpawnPlayback = false;
        spawnPlaybackLoadFailed = false;
        spawnPlaybackSidecar = null;
        replayUserRating = null;
        replayUserRatingHoverScore = 0;
        replayUserRatingError = null;
        replayUserRatingLoadQueued = IsReplayUserRatingEnabled;
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

    private bool IsReplayUserRatingEnabled =>
        HostOptions.Value.Kind == HostAppKind.BlazorServer
        && !string.IsNullOrWhiteSpace(ReplayDetails.ReplayHash);

    private bool CanSubmitReplayUserRating =>
        replayUserRating?.NextAllowedVoteAt is null
        || replayUserRating.NextAllowedVoteAt <= DateTime.UtcNow;

    private async Task LoadReplayUserRatingAsync()
    {
        if (!IsReplayUserRatingEnabled || replayUserRatingLoading)
        {
            return;
        }

        replayUserRatingLoading = true;
        replayUserRatingError = null;
        try
        {
            var module = await replayUserRatingModuleTask.Value;
            var result = await module.InvokeAsync<ReplayUserRatingFetchResult>(
                "getReplayUserRating",
                ReplayUserRatingClientOptions.Value.ApiBaseAddress,
                ReplayDetails.ReplayHash);

            if (result.Ok)
            {
                replayUserRating = result.Rating;
            }
            else if (result.Status != 404)
            {
                replayUserRatingError = "Rating unavailable";
            }
        }
        catch (JSDisconnectedException)
        {
        }
        catch (Exception)
        {
            replayUserRatingError = "Rating unavailable";
        }
        finally
        {
            replayUserRatingLoading = false;
            StateHasChanged();
        }
    }

    private async Task SubmitReplayUserRatingAsync(int score)
    {
        if (!IsReplayUserRatingEnabled || replayUserRatingSubmitting || !CanSubmitReplayUserRating)
        {
            return;
        }

        replayUserRatingSubmitting = true;
        replayUserRatingError = null;
        try
        {
            var module = await replayUserRatingModuleTask.Value;
            var result = await module.InvokeAsync<ReplayUserRatingFetchResult>(
                "submitReplayUserRating",
                ReplayUserRatingClientOptions.Value.ApiBaseAddress,
                ReplayDetails.ReplayHash,
                score);

            if (result.Rating is not null)
            {
                replayUserRating = result.Rating;
            }

            if (!result.Ok && result.Status != 429)
            {
                replayUserRatingError = "Vote failed";
            }
        }
        catch (JSDisconnectedException)
        {
        }
        catch (Exception)
        {
            replayUserRatingError = "Vote failed";
        }
        finally
        {
            replayUserRatingSubmitting = false;
        }
    }

    private string GetReplayUserRatingStarClass(int star)
    {
        var activeStars = replayUserRatingHoverScore > 0
            ? replayUserRatingHoverScore
            : replayUserRating?.CurrentVote
            ?? (replayUserRating is null ? 0 : (int)Math.Round(replayUserRating.Average, MidpointRounding.AwayFromZero));
        return star <= activeStars ? "bi-star-fill" : "bi-star";
    }

    private string GetReplayUserRatingText()
    {
        if (replayUserRatingError is not null)
        {
            return replayUserRatingError;
        }

        if (replayUserRatingLoading)
        {
            return "Loading";
        }

        if (replayUserRating is null || replayUserRating.VoteCount == 0)
        {
            return "No ratings";
        }

        return $"{replayUserRating.Average:N2} ({replayUserRating.VoteCount})";
    }

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

        if (replayUserRatingModuleTask.IsValueCreated)
        {
            try
            {
                var module = await replayUserRatingModuleTask.Value.ConfigureAwait(false);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            catch (TaskCanceledException) { }
        }
    }

    private sealed class ReplayUserRatingFetchResult
    {
        public bool Ok { get; set; }
        public int Status { get; set; }
        public ReplayUserRatingDto? Rating { get; set; }
    }
}


