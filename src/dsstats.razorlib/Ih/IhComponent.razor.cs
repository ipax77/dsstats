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

    private bool decoding;

    protected override async Task OnInitializedAsync()
    {
        groupState.GroupId = guid;
        groupState.Visitors = 1;

        // DEBUG
        //groupState.PlayerStates = [
        //    new() {
        //        PlayerId = new(1, 1, 2),
        //        Name = "Test1",
        //        RatingStart = 1000
        //    },
        //    new() {
        //        PlayerId = new(2, 1, 2),
        //        Name = "Test2",
        //        RatingStart = 1000
        //    },
        //    new() {
        //        PlayerId = new(3, 1, 2),
        //        Name = "Test3",
        //        RatingStart = 1000
        //    },
        //    new() {
        //        PlayerId = new(4, 1, 2),
        //        Name = "Test4",
        //        RatingStart = 1000
        //    },
        //    new() {
        //        PlayerId = new(5, 1, 2),
        //        Name = "Test5",
        //        RatingStart = 1000
        //    },
        //    new() {
        //        PlayerId = new(6, 1, 2),
        //        Name = "Test6",
        //        RatingStart = 1000
        //    },
        //    new() {
        //        PlayerId = new(7, 1, 2),
        //        Name = "Test7",
        //        RatingStart = 1000
        //    },
        //    new() {
        //        PlayerId = new(8, 1, 2),
        //        Name = "Test8",
        //        RatingStart = 1000
        //    },
        //    new() {
        //        PlayerId = new(9, 1, 2),
        //        Name = "Test9",
        //        RatingStart = 1000
        //    },
        //    new() {
        //        PlayerId = new(10, 1, 2),
        //        Name = "Test10",
        //        RatingStart = 1000
        //    },
        //];
        //groupState.PlayerStates.ForEach(f => f.InQueue = true);

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

        hubConnection.On<GroupState>("NewState", (newgroupState) =>
        {
            groupState = newgroupState;
            decoding = false;
            InvokeAsync(() => StateHasChanged());
        });

        hubConnection.On("DecodingStart", () =>
        {
            decoding = true;
            InvokeAsync(() => StateHasChanged());
        });

        hubConnection.On("DecodeError", () =>
        {
            decoding = false;
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