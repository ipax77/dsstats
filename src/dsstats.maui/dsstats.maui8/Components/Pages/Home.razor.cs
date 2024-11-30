using Blazored.Toast.Services;
using dsstats.db8services;
using dsstats.maui8.Services;
using dsstats.razorlib.Players.Profile;
using dsstats.razorlib.Replays;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace dsstats.maui8.Components.Pages;

public partial class Home : ComponentBase, IDisposable
{
    [Inject]
    public IReplayRepository replayRepository { get; set; } = default!;
    [Inject]
    public ConfigService configService { get; set; } = default!;
    [Inject]
    public DsstatsService dsstatsService { get; set; } = default!;
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    public IToastService toastService { get; set; } = default!;
    [Inject]
    public IRemoteToggleService remoteToggleService { get; set; } = default!;
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    ReplayDto? currentReplay = null;
    PlayerId? interestPlayer = null;
    bool isLatestreplay = true;
    SessionComponent? sessionComponent;
    bool showSessionProgress = true;
    bool showPlayers = true;
    // PlayerDetails? playerDetails;
    ProfileComponent? playerDetails;
    AppPlayersComponent? appPlayersComponent;
    ReplayComponent? replayComponent;
    bool DEBUG = false;
    bool isChartAnnotationPluginRegistered;

    protected override void OnInitialized()
    {
        _ = LoadLatestReplay();
        dsstatsService.DecodeStateChanged += DssstatsService_DecodeStateChanged;
        remoteToggleService.CultureChanged += RemoteToggleService_CultureChanged;
        base.OnInitialized();
    }

    //protected override async Task OnAfterRenderAsync(bool firstRender)
    //{
    //    if (firstRender)
    //    {
    //        var module = await moduleTask.Value.ConfigureAwait(false);
    //        await module.InvokeVoidAsync("registerPlugin");
    //        isChartAnnotationPluginRegistered = true;
    //        await InvokeAsync(() => StateHasChanged());
    //    }
    //    await base.OnAfterRenderAsync(firstRender);
    //}

    private void RemoteToggleService_CultureChanged(object? sender, EventArgs e)
    {
        InvokeAsync(() => StateHasChanged());
    }

    private void DssstatsService_DecodeStateChanged(object? sender, DecodeInfoEventArgs e)
    {
        if (e.Finished)
        {
            _ = LoadLatestReplay(true);
        }
        InvokeAsync(() => StateHasChanged());
    }

    private async Task LoadLatestReplay(bool afterDecode = false)
    {
        currentReplay = await replayRepository.GetLatestReplay();

        if (currentReplay is null)
        {
            return;
        }

        isLatestreplay = true;

        var appPlayers = configService.GetRequestNames()
            .Select(s => new PlayerId(s.ToonId, s.RealmId, s.RegionId))
            .ToList();

        var repPlayers = currentReplay.ReplayPlayers
            .Select(s => new PlayerId(s.Player.ToonId, s.Player.RealmId, s.Player.RegionId))
            .ToList();

        interestPlayer = repPlayers.FirstOrDefault(f => appPlayers.Contains(f));

        if (interestPlayer is not null)
        {
            playerDetails?.Update(interestPlayer,
                RatingCalcType.Dsstats,
                Data.GetReplayRatingType(currentReplay.GameMode, currentReplay.TournamentEdition));
            appPlayersComponent?.UpdatePlayer(interestPlayer);
        }
        replayComponent?.Init();
        sessionComponent?.Update();
    }

    private async Task LoadNextReplay(bool next)
    {
        if (currentReplay is null)
        {
            return;
        }

        if (next)
        {
            var nextReplay = await replayRepository
                .GetNextReplay(currentReplay.GameTime);
            if (nextReplay is null)
            {
                isLatestreplay = true;
                return;
            }
            currentReplay = nextReplay;
        }
        else
        {
            var prevReplay = await replayRepository
                .GetPreviousReplay(currentReplay.GameTime);
            if (prevReplay is null)
            {
                return;
            }
            currentReplay = prevReplay;
        }
        isLatestreplay = false;
        await InvokeAsync(() => StateHasChanged());
    }

    private async Task LoadSessionReplay(string replayHash)
    {
        var sessionReplay = await replayRepository.GetReplay(replayHash);
        if (sessionReplay is null)
        {
            return;
        }
        currentReplay = sessionReplay;
        await InvokeAsync(() => StateHasChanged());
    }

    private void Upload()
    {
        if (!configService.AppOptions.UploadCredential)
        {
            toastService.ShowWarning("Upload is disabled. Please enable replay upload in the settings.");
            NavigationManager.NavigateTo("/settings");
        }
        else
        {
            _ = dsstatsService.UploadReplays();
        }
    }

    private void PlayerRequest(PlayerId playerId)
    {
        interestPlayer = playerId;
        var ratingType = currentReplay is null ? RatingType.Cmdr : Data.GetReplayRatingType(currentReplay.GameMode, currentReplay.TournamentEdition);
        playerDetails?.Update(playerId, RatingCalcType.Dsstats, ratingType, true);
    }

    private void AnnotationsRegistered()
    {
        InvokeAsync(() =>
        {
            isChartAnnotationPluginRegistered = true;
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        dsstatsService.DecodeStateChanged -= DssstatsService_DecodeStateChanged;
        remoteToggleService.CultureChanged -= RemoteToggleService_CultureChanged;
    }
}
