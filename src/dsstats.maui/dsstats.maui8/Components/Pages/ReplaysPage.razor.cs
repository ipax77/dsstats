using dsstats.razorlib.Replays;
using dsstats.shared;
using Microsoft.AspNetCore.Components;

namespace dsstats.maui8.Components.Pages;

public partial class ReplaysPage
{
    [Parameter, SupplyParameterFromQuery]
    public string? Replay { get; set; }

    [Parameter, SupplyParameterFromQuery]
    public string? PlayerId { get; set; }
    [Parameter, SupplyParameterFromQuery]
    public string? Vs { get; set; }
    [Parameter, SupplyParameterFromQuery]
    public string? With { get; set; }

    ReplaysRequest Request = new()
    {
        MauiInfo = true,
        Orders = new() { new() { Property = "GameTime", Ascending = false } }
    };

    ReplaysComponent? replaysComponent;

    protected override void OnInitialized()
    {
        SetRequest();
        base.OnInitialized();
    }

    private void SetRequest()
    {
        if (!string.IsNullOrEmpty(Replay))
        {
            Request.ReplayHash = Replay;
        }

        if (!string.IsNullOrEmpty(PlayerId))
        {
            Request.PlayerId = Data.GetPlayerId(PlayerId);
        }

        if (!string.IsNullOrEmpty(Vs))
        {
            Request.PlayerIdVs = Data.GetPlayerId(Vs);
        }

        if (!string.IsNullOrEmpty(With))
        {
            Request.PlayerIdWith = Data.GetPlayerId(With);
        }
    }

    private void SetParameters(ReplaysRequest request)
    {
        Dictionary<string, object?> queryDic = new();

        if (!string.IsNullOrEmpty(request.ReplayHash))
        {
            queryDic.Add("replay", request.ReplayHash);
        }
        else
        {
            queryDic.Add("replay", null);
        }

        queryDic.Add("playerid", Data.GetPlayerIdString(Request.PlayerId));
        queryDic.Add("vs", Data.GetPlayerIdString(Request.PlayerIdVs));
        queryDic.Add("with", Data.GetPlayerIdString(Request.PlayerIdWith));

        NavigationManager.NavigateTo(
        NavigationManager.GetUriWithQueryParameters(
        new Dictionary<string, object?>(queryDic)
        )
        );
    }

    private void ShowReplays(RequestNames name)
    {
        Dictionary<string, object?> queryDic = new();
        queryDic.Add("PlayerId", $"{name.ToonId}|{name.RealmId}|{name.RegionId}");

        Request.PlayerId = new(name.ToonId, name.RealmId, name.RegionId);
        Request.Players = name.Name;
        replaysComponent?.Update(Request);
    }
}