using Blazored.Toast.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace sc2dsstats.maui.Services;

internal class HubService : IDisposable
{
    private readonly IToastService toastService;
    private readonly ILogger<HubService> logger;
    private static readonly Uri hubUrl = new Uri("https://localhost:7174/hubs/maui");
    private HubConnection hubConnection = null!;

    public double CurrentMmr { get; private set; } = 1000.0;

    public event EventHandler<EventArgs>? MmrChanged;
    protected virtual void OnMmrChanged(EventArgs e)
    {
        EventHandler<EventArgs>? handler = MmrChanged;
        handler?.Invoke(this, e);
    }

    public HubService(IToastService toastService, ILogger<HubService> logger)
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        hubConnection.On<double>("CurrentMmr", mmr =>
        {
            CurrentMmr = mmr;
            OnMmrChanged(EventArgs.Empty);
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
        await hubConnection.StopAsync();
    }

    public void Dispose()
    {
        hubConnection.DisposeAsync();
    }
}
