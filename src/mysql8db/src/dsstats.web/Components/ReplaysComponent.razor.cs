using dsstats.shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace dsstats.web.Components;

public partial class ReplaysComponent : ComponentBase
{
    ReplaysRequest replaysRequest = new();
    int totalReplays = 0;

    protected override void OnInitialized()
    {
        _ = SetCount(replaysRequest);
        base.OnInitialized();
    }

    private async Task SetCount(ReplaysRequest request)
    {
        totalReplays = await replaysService.GetReplaysCount(request, default);
        await InvokeAsync(StateHasChanged);
    }

    private async ValueTask<ItemsProviderResult<ReplayListDto>> LoadReplays(
    ItemsProviderRequest request)
    {
        var numReplays = Math.Min(request.Count, totalReplays - request.StartIndex);
        replaysRequest.Skip = request.StartIndex;
        replaysRequest.Take = numReplays;
        var replays = await replaysService.GetReplays(replaysRequest, request.CancellationToken);

        return new ItemsProviderResult<ReplayListDto>(replays, totalReplays);
    }

    private void Refresh()
    {
        StateHasChanged();
    }
}