using MathNet.Numerics;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using pax.BlazorChartJs;
using pax.dsstats.shared;

namespace sc2dsstats.razorlib.Stats;

public partial class StatsChartComponent : ComponentBase
{
    [Parameter]
    public EventCallback<Commander> OnLabelClicked { get; set; }

    [Parameter]
    public bool IsMaui { get; set; }

    [Inject]
    protected ILogger<StatsChart> Logger { get; set; } = default!;

    private ChartJsConfig chartConfig = null!;
    private ChartComponent? chartComponent;

    SemaphoreSlim ss = new(1, 1);

    protected override void OnInitialized()
    {
        ss.WaitAsync();
        chartConfig = new();
        SetBarChartConfig(new() { StatsMode = StatsMode.Winrate });
        base.OnInitialized();
    }

    public void SetBeginAtZero(bool beginAtZero)
    {
        if (chartConfig.Options?.Scales?.Y is LinearAxis linearAxis)
        {
            linearAxis.BeginAtZero = beginAtZero;
            chartComponent?.UpdateChartOptions();
        }
    }

    public async Task PrepareChart(StatsRequest statsRequest, bool addOrRemove)
    {
        await ss.WaitAsync();
        try
        {
            var requestedChartType = GetRequestChartType(statsRequest);

            if (chartConfig.Type == requestedChartType && chartConfig.Data.Datasets.Any())
            {
                if (addOrRemove)
                {
                    var datasets = GetAddRemoveDatasets(statsRequest);
                    if (datasets.Any())
                    {
                        chartConfig.RemoveDatasets(datasets);
                        // fs fix
                        await Task.Delay(100);
                    }
                }
                else
                {
                    foreach (var dataset in chartConfig.Data.Datasets.ToArray())
                    {
                        chartConfig.RemoveDataset(dataset);
                    }
                }
            }
            else
            {
                _ = requestedChartType switch
                {
                    ChartType.bar => SetBarChartConfig(statsRequest),
                    ChartType.pie => SetPieChartConfig(statsRequest),
                    ChartType.radar => SetRadarChartConfig(statsRequest),
                    ChartType.line => SetLineChartConfig(statsRequest),
                    _ => SetBarChartConfig(statsRequest)
                };
                chartComponent?.DrawChart();
                // fs fix
                await Task.Delay(100);
            }
        }
        finally
        {
            ss.Release();
        }
        Logger.LogInformation("chart prepared");
    }

    private ChartType GetRequestChartType(StatsRequest statsRequest)
    {
        return statsRequest.StatsMode switch
        {
            StatsMode.Winrate => ChartType.bar,
            StatsMode.Mvp => ChartType.bar,
            StatsMode.Synergy => ChartType.radar,
            StatsMode.Duration => ChartType.line,
            StatsMode.Timeline => ChartType.line,
            StatsMode.Count => ChartType.pie,
            _ => ChartType.bar
        };
    }

    private List<ChartJsDataset> GetAddRemoveDatasets(StatsRequest request)
    {
        var datasetLabel = GetRequestDatasetLabel(request);
        if (chartConfig.Type == ChartType.bar)
        {
            var barDataset = chartConfig.Data.Datasets
                .Cast<BarDataset>()
                .FirstOrDefault(f => f.Label == datasetLabel);
            if (barDataset != null)
            {
                return new List<ChartJsDataset>() { barDataset };
            }
        }
        else if (chartConfig.Type == ChartType.radar)
        {
            var radarDataset = chartConfig.Data.Datasets
                .Cast<RadarDataset>()
                .FirstOrDefault(f => f.Label == datasetLabel);
            if (radarDataset != null)
            {
                return new List<ChartJsDataset>() { radarDataset };
            }
        }
        else if (chartConfig.Type == ChartType.line)
        {
            var lineDataset = chartConfig.Data.Datasets
                .Cast<BarDataset>()
                .FirstOrDefault(f => f.Label == datasetLabel);
            if (lineDataset != null)
            {
                var linePointsDataset = chartConfig.Data.Datasets
                .Cast<BarDataset>()
                .FirstOrDefault(f => f.Label == $"{datasetLabel} Data");
                return linePointsDataset == null ?
                    new List<ChartJsDataset>() { lineDataset }
                    : new List<ChartJsDataset>() { lineDataset, linePointsDataset };
            }
        }
        return new();
    }

    private string GetRequestDatasetLabel(StatsRequest statsRequest)
    {
        if (statsRequest.Interest == Commander.None)
        {
            return "All";
        }
        else
        {
            return $"{statsRequest.Interest}";
        }
    }

    public async Task SetupChart(StatsResponse statsResponse)
    {
        await ss.WaitAsync();
        try
        {
            _ = chartConfig.Type switch
            {
                ChartType.bar => AddBarChartDataset(statsResponse),
                ChartType.pie => AddPieChartDataset(statsResponse),
                ChartType.radar => AddRadarChartDataset(statsResponse),
                ChartType.line => AddLineChartDataset(statsResponse),
                _ => AddBarChartDataset(statsResponse)
            };
        }
        finally
        {
            ss.Release();
        }
        Logger.LogInformation("chart setup");
    }

    private void ChartEventTriggered(ChartJsEvent chartJsEvent)
    {
        if (chartJsEvent is ChartJsLabelClickEvent labelClickEvent)
        {
            if (Enum.TryParse(typeof(Commander), labelClickEvent.Label, out object? cmdrObj))
            {
                if (cmdrObj is Commander cmdr)
                {
                    OnLabelClicked.InvokeAsync(cmdr);
                }
            }
        }
        else if (chartJsEvent is ChartJsInitEvent initEvent)
        {
            ss.Release();
            Logger.LogInformation("chart init");
        }
        else if (chartJsEvent is ChartJsAnimationCompleteEvent animationCompleteEvent)
        {
            Logger.LogInformation($"chart animation complete {animationCompleteEvent.Initial}");
        }
    }

    private bool AddBarChartDataset(StatsResponse statsResponse)
    {
        var items = statsResponse.Items.OrderBy(o => o.Winrate).ToList();

        chartConfig.SetLabels(items.Select(s => s.Label).ToList());

        var barDataset = new BarDataset()
        {
            Label = GetRequestDatasetLabel(statsResponse.Request),
            Data = items.Select(s => (object)Math.Round(s.Winrate, 2)).ToList(),
            BackgroundColor = statsResponse.Request.Interest == Commander.None ?
                        new IndexableOption<string>(items.Select(s => Data.GetBackgroundColor(s.Cmdr)).ToList())
                        : new IndexableOption<string>(Data.GetBackgroundColor(statsResponse.Request.Interest)),
            BorderColor = statsResponse.Request.Interest == Commander.None ?
                        new IndexableOption<string>(items.Select(s => Data.CmdrColor[s.Cmdr]).ToList())
                        : new IndexableOption<string>(Data.CmdrColor[statsResponse.Request.Interest]),
            BorderWidth = new IndexableOption<double>(2)
        };
        chartConfig.AddDataset(barDataset);
        return true;
    }

    private bool AddLineChartDataset(StatsResponse statsResponse)
    {
        chartConfig.SetLabels(statsResponse.Items.Select(s => s.Label).ToList());

        var dataPoints = new LineDataset()
        {
            Label = GetRequestDatasetLabel(statsResponse.Request) + " Data",
            Data = statsResponse.Items.Select(s => (object)Math.Round(s.Winrate, 2)).ToList(),
            PointBackgroundColor = statsResponse.Request.Interest == Commander.None ? new IndexableOption<string>("blue") : new IndexableOption<string>(Data.CmdrColor[statsResponse.Request.Interest]),
            PointRadius = new IndexableOption<double>(3),
            ShowLine = false
        };
        var niceLine = new LineDataset()
        {
            Label = GetRequestDatasetLabel(statsResponse.Request),
            Data = GetNiceLineData(statsResponse.Items.Select(s => s.Winrate).ToList(), Math.Max(statsResponse.Items.Count / 4, 3)).Select(s => (object)Math.Round(s, 2)).ToList(),
            BorderColor = statsResponse.Request.Interest == Commander.None ? "blue" : Data.CmdrColor[statsResponse.Request.Interest],
            BorderWidth = 2,
            Tension = 0.4,
            PointRadius = new IndexableOption<double>(0),
        };

        chartConfig.AddDatasets(new List<ChartJsDataset>() { dataPoints, niceLine });
        return true;
    }

    private bool AddRadarChartDataset(StatsResponse statsResponse)
    {
        chartConfig.SetLabels(statsResponse.Items.Select(s => s.Label).ToList());

        var radarDataset = new RadarDataset()
        {
            Label = GetRequestDatasetLabel(statsResponse.Request),
            Data = statsResponse.Items.Select(s => (object)Math.Round(s.Winrate, 2)).ToList(),
            BackgroundColor = statsResponse.Request.Interest == Commander.None ? "blue" : Data.GetBackgroundColor(statsResponse.Request.Interest),
            BorderColor = statsResponse.Request.Interest == Commander.None ? "blue" : Data.CmdrColor[statsResponse.Request.Interest],
            BorderWidth = 3,
            PointRadius = new IndexableOption<double>(4),
            Fill = true
        };
        chartConfig.AddDataset(radarDataset);
        return true;
    }


    private bool AddPieChartDataset(StatsResponse statsResponse)
    {
        var items = statsResponse.Items.OrderByDescending(o => o.Matchups).ToList();

        chartConfig.SetLabels(items.Select(s => s.Label).ToList());

        var pieChartDataset =
                new PieDataset()
                {
                    Data = items.Select(s => (object)s.Matchups).ToList(),
                    BackgroundColor = statsResponse.Request.Interest == Commander.None ?
                        new IndexableOption<string>(items.Select(s => Data.GetBackgroundColor(s.Cmdr)).ToList())
                        : new IndexableOption<string>(Data.GetBackgroundColor(statsResponse.Request.Interest)),
                    BorderColor = statsResponse.Request.Interest == Commander.None ?
                        new IndexableOption<string>(items.Select(s => Data.CmdrColor[s.Cmdr]).ToList())
                        : new IndexableOption<string>(Data.CmdrColor[statsResponse.Request.Interest]),
                    BorderWidth = new IndexableOption<double>(2)
                };

        chartConfig.AddDataset(pieChartDataset);
        return true;
    }

    private bool SetBarChartConfig(StatsRequest statsRequest)
    {
        chartConfig.Type = ChartType.bar;
        chartConfig.Data.Labels = new List<string>();
        chartConfig.Data.Datasets = new List<ChartJsDataset>();
        chartConfig.Options = new ChartJsOptions()
        {
            MaintainAspectRatio = true,
            Responsive = true,
            OnClickEvent = true,
            Plugins = new Plugins()
            {
                Title = new()
                {
                    Display = true,
                    Text = $"{statsRequest.StatsMode} {(statsRequest.Interest == Commander.None ? "" : statsRequest.Interest)}",
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
                Y = new LinearAxis()
                {
                    BeginAtZero = statsRequest.BeginAtZero,
                }
            }
        };
        return true;
    }



    private bool SetLineChartConfig(StatsRequest statsRequest)
    {
        chartConfig.Type = ChartType.line;
        chartConfig.Data.Labels = new List<string>();
        chartConfig.Data.Datasets = new List<ChartJsDataset>();
        chartConfig.Options = new ChartJsOptions()
        {
            MaintainAspectRatio = true,
            Responsive = true,
            Plugins = new Plugins()
            {
                Labels = new LabelsConfig()
                {
                    Render = "image",
                    Images = new List<LabelsConfigImage>()
                },
                Datalabels = new()
                {
                    Display = false
                },
                Title = new Title()
                {
                    Display = true,
                    Font = new Font()
                    {
                        Size = 20,
                    },
                    Text = statsRequest.StatsMode.ToString(),
                    Color = "#f2f2f2"
                }
            }
        };
        return true;
    }



    private bool SetRadarChartConfig(StatsRequest statsRequest)
    {
        chartConfig.Type = ChartType.radar;
        chartConfig.Data.Labels = new List<string>();
        chartConfig.Data.Datasets = new List<ChartJsDataset>();
        chartConfig.Options = new ChartJsOptions()
        {
            Responsive = true,
            MaintainAspectRatio = true,
            Plugins = new Plugins()
            {
                Legend = new Legend()
                {
                    Position = "right",
                    Labels = new Labels()
                    {
                        Color = "#f2f2f2",
                    }
                },
                Title = new Title()
                {
                    Display = true,
                    Font = new Font()
                    {
                        Size = 20,
                    },
                    Text = statsRequest.StatsMode.ToString(),
                    Color = "#f2f2f2"
                }
            },
            Scales = new ChartJsOptionsScales()
            {
                R = new LinearRadialAxis()
                {
                    AngelLines = new AngelLines()
                    {
                        Display = true,
                        Color = "#f2f2f233"
                    },
                    Grid = new ChartJsGrid()
                    {
                        Display = true,
                        Color = "#f2f2f233"
                    },
                    PointLabels = new PointLabels()
                    {
                        Display = true,
                        Font = new Font()
                        {
                            Size = 12
                        },
                        Color = "#f2f2f2"
                    },
                    BeginAtZero = true
                }
            }
        };
        return true;
    }

    private bool SetPieChartConfig(StatsRequest statsRequest)
    {
        chartConfig.Type = ChartType.pie;
        chartConfig.Data.Labels = new List<string>();
        chartConfig.Data.Datasets = new List<ChartJsDataset>();
        chartConfig.Options = new ChartJsOptions()
        {
            Plugins = new Plugins()
            {
                Legend = new Legend()
                {
                    Position = "right"
                },
                Title = new Title()
                {
                    Display = true,
                    Font = new Font()
                    {
                        Size = 20,
                    },
                    Text = statsRequest.StatsMode.ToString(),
                    Color = "#f2f2f2"
                }
            }
        };
        return true;
    }

    private static List<double> GetNiceLineData(List<double> data, int order)
    {
        List<double> xdata = new List<double>();
        for (int i = 0; i < data.Count(); i++)
        {
            xdata.Add(i);
        }

        if (xdata.Count < 4)
            return new List<double>();

        if (xdata.Count() < order)
            order = Math.Max(xdata.Count() - 2, 3);

        var poly = Fit.PolynomialFunc(xdata.ToArray(), data.ToArray(), order);

        List<double> nicedata = new List<double>();
        for (int i = 0; i < data.Count(); i++)
        {
            nicedata.Add(Math.Round(poly(i), 2));
        }

        return nicedata;
    }

    private void DEBUGRemovaDatasets()
    {
        //chartConfig.RemoveDatasets(chartConfig.Data.Datasets.ToList());
        foreach (var dataset in chartConfig.Data.Datasets.ToArray())
        {
            chartConfig.RemoveDataset(dataset);
        }
    }
}
