
using Microsoft.AspNetCore.Components;
using pax.dsstats.shared.Interfaces;
using pax.dsstats.shared;

namespace sc2dsstats.razorlib.Stats.Duration;

public partial class DurationComponent : ComponentBase, IDisposable
{
    private DurationResponse? Response = null;
    private bool isLoading;
    private CancellationTokenSource cts = new();
    private DurationChart? durationChart;

    [Inject]
    protected IDurationService durationService { get; set; } = null!;

    [Parameter, EditorRequired]
    public DurationRequest Request { get; set; } = default!;

    [Parameter]
    public EventCallback<DurationRequest> OnRequetChanged { get; set; }

    protected override void OnInitialized()
    {
        _ = LaodData();
        base.OnInitialized();
    }

    public async Task LaodData()
    {
        isLoading = true;
        await InvokeAsync(() => StateHasChanged());
        Response = await durationService.GetDuration(Request, cts.Token);
        isLoading = false;
        await InvokeAsync(() => StateHasChanged());
        await OnRequetChanged.InvokeAsync(Request);
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}