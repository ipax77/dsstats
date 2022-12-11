
using Microsoft.AspNetCore.Components;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace sc2dsstats.razorlib.PlayerDetails;

public partial class PlayerDetailsNgComponent : ComponentBase, IDisposable
{
    [Parameter, EditorRequired]
    public RequestNames RequestNames { get; set; } = default!;
    [Parameter, EditorRequired]
    public RatingType RatingType { get; set; }
    [Parameter]
    public EventCallback OnCloseRequested { get; set; }

    [Inject]
    protected IDataService dataService { get; set; } = default!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    private CancellationTokenSource cts = new();
    private PlayerDetailsResult? playerDetailsResult = null;
    private PlayerDetailsGroupResult? playerGroupResult = null;

    private bool groupDataLoading = false;

    protected override void OnInitialized()
    {
        _ = LoadData();
        base.OnInitialized();
    }

    //protected override void OnAfterRender(bool firstRender)
    //{
    //    if (firstRender)
    //    {
    //        _ = LoadGroupData();
    //    }
    //    base.OnAfterRender(firstRender);
    //}

    private async Task LoadData()
    {
        playerDetailsResult = await dataService.GetPlayerDetailsNg(RequestNames.ToonId, (int)RatingType, cts.Token);
        await InvokeAsync(() => StateHasChanged());
    }

    private async Task LoadGroupData()
    {
        if (groupDataLoading)
        {
            return;
        }
        groupDataLoading = true;
        await InvokeAsync(() => StateHasChanged());
        playerGroupResult = await dataService.GetPlayerGroupDetails(RequestNames.ToonId, (int)RatingType, cts.Token);
        groupDataLoading = false;
        await InvokeAsync(() => StateHasChanged());
    }

    private void ShowReplays()
    {
        NavigationManager.NavigateTo(
            NavigationManager.GetUriWithQueryParameters("replays",
                new Dictionary<string, object?>() { { "Players", RequestNames.Name } }
            )
        );
    }

    private void FromServerChange(ChangeEventArgs e)
    {
        if (e.Value is bool value)
        {
            dataService.SetFromServer(value);
            _ = LoadData();
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}
