using dsstats.shared;
using dsstats.shared.Extensions;
using Microsoft.AspNetCore.Components;
using pax.BlazorChartJs;

namespace dsstats.razorlib.Stats.Damage;

public partial class DamageChart : ComponentBase
{
    [Parameter, EditorRequired]
    public StatsRequest Request { get; set; } = default!;

    [Parameter, EditorRequired]
    public Breakpoint Breakpoint { get; set; }

    [Parameter, EditorRequired]
    public DamageChartType DamageChartType { get; set; }

    [Parameter, EditorRequired]
    public DamageResponse Response { get; set; } = default!;

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
            SetupChart(Request, Breakpoint, DamageChartType, Response, TableOrders);
        }
    }

    public void SetupChart(StatsRequest request,
                           Breakpoint breakpoint,
                           DamageChartType damageChartType,
                           DamageResponse response,
                           List<TableOrder> tableOrders)
    {
        Request = request;
        Breakpoint = breakpoint;
        DamageChartType = damageChartType;
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

        chartConfig.SetLabels(GetLabels());

        List<ChartJsDataset> datasets = new();

        if (DamageChartType == DamageChartType.Army)
        {
            datasets.Add(GetArmyDataset());
            datasets.Add(GetUpgradesDataset());
        }
        else if (DamageChartType == DamageChartType.Damage)
        {
            datasets.Add(GetDamageDataset());
        }
        else if (DamageChartType == DamageChartType.MVP)
        {
            datasets.Add(GetMVPDataset());
        }

        chartConfig.AddDatasets(datasets);
    }

    private IndexableOption<string> GetTitle(StatsRequest request)
    {
        var title = $"{GetTimeInfo(request)} - {Breakpoint}";

        if (request.Interest != Commander.None)
        {
            title = $"{DamageChartType} vs {request.Interest} - " + title;
        }
        else
        {
            title = $"{DamageChartType} - " + title;
        }

        List<string> titleList = [title];

        if (request.Filter is not null)
        {
            if (request.Filter.Rating is not null
                && (request.Filter.Rating.FromRating > 0 || request.Filter.Rating.ToRating > 0))
            {
                titleList.Add($"Rating Range {(request.Filter.Rating.FromRating == Data.MinBuildRating ? 0 : request.Filter.Rating.FromRating)} - {(request.Filter.Rating.ToRating == Data.MaxBuildRating ? $"{request.Filter.Rating.ToRating}+" : $"{request.Filter.Rating.ToRating}")}");
            }

            if (request.Filter.Exp2Win is not null
                && (request.Filter.Exp2Win.FromExp2Win > 0 || request.Filter.Exp2Win.ToExp2Win > 0))
            {
                titleList.Add($"Exp2Win Range {request.Filter.Exp2Win.FromExp2Win}% - {request.Filter.Exp2Win.ToExp2Win}%");
            }
        }

        if (titleList.Count == 1)
        {
            return new IndexableOption<string>(titleList[0]);
        }
        else
        {
            return new IndexableOption<string>(titleList);
        }
    }

    private string GetTimeInfo(StatsRequest request)
    {
        if (request.Filter.Time is null)
        {
            return Data.GetTimePeriodLongName(request.TimePeriod);
        }
        else
        {
            return $"{request.Filter.Time.FromDate.ToString("yyyy-MM-dd")} - {request.Filter.Time.ToDate.ToString("yyyy-MM-dd")}";
        }
    }

    private List<DamageEnt> GetSortedData()
    {
        var data = Response.Entities
                .Where(x => x.Breakpoint == Breakpoint)
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

    private List<string> GetLabels()
    {
        return GetSortedData()
                .Select(s => s.Commander.ToString())
                .ToList();
    }

    private ChartJsDataset GetMVPDataset()
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

    private ChartJsDataset GetDamageDataset()
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

    private ChartJsDataset GetArmyDataset()
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

    private ChartJsDataset GetUpgradesDataset()
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
