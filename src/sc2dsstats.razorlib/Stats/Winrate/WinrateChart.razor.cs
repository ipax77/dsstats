using Microsoft.AspNetCore.Components;
using pax.dsstats.shared;
using pax.BlazorChartJs;

namespace sc2dsstats.razorlib.Stats.Winrate;

public partial class WinrateChart : ComponentBase
{
    [Parameter, EditorRequired]
    public WinrateResponse Response { get; set; } = default!;

    [Parameter, EditorRequired]
    public WinrateRequest Request { get; set; } = default!;

    ChartJsConfig chartConfig = null!;
    bool chartReady;

    ChartComponent? chartComponent;

    protected override void OnInitialized()
    {
        chartConfig = GetChartConfig();
        base.OnInitialized();
    }

    private void ChartEventTriggered(ChartJsEvent chartEvent)
    {
        if (chartEvent is ChartJsInitEvent initEvent)
        {
            chartReady = true;
            PrepareData(Response, Request);
        }
    }

    public void PrepareData(WinrateResponse response, WinrateRequest request)
    {
        if (!chartReady)
        {
            return;
        }

        Response = response;
        Request = request;

        if (chartConfig.Data.Datasets.Any())
        {
            chartConfig.RemoveDatasets(chartConfig.Data.Datasets);
        }

        chartConfig.SetLabels(response.WinrateEnts.Select(s => s.Commander.ToString()).ToList());

        chartConfig.AddDataset(GetAvgGainDataset(response));
        chartConfig.AddDataset(GetWinrateDataset(response));
    }

    private ChartJsDataset GetAvgGainDataset(WinrateResponse response)
    {
        var data = response.WinrateEnts.Select(s => s.AvgGain).Cast<object>().ToList();

        var barDataset = new BarDataset()
        {
            Label = $"{response.Interest.ToString()} AvgGain",
            Data = data,
            BackgroundColor = response.Interest == Commander.None ?
                new IndexableOption<string>(response.WinrateEnts.Select(s => Data.GetBackgroundColor(s.Commander)).ToList())
                : new IndexableOption<string>(Data.GetBackgroundColor(response.Interest)),
            BorderColor = response.Interest == Commander.None ?
                new IndexableOption<string>(response.WinrateEnts.Select(s => Data.CmdrColor[s.Commander]).ToList())
                : new IndexableOption<string>(Data.CmdrColor[response.Interest]),
            BorderWidth = new IndexableOption<double>(2),
            Stack = "Stack 0"
        };

        return barDataset;
    }

    private ChartJsDataset GetWinrateDataset(WinrateResponse response)
    {
        var data = response.WinrateEnts.Select(s => s.Count == 0 ? 0 : Math.Round((double)s.Wins / s.Count, 2)).Cast<object>().ToList();

        var barDataset = new BarDataset()
        {
            Label = $"{response.Interest.ToString()} Winrate",
            Data = data,
            BackgroundColor = response.Interest == Commander.None ?
                new IndexableOption<string>(response.WinrateEnts.Select(s => Data.GetBackgroundColor(s.Commander)).ToList())
                : new IndexableOption<string>(Data.GetBackgroundColor(response.Interest)),
            BorderColor = response.Interest == Commander.None ?
                new IndexableOption<string>(response.WinrateEnts.Select(s => Data.CmdrColor[s.Commander]).ToList())
                : new IndexableOption<string>(Data.CmdrColor[response.Interest]),
            BorderWidth = new IndexableOption<double>(2),
            Stack = "Stack 1"
        };

        return barDataset;
    }


    private ChartJsConfig GetChartConfig()
    {
        return new()
        {
            Type = ChartType.bar,
            Options = new ChartJsOptions()
            {
                MaintainAspectRatio = true,
                Responsive = true,
                OnClickEvent = true,
                Plugins = new Plugins()
                {
                    Title = new()
                    {
                        Display = true,
                        Text = new IndexableOption<string>("Winrate"),
                        Color = "white",
                        Font = new()
                        {
                            Size = 16,
                        }
                    },
                    Datalabels = new()
                    {
                        Display = "auto",
                        Color = "#0a050c",
                        BackgroundColor = "#cdc7ce",
                        BorderColor = "#491756",
                        BorderRadius = 4,
                        BorderWidth = 1,
                        Anchor = "end",
                        Align = "start",
                        Clip = true
                    }
                },
                Scales = new()
                {
                    X = new LinearAxis()
                    {
                        Stacked = true,
                    },
                    Y = new LinearAxis()
                    {
                      Stacked = true  
                    }
                }
            }
        };
    }
}
