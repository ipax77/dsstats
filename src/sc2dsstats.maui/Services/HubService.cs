using Microsoft.AspNetCore.SignalR.Client;

namespace sc2dsstats.maui.Services;

internal class HubService : IDisposable
{
    private static readonly Uri hubUrl = new Uri("https://localhost:7174/hubs/maui");

    private HubConnection hubConnection = null!;

    public double CurrentMmr { get; private set; } = 1000.0;

    public event EventHandler<EventArgs>? MmrChanged;
    protected virtual void OnMmrChanged(EventArgs e)
    {
        EventHandler<EventArgs>? handler = MmrChanged;
        handler?.Invoke(this, e);
    }

    public HubService()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        hubConnection.On<double>("CurrentMmr", mmr =>
        {
            CurrentMmr = mmr;
            OnMmrChanged(EventArgs.Empty);
        }); 
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
        await hubConnection.StartAsync();
        await hubConnection.SendAsync("Subscribe", UserSettingsService.UserSettings.AppGuid);
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
