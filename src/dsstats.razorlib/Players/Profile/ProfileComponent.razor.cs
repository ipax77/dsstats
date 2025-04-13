using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace dsstats.razorlib.Players.Profile;

public partial class ProfileComponent : ComponentBase
{
    [Inject]
    public IPlayerService PlayerService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    public IRemoteToggleService RemoteToggleService { get; set; } = default!;

    [Inject]
    public ILogger<ProfileComponent> Logger { get; set; } = default!;

    [Parameter, EditorRequired]
    public PlayerId PlayerId { get; set; } = default!;

    [Parameter, EditorRequired]
    public RatingNgType RatingType { get; set; } = RatingNgType.Global;

    [Parameter]
    public EventCallback OnCloseRequested { get; set; }
    [Parameter]
    public EventCallback<PlayerReplaysRequest> OnPlayerReplaysRequested { get; set; }

    bool isLoading;
    PlayerDetailSummary summary = new();
    PlayerRatingDetailChart? playerRatingDetailChart;
    PlayerCmdrCounts? playerCmdrCounts;
    PlayerCmdrsAvgGainComponent? playerCmdrsAvgGainComponent;
    string name = string.Empty;
    bool isUploader;
    PlayerRatingDetailDto? interestRating;
    bool shouldRender = true;
    PlayerRatingDetails? ratingDetails = null;

    protected override bool ShouldRender()
    {
        return shouldRender;
    }
    protected override void OnInitialized()
    {
        _ = LoadData();
        base.OnInitialized();
    }

    private async Task LoadData(bool forceDetailChartRefresh = false)
    {
        isLoading = true;
        await InvokeAsync(() => StateHasChanged());
        try
        {
            summary = await PlayerService.GetPlayerPlayerIdSummary(PlayerId, RatingType);

            if (summary.Ratings.Count > 0)
            {
                interestRating = summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType);
                isUploader = summary.Ratings[0].Player.IsUploader;
                name = summary.Ratings[0].Player.Name;
                if (interestRating != null)
                {
                    playerRatingDetailChart?.Update(RatingType,interestRating.Rating
                        , forceDetailChartRefresh);
                    playerCmdrCounts?.Update(RatingType);
                }
            }
            ratingDetails = null;
        } catch (Exception ex)
        {
            Logger.LogError("failed loading data: {error}", ex.Message);
        }
        isLoading = false;
        await InvokeAsync(() => StateHasChanged());
    }

    private async Task LoadDetailData()
    {
        isLoading = true;
        await InvokeAsync(() => StateHasChanged());
        ratingDetails = await PlayerService.GetPlayerIdPlayerRatingDetails(PlayerId, RatingType);
        isLoading = false;
        await InvokeAsync(() => StateHasChanged());
        await JSRuntime.InvokeVoidAsync("scrollToElementId", "playerdetails");
    }

    private async Task LoadCmdrAvgGain(TimePeriod timePeriod)
    {
        if (ratingDetails == null)
        {
            return;
        }
        isLoading = true;
        await InvokeAsync(() => StateHasChanged());

        var cmdrsAvgGain = await PlayerService.GetPlayerIdPlayerCmdrAvgGain(PlayerId, RatingType, timePeriod, default);
        ratingDetails.CmdrsAvgGain.Clear();
        ratingDetails.CmdrsAvgGain.AddRange(cmdrsAvgGain);
        isLoading = false;
        await InvokeAsync(() => StateHasChanged());
    }

    public void Update(PlayerId playerId, RatingNgType ratingType, bool force = false)
    {
        PlayerId = playerId;
        RatingType = ratingType;
        _ = LoadData(force);
    }

    private void ChangeRating(PlayerRatingDetailDto rating)
    {
        shouldRender = false;
        RatingType = rating.RatingType;
        interestRating = rating;
        playerRatingDetailChart?.Update(RatingType, rating.Rating);
        playerCmdrCounts?.Update(RatingType);
        shouldRender = true;
        ratingDetails = null;
        InvokeAsync(() => StateHasChanged());
    }

    private void ToggleFromServer(ChangeEventArgs e)
    {
        if (e.Value is bool value)
        {
            RemoteToggleService.SetFromServer(value);
            _ = LoadData();
        }
    }

    private void ShowReplays()
    {
        var currentUri = new Uri(NavigationManager.Uri);

        if (currentUri.AbsolutePath.EndsWith("/replays", StringComparison.OrdinalIgnoreCase))
        {
            OnPlayerReplaysRequested.InvokeAsync(new() { PlayerId = PlayerId });
        }
        else
        {
            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters("replays",
            new Dictionary<string, object?>()
                {
                {"PlayerId", Data.GetPlayerIdString(PlayerId) }
                }
            ));
        }
    }

    private void ShowVsReplays(PlayerId playerIdvs)
    {
        var currentUri = new Uri(NavigationManager.Uri);

        if (currentUri.AbsolutePath.EndsWith("/replays", StringComparison.OrdinalIgnoreCase))
        {
            OnPlayerReplaysRequested.InvokeAsync(new() { PlayerId = PlayerId, PlayerIdVs = playerIdvs });
        }
        else
        {
            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters("replays",
            new Dictionary<string, object?>()
                {
                    {"PlayerId", Data.GetPlayerIdString(PlayerId) },
                    {"Vs", Data.GetPlayerIdString(playerIdvs) },
                }
            ), true);
        }
    }

    private void ShowWithReplays(PlayerId playerIdwith)
    {
        var currentUri = new Uri(NavigationManager.Uri);

        if (currentUri.AbsolutePath.EndsWith("/replays", StringComparison.OrdinalIgnoreCase))
        {
            OnPlayerReplaysRequested.InvokeAsync(new() { PlayerId = PlayerId, PlayerIdWith = playerIdwith });
        }
        else
        {
            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters("replays",
            new Dictionary<string, object?>()
                {
                    {"PlayerId", Data.GetPlayerIdString(PlayerId) },
                    {"With", Data.GetPlayerIdString(playerIdwith) },
                }
            ), true);
        }
    }

    private void ShowReplaysWithReplay(string replayHash)
    {
        var currentUri = new Uri(NavigationManager.Uri);

        if (currentUri.AbsolutePath.EndsWith("/replays", StringComparison.OrdinalIgnoreCase))
        {
            OnPlayerReplaysRequested.InvokeAsync(new() { PlayerId = PlayerId, ReplayHash = replayHash });
        }
        else
        {
            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters("replays",
            new Dictionary<string, object?>()
                {
                    {"PlayerId", Data.GetPlayerIdString(PlayerId) },
                    {"Replay", replayHash },
                }
            ), true);
        }
    }

    private void ShowReview()
    {
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters("review/2024",
        new Dictionary<string, object?>()
            {
                            {"PlayerId", Data.GetPlayerIdString(PlayerId) },
                            {"Name", name }
            }
        ), true);
    }
}