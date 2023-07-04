using Microsoft.AspNetCore.Components;
using pax.BlazorChartJs;
using pax.dsstats.shared;
using static sc2dsstats.razorlib.Stats.StatsChartComponent;

namespace sc2dsstats.razorlib.Stats.Damage;

public partial class DamageChart : ComponentBase
{
    [Parameter, EditorRequired]
    public DamageRequest Request { get; set; } = default!;

    [Parameter, EditorRequired]
    public DamageResponse Response { get; set;} = default!;

    private readonly string mainColor = "#3F5FFA";
    ChartJsConfig chartConfig = null!;
    bool chartReady;

    protected override void OnInitialized()
    {
        chartConfig = GetChartConfig();
        base.OnInitialized();
    }

    private void ChartEventTriggered(ChartJsEvent chartJsEvent)
    {
        if (chartJsEvent is ChartJsInitEvent initEvent)
        {
            chartReady = true;
            SetupChart(Request, Response);
        }
    }

    public void SetupChart(DamageRequest request, DamageResponse response)
    {
        Request = request;
        Response = response;
        if (!chartReady) 
        {
            return;
        }

        if (chartConfig.Data.Datasets.Any())
        {
            chartConfig.RemoveDatasets(chartConfig.Data.Datasets);
        }

        if (chartConfig.Options?.Plugins?.Title != null)
        {
            chartConfig.Options.Plugins.Title.Text = GetTitle(request);
            chartConfig.UpdateChartOptions();
        }

        chartConfig.SetLabels(GetLabels(request, response));

        List<ChartJsDataset> datasets = new();
        datasets.Add(GetArmyDataset(request, response));
        datasets.Add(GetUpgradesDataset(request, response));
        chartConfig.AddDatasets(datasets);
    }

    private IndexableOption<string> GetTitle(DamageRequest request)
    {
        if (request.FromRating == 0 && request.ToRating == 0 && request.Exp2WinOffset == 0)
        {
            return new IndexableOption<string>($"Army - {Data.GetRatingTypeLongName(request.RatingType)} - {Data.GetTimePeriodLongName(request.TimePeriod)}");
        }
        else
        {
            List<string> titles = new()
            {
                $"Army - {Data.GetRatingTypeLongName(request.RatingType)} - {Data.GetTimePeriodLongName(request.TimePeriod)}"
            };
            if (request.FromRating != 0 || request.ToRating != 0)
            {
                titles.Add($"Rating range {request.FromRating} - {request.ToRating}");
            }
            if (request.Exp2WinOffset != 0)
            {
                titles.Add($"Expectation to win range {(50 - request.Exp2WinOffset)}% - {(50 + request.Exp2WinOffset)}% ");
            }
            return new IndexableOption<string>(titles);
        }
    }

    private List<string> GetLabels(DamageRequest request, DamageResponse response)
    {
        return response.Entities
                .Where(x => x.Breakpoint == Request.Breakpoint)
                .OrderBy(o => o.AvgArmy + o.AvgUpgrades)
                .Select(s => s.Commander.ToString())
                .ToList();
    }

    private ChartJsDataset GetArmyDataset(DamageRequest request, DamageResponse response)
    {
        var data = response.Entities
                .Where(x => x.Breakpoint == Request.Breakpoint)
                .OrderBy(o => o.AvgArmy + o.AvgUpgrades)
                .ToList();

        return new BarDataset()
        {
            Label = "Army",
            Data = data
                .Select(s => s.AvgArmy)
                .Cast<object>()
                .ToList(),
            BackgroundColor = new IndexableOption<string>(data.Select(s => Data.GetBackgroundColor(s.Commander, "66")).ToList()),
            BorderColor = new IndexableOption<string>(data.Select(s => Data.GetBackgroundColor(s.Commander)).ToList()),
            BorderWidth = new IndexableOption<double>(2)
        };
    }

    private ChartJsDataset GetUpgradesDataset(DamageRequest request, DamageResponse response)
    {
        var data = response.Entities
        .Where(x => x.Breakpoint == Request.Breakpoint)
        .OrderBy(o => o.AvgArmy + o.AvgUpgrades)
        .ToList();

        return new BarDataset()
        {
            Label = "Upgrades",
            Data = data
                .Select(s => s.AvgUpgrades)
                .Cast<object>()
                .ToList(),
            BackgroundColor = new IndexableOption<string>(data.Select(s => Data.GetBackgroundColor(s.Commander, "33")).ToList()),
            BorderColor = new IndexableOption<string>(data.Select(s => Data.GetBackgroundColor(s.Commander)).ToList()),
            BorderWidth = new IndexableOption<double>(2)
        };
    }

    private ChartJsConfig GetChartConfig()
    {
        return new()
        {
            Type = ChartType.bar,
            Options = new IconsChartJsOptions()
            {
                MaintainAspectRatio = true,
                Responsive = true,
                IndexAxis = "y",
                Plugins = new IconsPlugins()
                {
                    Title = new()
                    {
                        Display = true,
                        Position = "top",
                        Text = new IndexableOption<string>("Army Value"),
                        Color = "white",
                        Font = new()
                        {
                            Size = 16,
                        }
                    },
                    Legend = new Legend()
                    {
                        Display = false,
                        Labels = new Labels()
                        {
                            Padding = 0,
                            BoxHeight = 0,
                            BoxWidth = 0
                        }
                    }
                },
                Scales = new()
                {
                    X = new LinearAxis()
                    {
                        Stacked = true,
                        Ticks = new ChartJsAxisTick()
                        {
                            Color = mainColor
                        },
                        Grid = new ChartJsGrid()
                        {
                            Display = true,
                            Color = "rgba(113, 116, 143, 0.25)",
                            TickColor = "rgba(113, 116, 143, 0.75)",
                            Z = -1
                        },
                        Border = new ChartJsAxisBorder()
                        {
                            Display = true,
                            Color = "rgba(113, 116, 143)",
                            Dash = new List<double>() { 2, 4 }
                        }
                    },
                    Y = new LinearAxis()
                    {
                        BeginAtZero = true,
                        Stacked = true,
                        Title = new Title()
                        {
                            Display = false,
                            Text = new IndexableOption<string>("Commander"),
                            Color = mainColor
                        },
                        Ticks = new ChartJsAxisTick()
                        {
                            Color = mainColor
                        },
                        Grid = new ChartJsGrid()
                        {
                            Display = true,
                            Color = "rgba(113, 116, 143, 0.25)",
                            TickColor = "rgba(113, 116, 143, 0.75)",
                            Z = -1
                        },
                        Border = new ChartJsAxisBorder()
                        {
                            Display = true,
                            Color = "rgba(113, 116, 143)",
                            Dash = new List<double>() { 2, 4 }
                        }
                    }
                }
            }
        };
    }
}
