using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;
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

    [Parameter, EditorRequired]
    public PlayerId PlayerId { get; set; } = default!;

    [Parameter, EditorRequired]
    public RatingType RatingType { get; set; } = RatingType.Cmdr;

    [Parameter, EditorRequired]
    public RatingCalcType RatingCalcType { get; set; } = RatingCalcType.Combo;

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
        if (RemoteToggleService.IsMaui)
        {
            RatingCalcType = RatingCalcType.Dsstats;
        }
        _ = LoadData();
        base.OnInitialized();
    }

    private async Task LoadData(bool forceDetailChartRefresh = false)
    {
        isLoading = true;
        await InvokeAsync(() => StateHasChanged());

        summary = await PlayerService.GetPlayerPlayerIdSummary(PlayerId, RatingType, RatingCalcType);

        if (summary.Ratings.Count > 0)
        {
            interestRating = summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType);
            isUploader = summary.Ratings[0].Player.IsUploader;
            name = summary.Ratings[0].Player.Name;
            if (interestRating != null)
            {
                playerRatingDetailChart?.Update(RatingType, RatingCalcType, RatingCalcType == RatingCalcType.Combo ? 0 : interestRating.Rating
                    , forceDetailChartRefresh);
                playerCmdrCounts?.Update(RatingType);
            }
        }
        ratingDetails = null;
        isLoading = false;
        await InvokeAsync(() => StateHasChanged());
    }

    private async Task LoadDetailData()
    {
        isLoading = true;
        await InvokeAsync(() => StateHasChanged());
        ratingDetails = await PlayerService.GetPlayerIdPlayerRatingDetails(PlayerId, RatingType, RatingCalcType);
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

    public void Update(PlayerId playerId, RatingCalcType ratingCalcType, RatingType ratingType, bool force = false)
    {
        PlayerId = playerId;
        RatingCalcType = ratingCalcType;
        RatingType = ratingType;
        _ = LoadData(force);
    }

    private void ChangeRating(PlayerRatingDetailDto rating)
    {
        shouldRender = false;
        RatingType = rating.RatingType;
        interestRating = rating;
        playerRatingDetailChart?.Update(RatingType, RatingCalcType, rating.Rating);
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
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters("review/2023",
        new Dictionary<string, object?>()
            {
                            {"PlayerId", Data.GetPlayerIdString(PlayerId) },
                            {"Name", name }
            }
        ), true);
    }
}