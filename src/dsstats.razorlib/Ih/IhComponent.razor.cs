using dsstats.shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace dsstats.razorlib.Ih;

public partial class IhComponent() : ComponentBase, IDisposable
{
    [Inject]
    public HttpClient httpClient { get; set; } = default!;

    private Guid guid = Guid.NewGuid();
    private HubConnection? hubConnection;
    private bool isConnected => hubConnection?.State == HubConnectionState.Connected;
    GroupState groupState = new();

    protected override async Task OnInitializedAsync()
    {
        groupState.GroupId = guid;
        groupState.Visitors = 1;
        var uri = httpClient.BaseAddress ?? new Uri("https://dsstats.pax77.org");
        uri = new Uri(uri, "/hubs/ih");

        hubConnection = new HubConnectionBuilder()
            .WithUrl(uri)
            .Build();

        hubConnection.On<int>("VisitorJoined", (count) =>
        {
            groupState.Visitors = count;
            InvokeAsync(() => StateHasChanged());
        });

        hubConnection.On<int>("VisitorLeft", (count) =>
        {
            groupState.Visitors = count;
            InvokeAsync(() => StateHasChanged());
        });

        hubConnection.On<List<string>>("NewReplays", (replayHashes) =>
        {
            groupState.ReplayHashes.UnionWith(replayHashes);
            InvokeAsync(() => StateHasChanged());
        });

        await hubConnection.StartAsync();
        if (isConnected)
        {
            await hubConnection.SendAsync("JoinGroup", guid.ToString());
        }

        await base.OnInitializedAsync();
    }

    public void DecodeRequested()
    {
        if (isConnected)
        {
            hubConnection?.SendAsync("DecodeRequest");
        }
    }

    public void Dispose()
    {
        hubConnection?.DisposeAsync();
    }
}