using dsstats.razorlib.Builds;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace dsstats.razorlib.Ih;

public partial class IhComponent : ComponentBase, IDisposable
{
    [Inject]
    public HttpClient httpClient { get; set; } = default!;

    [Inject]
    public IReplaysService replaysService { get; set; } = default!;
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter, EditorRequired]
    public Guid Guid { get; set; } = Guid.NewGuid();


    private HubConnection? hubConnection;
    private bool isConnected => hubConnection?.State == HubConnectionState.Connected;
    GroupState groupState = new();

    private bool decoding;

    IhMatchComponent? ihMatchComponent;
    AddPlayersModal? addPlayersModal;

    List<ReplayListDto> replays = [];
    ReplayDto? interestReplay = null;

    protected override async Task OnInitializedAsync()
    {
        groupState.GroupId = Guid;
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

        hubConnection.On<GroupState>("ConnectInfo", (newgroupState) =>
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

        hubConnection.On<PlayerState>("NewPlayer", (player) =>
        {
            groupState.PlayerStates.Add(player);
            //ihMatchComponent?.Update();
            InvokeAsync(() => StateHasChanged());
        });

        hubConnection.On<PlayerState>("RemovePlayer", (player) =>
        {
            var playerState = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == player.PlayerId);
            if (playerState != null)
            {
                groupState.PlayerStates.Remove(playerState);
                //ihMatchComponent?.Update();
                InvokeAsync(() => StateHasChanged());
            }
        });

        hubConnection.On<PlayerId>("AddedToQueue", (player) =>
        {
            var playerState = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == player);
            if (playerState != null)
            {
                playerState.InQueue = true;
                //ihMatchComponent?.Update();
                InvokeAsync(() => StateHasChanged());
            }
        });

        hubConnection.On<PlayerId>("RemovedFromQueue", (player) =>
        {
            var playerState = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == player);
            if (playerState != null)
            {
                playerState.InQueue = false;
                //ihMatchComponent?.Update();
                InvokeAsync(() => StateHasChanged());
            }
        });

        hubConnection.On<List<ReplayListDto>>("Replays", (replaylist) =>
        {
            replays = replaylist;
            InvokeAsync(() => StateHasChanged());
        });

        await hubConnection.StartAsync();
        if (isConnected)
        {
            await hubConnection.SendAsync("JoinGroup", Guid.ToString());
        }

        await base.OnInitializedAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            JSRuntime.InvokeVoidAsync("enableTooltips");
        }
        base.OnAfterRender(firstRender);
    }

    public void DecodeRequested()
    {
        if (isConnected)
        {
            hubConnection?.SendAsync("DecodeRequest");
            decoding = true;
        }
    }

    public async void PlayersSelected(List<RequestNames> requestNames)
    {
        if (isConnected && hubConnection is not null)
        {
            foreach (var requestName in requestNames)
            {
                await hubConnection.SendAsync("AddPlayerToGroup", requestName);
            }
        }
    }

    public async Task ChangeQueueState(PlayerState state)
    {
        if (isConnected && hubConnection is not null)
        {
            if (state.InQueue)
            {
                await hubConnection.SendAsync("RemovePlayerFromQueue", state.PlayerId);
            }
            else
            {
                await hubConnection.SendAsync("AddPlayerToQueue", state.PlayerId);
            }
        }
    }

    public async Task RemovePlayer(PlayerState playerState)
    {
        if (isConnected && hubConnection is not null)
        {
            await hubConnection.SendAsync("RemovePlayerFromGroup", playerState.PlayerId);
        }
    }

    private async Task LoadReplay(string replayHash)
    {
        interestReplay = await replaysService.GetReplay(replayHash);
        await InvokeAsync(() => StateHasChanged());
    }

    private async Task CalculatePerformance()
    {
        if (isConnected && hubConnection is not null)
        {
            await hubConnection.SendAsync("CalculatePerformance", groupState.GroupId);
        }
    }

    public void Dispose()
    {
        hubConnection?.DisposeAsync();
    }
}