using Microsoft.AspNetCore.Components;
using pax.BlazorChartJs;
using pax.dsstats.shared;
using sc2dsstats.razorlib.Extensions;

namespace sc2dsstats.razorlib.Stats.Damage;

public partial class DamageChart : ComponentBase
{
    [Parameter, EditorRequired]
    public DamageRequest Request { get; set; } = default!;

    [Parameter, EditorRequired]
    public DamageResponse Response { get; set;} = default!;

    [Parameter, EditorRequired]
    public List<TableOrder> TableOrders { get; set; } = default!;

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
            SetupChart(Request, Response, TableOrders);
        }
    }

    public void SetupChart(DamageRequest request, DamageResponse response, List<TableOrder> tableOrders)
    {
        Request = request;
        Response = response;
        TableOrders = tableOrders;

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

        if (request.ChartType == DamageChartType.Army)
        {
            datasets.Add(GetArmyDataset(request, response));
            datasets.Add(GetUpgradesDataset(request, response));
        }
        else if (request.ChartType == DamageChartType.Damage)
        {
            datasets.Add(GetDamageDataset(request, response));
        }
        else if (request.ChartType == DamageChartType.MVP)
        {
            datasets.Add(GetMVPDataset(request, response));
        }
        
        chartConfig.AddDatasets(datasets);
    }

    private IndexableOption<string> GetTitle(DamageRequest request)
    {
        if (request.FromRating == 0 && request.ToRating == 0 && request.Exp2WinOffset == 0)
        {
            return new IndexableOption<string>($"{request.ChartType} - {Data.GetRatingTypeLongName(request.RatingType)} - Breakpoint {Request.Breakpoint} - {Data.GetTimePeriodLongName(request.TimePeriod)}");
        }
        else
        {
            List<string> titles = new()
            {
                $"{request.ChartType} - {Data.GetRatingTypeLongName(request.RatingType)} - Breakpoint {Request.Breakpoint} - {Data.GetTimePeriodLongName(request.TimePeriod)}"
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

    private List<DamageEnt> GetSortedData()
    {
        var data = Response.Entities
                .Where(x => x.Breakpoint == Request.Breakpoint)
                .AsQueryable();

        foreach (var order in TableOrders)
        {
            if (order.Ascending)
            {
                data = data.AppendOrderBy(order.Property);
            }
            else
            {
                data = data.AppendOrderByDescending(order.Property);
            }
        }
        return data.ToList();
    }

    private List<string> GetLabels(DamageRequest request, DamageResponse response)
    {
        return GetSortedData()
                .Select(s => s.Commander.ToString())
                .ToList();
    }

    private ChartJsDataset GetMVPDataset(DamageRequest request, DamageResponse response)
    {
        var data = GetSortedData();

        return new BarDataset()
        {
            Label = "MVP",
            Data = data
                .Select(s => s.MvpPercentage)
                .Cast<object>()
                .ToList(),
            BackgroundColor = new IndexableOption<string>(data.Select(s => Data.GetBackgroundColor(s.Commander, "66")).ToList()),
            BorderColor = new IndexableOption<string>(data.Select(s => Data.GetBackgroundColor(s.Commander)).ToList()),
            BorderWidth = new IndexableOption<double>(2)
        };
    }

    private ChartJsDataset GetDamageDataset(DamageRequest request, DamageResponse response)
    {
        var data = GetSortedData();

        return new BarDataset()
        {
            Label = "Kills",
            Data = data
                .Select(s => s.AvgKills)
                .Cast<object>()
                .ToList(),
            BackgroundColor = new IndexableOption<string>(data.Select(s => Data.GetBackgroundColor(s.Commander, "66")).ToList()),
            BorderColor = new IndexableOption<string>(data.Select(s => Data.GetBackgroundColor(s.Commander)).ToList()),
            BorderWidth = new IndexableOption<double>(2)
        };
    }

    private ChartJsDataset GetArmyDataset(DamageRequest request, DamageResponse response)
    {
        var data = GetSortedData();

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
        var data = GetSortedData();

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
            Options = new ChartJsOptions()
            {
                MaintainAspectRatio = true,
                Responsive = true,
                IndexAxis = "y",
                Plugins = new Plugins()
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
                    //Legend = new Legend()
                    //{
                    //    Display = false,
                    //    Labels = new Labels()
                    //    {
                    //        Padding = 0,
                    //        BoxHeight = 0,
                    //        BoxWidth = 0
                    //    }
                    //}
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
                            Color = "#f39c12"
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
