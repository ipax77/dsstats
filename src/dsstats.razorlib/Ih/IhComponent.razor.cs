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
    public GroupStateV2 GroupState { get; set; } = default!;


    private HubConnection? hubConnection;
    private bool isConnected => hubConnection?.State == HubConnectionState.Connected;
    

    private bool decoding;

    IhMatchComponent? ihMatchComponent;
    AddPlayersModal? addPlayersModal;

    List<ReplayListDto> replays = [];
    ReplayDto? interestReplay = null;

    protected override async Task OnInitializedAsync()
    {
        var uri = httpClient.BaseAddress ?? new Uri("https://dsstats.pax77.org");
        uri = new Uri(uri, "/hubs/ih");

        hubConnection = new HubConnectionBuilder()
            .WithUrl(uri)
            .Build();

        hubConnection.On<int>("VisitorJoined", (count) =>
        {
            GroupState.Visitors = count;
            InvokeAsync(() => StateHasChanged());
        });

        hubConnection.On<int>("VisitorLeft", (count) =>
        {
            GroupState.Visitors = count;
            InvokeAsync(() => StateHasChanged());
        });

        hubConnection.On<List<string>>("NewReplays", (replayHashes) =>
        {
            GroupState.ReplayHashes.UnionWith(replayHashes);
            InvokeAsync(() => StateHasChanged());
        });

        hubConnection.On<GroupStateV2>("ConnectInfo", (newgroupState) =>
        {
            GroupState = newgroupState;
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

        hubConnection.On<PlayerStateV2>("NewPlayer", (player) =>
        {
            GroupState.PlayerStates.Add(player);
            //ihMatchComponent?.Update();
            InvokeAsync(() => StateHasChanged());
        });

        hubConnection.On<PlayerState>("RemovePlayer", (player) =>
        {
            var playerState = GroupState.PlayerStates.FirstOrDefault(f => f.PlayerId == player.PlayerId);
            if (playerState != null)
            {
                GroupState.PlayerStates.Remove(playerState);
                //ihMatchComponent?.Update();
                InvokeAsync(() => StateHasChanged());
            }
        });

        hubConnection.On<PlayerId>("AddedToQueue", (player) =>
        {
            var playerState = GroupState.PlayerStates.FirstOrDefault(f => f.PlayerId == player);
            if (playerState != null)
            {
                playerState.InQueue = true;
                //ihMatchComponent?.Update();
                InvokeAsync(() => StateHasChanged());
            }
        });

        hubConnection.On<PlayerId>("RemovedFromQueue", (player) =>
        {
            var playerState = GroupState.PlayerStates.FirstOrDefault(f => f.PlayerId == player);
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
            await hubConnection.SendAsync("JoinGroup", GroupState.GroupId);
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

    public async Task ChangeQueueState(PlayerStateV2 playerState)
    {
        if (isConnected && hubConnection is not null)
        {
            if (playerState.InQueue)
            {
                await hubConnection.SendAsync("RemovePlayerFromQueue", playerState.PlayerId);
            }
            else
            {
                await hubConnection.SendAsync("AddPlayerToQueue", playerState.PlayerId);
            }
        }
    }

    public async Task RemovePlayer(PlayerStateV2 playerState)
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
            await hubConnection.SendAsync("CalculatePerformance", GroupState.GroupId);
        }
    }

    private static string GetPlayerColor(PlayerStateV2 playerState)
    {
        if (playerState.Quit)
        {
            return "table-danger";
        }
        else if (playerState.PlayedLastGame)
        {
            return "table-success";
        }
        else if (playerState.ObsLastGame)
        {
            return "table-warning";
        }
        else if (playerState.NewPlayer)
        {
            return "table-light";
        }
        else
        {
            return "";
        }
    }

    public void Dispose()
    {
        hubConnection?.DisposeAsync();
    }
}