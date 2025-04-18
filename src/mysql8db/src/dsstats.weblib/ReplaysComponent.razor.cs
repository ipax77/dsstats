using dsstats.shared;
using dsstats.shared8;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace dsstats.weblib;

public partial class ReplaysComponent : ComponentBase
{
    ReplaysRequest replaysRequest = new();
    PageInfo pageInfo = new();
    bool isLoading = false;

    Virtualize<ReplayListDto>? virtualize;

    protected override void OnInitialized()
    {
        _ = SetCount(replaysRequest);
        base.OnInitialized();
    }

    private async Task SetCount(ReplaysRequest request)
    {
        pageInfo.TotalSize = await replaysService.GetReplaysCount(request, default);
        pageInfo.Page = 0;
        pageInfo.PageRequest = 0;
        await InvokeAsync(StateHasChanged);
    }

    private async ValueTask<ItemsProviderResult<ReplayListDto>> LoadReplays(
    ItemsProviderRequest request)
    {
        var replaysOffset = pageInfo.PageSize * pageInfo.Page;
        int globalSkip = replaysOffset + request.StartIndex;
        var numReplays = Math.Min(request.Count, pageInfo.TotalSize - pageInfo.PageSize * pageInfo.Page);
        replaysRequest.Skip = globalSkip;
        replaysRequest.Take = numReplays;
        var replays = await replaysService.GetReplays(replaysRequest, request.CancellationToken);

        return new ItemsProviderResult<ReplayListDto>(replays, Math.Min(pageInfo.TotalSize, pageInfo.PageSize));
    }

    private async Task OnPageChanged(int page)
    {
        if (virtualize is null || isLoading)
        {
            return;
        }
        isLoading = true;
        pageInfo.Page = page;
        pageInfo.PageRequest = page;
        await virtualize.RefreshDataAsync();
        isLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task PageChanged()
    {
        await OnPageChanged(pageInfo.PageRequest);
    }

    private async Task Reload()
    {
        if (virtualize is null)
        {
            return;
        }
        isLoading = true;
        await SetCount(replaysRequest);
        await virtualize.RefreshDataAsync();
        isLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private void Refresh()
    {
        StateHasChanged();
    }

    private async Task SortList(Microsoft.AspNetCore.Components.Web.MouseEventArgs e, string property)
    {
        var exOrder = replaysRequest.Orders.FirstOrDefault(f => f.Property == property);
        if (e.ShiftKey)
        {
            if (exOrder == null)
            {
                replaysRequest.Orders.Add(new TableOrder()
                {
                    Property = property
                });
            }
            else
            {
                exOrder.Ascending = !exOrder.Ascending;
            }
        }
        else
        {
            replaysRequest.Orders.Clear();
            replaysRequest.Orders.Add(new TableOrder()
            {
                Property = property,
                Ascending = exOrder == null ? false : !exOrder.Ascending
            });
        }
        await Reload();
    }
}