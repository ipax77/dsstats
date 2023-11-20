using dsstats.shared;
using dsstats.shared.Extensions;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;

namespace dsstats.razorlib.Stats.Winrate
{
    public partial class WinrateInComponent : StatsComponent, IDisposable
    {
        [Inject]
        public IWinrateService winrateService { get; set; } = default!;

        SemaphoreSlim ss = new(1, 1);
        CancellationTokenSource cts = new();

        WinrateResponse? response = null;
        WinrateChart? winrateChart;
        WinrateTable? winrateTable;
        CmdrSelectComponent? cmdrSelectComponent;
        WinrateType winrateType = WinrateType.AvgGain;

        TableOrder tableOrder = new()
        {
            Property = nameof(WinrateEnt.AvgGain),
            Ascending = false
        };
        List<TableOrder> tableOrders = new();

        WinrateResponse orderedResponse => response == null ? new() : new()
        {
            Interest = response.Interest,
            WinrateEnts = tableOrder.Property == "Winrate" ?
                                                    tableOrder.Ascending ? response.WinrateEnts.OrderBy(o => o.Wins * 100.0 / o.Count).ToList()
                                                    : response.WinrateEnts.OrderByDescending(o => o.Wins * 100.0 / o.Count).ToList()
                                                : tableOrder.Ascending ? response.WinrateEnts.AsQueryable().AppendOrderBy(tableOrder.Property).ToList()
                                                    : response.WinrateEnts.AsQueryable().AppendOrderByDescending(tableOrder.Property).ToList()
        };

        public override async Task LoadData(bool init = false)
        {
            await ss.WaitAsync();
            try
            {
                IsLoading = true;
                await InvokeAsync(() => StateHasChanged());
                response = await winrateService.GetWinrate(Request, cts.Token);

                winrateChart?.PrepareData(orderedResponse, new(Request, winrateType));
                winrateTable?.SetTable(orderedResponse);
                cmdrSelectComponent?.SetParameters(Request.RatingType == RatingType.Std || Request.RatingType == RatingType.StdTE, true, Request.Interest);
                if (!init)
                {
                    await OnRequestChanged.InvokeAsync(Request);
                }

                IsLoading = false;
                await InvokeAsync(() => StateHasChanged());
            }
            finally
            {
                ss.Release();
            }

            await base.LoadData(init);
        }

        private void CommanderSelected(Commander cmdr)
        {
            Request.Interest = cmdr;
            _ = LoadData();
        }

        private void SetWinrateType(ChangeEventArgs e, WinrateType winrateType)
        {
            this.winrateType = winrateType;

            tableOrder.Property = winrateType switch
            {
                WinrateType.AvgGain => nameof(WinrateEnt.AvgGain),
                WinrateType.Matchups => nameof(WinrateEnt.Count),
                WinrateType.AvgRating => nameof(WinrateEnt.AvgRating),
                WinrateType.Winrate => "Winrate",
                _ => nameof(WinrateEnt.AvgGain)
            };
            tableOrder.Ascending = false;

            winrateChart?.PrepareData(orderedResponse, new(Request, winrateType));
            winrateTable?.SetTable(orderedResponse);
            OnRequestChanged.InvokeAsync(Request);
        }

        private string GetDescription()
        {
            return (winrateType, Request.Interest == Commander.None) switch
            {
                (WinrateType.AvgGain, true) => $"This bar chart displays the average rating gain of players who used various commanders within a specified time period. Each bar represents a different commander, and the height of the bar indicates the average rating gain achieved by players who chose that commander during the designated time frame. This chart provides a comprehensive overview of how different commanders perform in terms of rating gain.",
                (WinrateType.Matchups, true) => $"The number of matchups (up to 6 per game)",
                (WinrateType.AvgRating, true) => $"The average player rating of players who played the Commanders",
                (WinrateType.Winrate, true) => $"Winrate - average wins per 100 matchups",
                (WinrateType.AvgGain, false) => $"The average rating gain of players who played {Request.Interest} versus the other Commanders. This allows you to directly assess the relative strength of your chosen commander against the rest, assisting you in making informed decisions when selecting your preferred commander for optimal performance.",
                (WinrateType.Matchups, false) => $"The number of matchups (up to 6 per game) of {Request.Interest} vs the Commanders",
                (WinrateType.AvgRating, false) => $"The average player rating of players who played {Request.Interest} vs the Commanders",
                (WinrateType.Winrate, false) => $"Winrate - average wins per 100 matchups of {Request.Interest} vs the Commanders",
                _ => ""
            };
        }

        private void OrderChanged()
        {
            winrateTable?.SetTable(orderedResponse);
            winrateChart?.ChangeOrder(orderedResponse);
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
            ss.Dispose();
        }
    }
}