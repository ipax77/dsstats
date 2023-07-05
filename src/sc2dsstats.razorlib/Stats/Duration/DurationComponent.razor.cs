
using Microsoft.AspNetCore.Components;
using pax.dsstats.shared.Interfaces;
using pax.dsstats.shared;
using Microsoft.JSInterop;

namespace sc2dsstats.razorlib.Stats.Duration;

public partial class DurationComponent : ComponentBase, IDisposable
{
    private DurationResponse? Response = null;
    private bool isLoading;
    private CancellationTokenSource cts = new();
    private DurationChart? durationChart;
    private DurationTable? durationTable;

    [Inject]
    protected IDurationService durationService { get; set; } = null!;

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = null!;

    [Parameter, EditorRequired]
    public DurationRequest Request { get; set; } = default!;

    [Parameter]
    public EventCallback<DurationRequest> OnRequetChanged { get; set; }

    protected override void OnInitialized()
    {
        _ = LoadData();
        base.OnInitialized();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            JSRuntime.InvokeVoidAsync("enableTooltips");
        }
        base.OnAfterRender(firstRender);
    }

    public async Task LoadData()
    {
        isLoading = true;
        await InvokeAsync(() => StateHasChanged());
        Response = await durationService.GetDuration(Request, cts.Token);
        isLoading = false;
        await InvokeAsync(() => StateHasChanged());
        await OnRequetChanged.InvokeAsync(Request);
        durationChart?.SetupChart(Response, Request.RatingType);
        durationTable?.PrepareData(Response, Request);
    }

    private void RatingTypeChanged(RatingType ratingType)
    {
        if (Response == null)
        {
            return;
        }
        durationChart?.SetupChart(Response, ratingType);
        durationTable?.PrepareData(Response, Request);
    }

    private void ClearChart()
    {
        durationChart?.ClearDatasets();
        durationTable?.ClearChart();
    }

    private void ShowChart()
    {
        if (Response == null)
        {
            return;
        }
        durationChart?.SetupChart(Response, Request.RatingType);
        durationTable?.SetChart();
    }

    private void ChartRequest(KeyValuePair<Commander, bool> request)
    {
        if (request.Value)
        {
            durationChart?.AddDataset(request.Key);
        }
        else
        {
            durationChart?.RemoveDataset(request.Key);
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}