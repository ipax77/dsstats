using Blazored.Toast.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared.Raven;

namespace sc2dsstats.maui.Services;

internal class HubService : IDisposable
{
    private readonly IToastService toastService;
    private readonly ILogger<HubService> logger;
    // private static readonly Uri hubUrl = new Uri("https://dsstats.pax77.org/hubs/maui");
    private static readonly Uri hubUrl = new Uri("https://localhost:7174/hubs/maui");
    private HubConnection hubConnection = null!;

    public event EventHandler<MmrChangedEvent>? MmrChanged;
    protected virtual void OnMmrChanged(MmrChangedEvent e)
    {
        EventHandler<MmrChangedEvent>? handler = MmrChanged;
        handler?.Invoke(this, e);
    }

    public HubService(IToastService toastService, ILogger<HubService> logger)
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            //.WithAutomaticReconnect()
            .Build();

        hubConnection.On<ToonIdsRatingsResponse>("mmrchanged", mmrChange =>
        {
            OnMmrChanged(new() { Response = mmrChange });
        });
        this.toastService = toastService;
        this.logger = logger;
    }

    public async Task StartHubConnection()
    {
        if (!UserSettingsService.UserSettings.AllowUploads || !UserSettingsService.UserSettings.UseHubConnection)
        {
            return;
        }

        if (hubConnection.State != HubConnectionState.Disconnected)
        {
            return;
        }

        try
        {
            await hubConnection.StartAsync();
            await hubConnection.SendAsync("Subscribe", UserSettingsService.UserSettings.AppGuid);
        } catch (Exception ex)
        {
            toastService.ShowError($"Failed connection to server");
            logger.LogError($"failed opening hub connection: {ex.Message}");        
        }
    }

    public async Task CloseHubConnection()
    {
        if (hubConnection.State != HubConnectionState.Connected)
        {
            return;
        }
        await hubConnection.SendAsync("UnSubscribe", UserSettingsService.UserSettings.AppGuid);
        await hubConnection.StopAsync();
    }

    public async Task DEBUGMmrChange()
    {
        if (hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.SendAsync("DEBUGMmrChange", UserSettingsService.UserSettings.AppGuid);
        }
    }

    public void Dispose()
    {
        hubConnection.DisposeAsync();
    }
}

public class MmrChangedEvent : EventArgs
{
    public ToonIdsRatingsResponse Response { get; init; } = null!;
}