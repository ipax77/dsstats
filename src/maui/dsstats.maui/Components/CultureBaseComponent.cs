using dsstats.localization;
using dsstats.maui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace dsstats.maui.Components;

public partial class CultureBaseComponent : ComponentBase, IDisposable
{
    [Inject]
    protected DsstatsService DsstatsService { get; set; } = null!;

    [Inject]
    protected IStringLocalizer<DsstatsLoc> Loc { get; set; } = null!;

    protected override void OnInitialized()
    {
        DsstatsService.CultureChanged += DsstatsService_CultureChanged;
    }

    private void DsstatsService_CultureChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            DsstatsService.CultureChanged -= DsstatsService_CultureChanged;
        }
    }

    void IDisposable.Dispose()
    {
        Dispose(disposing: true);
    }
}
