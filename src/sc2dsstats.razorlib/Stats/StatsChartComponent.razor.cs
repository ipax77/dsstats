using MathNet.Numerics;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using pax.BlazorChartJs;
using pax.dsstats.shared;
using sc2dsstats.razorlib.Services;

namespace sc2dsstats.razorlib.Stats;

public partial class StatsChartComponent : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public StatsRequest StatsRequest { get; set; } = default!;

    [Parameter]
    public EventCallback<Commander> OnLabelClicked { get; set; }

    [Inject]
    protected ILogger<StatsChartComponent> Logger { get; set; } = default!;

    [Inject]
    protected IJSRuntime IJSRuntime { get; set; } = default!;

    [Inject]
    protected IDataService dataService { get; set; } = default!;

    private IconsChartJsConfig chartConfig = null!;
    private ChartComponent? chartComponent;

    SemaphoreSlim ssChart = new(1, 1);
    SemaphoreSlim ssInit = new(1, 1);
    bool isIconPluginRegistered;
    private int iconX = 30;
    private int iconY = 30;

    protected override void OnInitialized()
    {
        ssInit.WaitAsync();
        chartConfig = new();
        InitChart();

        base.OnInitialized();
    }

    private void InitChart()
    {
        var requestedChartType = GetRequestChartType(StatsRequest);
        _ = requestedChartType switch
        {
            ChartType.bar => SetBarChartConfig(StatsRequest),
            ChartType.pie => SetPieChartConfig(StatsRequest),
            ChartType.radar => SetRadarChartConfig(StatsRequest),
            ChartType.line => SetLineChartConfig(StatsRequest),
            _ => SetBarChartConfig(StatsRequest)
        };
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
            if (!isIconPluginRegistered)
            {
                IJSRuntime.InvokeVoidAsync("registerImagePlugin", iconX, iconY);
                isIconPluginRegistered = true;
            }
            Logger.LogInformation("chart init");
            ssInit.Release();
        }
    }

    public void SetBeginAtZero(bool beginAtZero)
    {
        if (chartConfig.Options?.Scales?.Y is LinearAxis linearAxis)
        {
            linearAxis.BeginAtZero = beginAtZero;
            chartConfig.UpdateChartOptions();
        }
    }

    public async Task PrepareChart(StatsRequest statsRequest, bool addOrRemove)
    {
        await ssChart.WaitAsync();
        try
        {
            var requestedChartType = GetRequestChartType(statsRequest);

            if (chartConfig.Type == requestedChartType && chartConfig.Data.Datasets.Any())
            {
                if (addOrRemove)
                {
                    var removeDatasets = GetAddRemoveDatasets(statsRequest);
                    if (removeDatasets.Any())
                    {
                        foreach (var removeDataset in removeDatasets)
                        {
                            chartConfig.RemoveDataset(removeDataset);
                        }
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
                if (chartConfig.Options != null && chartConfig.Options.Plugins != null)
                {
                    chartConfig.Options.Plugins.BarIcons = null;
                }
                _ = requestedChartType switch
                {
                    ChartType.bar => SetBarChartConfig(statsRequest),
                    ChartType.pie => SetPieChartConfig(statsRequest),
                    ChartType.radar => SetRadarChartConfig(statsRequest),
                    ChartType.line => SetLineChartConfig(statsRequest),
                    _ => SetBarChartConfig(statsRequest)
                };
                chartConfig.ReinitializeChart();
                await ssInit.WaitAsync();
            }
        }
        finally
        {
            ssChart.Release();
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
            var barDatasets = chartConfig.Data.Datasets
                .Cast<BarDataset>()
                .Where(f => f.Label == datasetLabel);
            if (barDatasets.Any())
            {
                return new List<ChartJsDataset>(barDatasets);
            }
        }
        else if (chartConfig.Type == ChartType.radar)
        {
            var radarDatasets = chartConfig.Data.Datasets
                .Cast<RadarDataset>()
                .Where(f => f.Label == datasetLabel);
            if (radarDatasets.Any())
            {
                return new List<ChartJsDataset>(radarDatasets);
            }
        }
        else if (chartConfig.Type == ChartType.line)
        {
            var lineDatasets = chartConfig.Data.Datasets
                .Cast<LineDataset>()
                .Where(f => f.Label == datasetLabel);
            if (lineDatasets.Any())
            {
                var linePointsDatasets = chartConfig.Data.Datasets
                .Cast<LineDataset>()
                .Where(f => f.Label == $"{datasetLabel} Data");
                return new List<ChartJsDataset>(lineDatasets.Concat(linePointsDatasets));
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

    private IndexableOption<string> GetRequestTitle(StatsRequest statsRequest)
    {
        string title = $"{statsRequest.StatsMode} - {statsRequest.TimePeriod}";
        if (statsRequest.Uploaders)
        {
            string append = !Data.IsMaui || dataService.GetFromServer() ? "Uploaders" : "Players";
            title = title + " - " + append;
        }
        return new IndexableOption<string>(title);
    }

    private void SetChartTitle(IndexableOption<string> title)
    {
        if (chartConfig.Options != null
            && chartConfig.Options.Plugins != null
            && chartConfig.Options.Plugins.Title != null)
        {
            chartConfig.Options.Plugins.Title.Text = title;
            chartConfig.UpdateChartOptions();
        }
    }

    public async Task SetupChart(StatsResponse statsResponse)
    {
        await ssChart.WaitAsync();
        await ssInit.WaitAsync();
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

            var title = GetRequestTitle(statsResponse.Request);
            if (chartConfig.Options?.Plugins?.Title != null && chartConfig.Options?.Plugins?.Title?.Text != title)
            {
                SetChartTitle(title);
            }
        }
        finally
        {
            ssChart.Release();
            ssInit.Release();
        }
        Logger.LogInformation("chart setup");
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

        List<ChartIconsConfig> icons = items.Select(s => new ChartIconsConfig()
        {
            XWidth = iconX,
            YWidth = iconY,
            YOffset = 0,
            ImageSrc = HelperService.GetImageSrc(s.Cmdr),
            Cmdr = s.Cmdr.ToString().ToLower()
        }).ToList();
        if (chartConfig.Options != null && chartConfig.Options.Plugins != null)
        {
            chartConfig.Options.Plugins.BarIcons = icons;
        }
        chartConfig.UpdateChartOptions();

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
            Data = GetNiceLineData(statsResponse.Items.Select(s => s.Winrate).ToList(), Math.Min(6, Math.Max(statsResponse.Items.Count / 4, 3))).Select(s => (object)Math.Round(s, 2)).ToList(),
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

        //List<ChartIconsConfig> icons = statsResponse.Items.Select(s => new ChartIconsConfig()
        //{
        //    XWidth = iconX,
        //    YWidth = iconY,
        //    YOffset = 0,
        //    ImageSrc = HelperService.GetImageSrc(s.Cmdr),
        //    Cmdr = s.Cmdr.ToString().ToLower()
        //}).ToList();
        //if (chartConfig.Options != null && chartConfig.Options.Plugins != null)
        //{
        //    chartConfig.Options.Plugins.BarIcons = icons;
        //}
        //chartConfig.UpdateChartOptions();
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

        List<ChartIconsConfig> icons = items.Select(s => new ChartIconsConfig()
        {
            XWidth = iconX,
            YWidth = iconY,
            YOffset = 0,
            ImageSrc = HelperService.GetImageSrc(s.Cmdr),
            Cmdr = s.Cmdr.ToString().ToLower()
        }).ToList();
        if (chartConfig.Options != null && chartConfig.Options.Plugins != null)
        {
            chartConfig.Options.Plugins.BarIcons = icons;
        }
        chartConfig.UpdateChartOptions();

        return true;
    }

    private bool SetBarChartConfig(StatsRequest statsRequest)
    {
        chartConfig.Type = ChartType.bar;
        chartConfig.Data.Labels = new List<string>();
        chartConfig.Data.Datasets = new List<ChartJsDataset>();
        chartConfig.Options = new IconsChartJsOptions()
        {
            MaintainAspectRatio = true,
            Responsive = true,
            OnClickEvent = true,
            Plugins = new IconsPlugins()
            {
                Title = new()
                {
                    Display = true,
                    Text = GetRequestTitle(statsRequest),
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
        chartConfig.Options = new IconsChartJsOptions()
        {
            MaintainAspectRatio = true,
            Responsive = true,
            Plugins = new IconsPlugins()
            {
                Legend = new Legend()
                {
                    Position = "bottom",
                    Labels = new Labels()
                    {
                        Color = "#f2f2f2",
                    }
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
                    Text = GetRequestTitle(statsRequest),
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
        chartConfig.Options = new IconsChartJsOptions()
        {
            Responsive = true,
            MaintainAspectRatio = true,
            Plugins = new IconsPlugins()
            {
                Legend = new Legend()
                {
                    Position = "bottom",
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
                    Text = GetRequestTitle(statsRequest),
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
        chartConfig.Options = new IconsChartJsOptions()
        {
            Responsive = true,
            MaintainAspectRatio = true,
            Plugins = new IconsPlugins()
            {
                Legend = new Legend()
                {
                    Position = "bottom"
                },
                Title = new Title()
                {
                    Display = true,
                    Font = new Font()
                    {
                        Size = 20,
                    },
                    Text = GetRequestTitle(statsRequest),
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

    private void SetBarChartIcons()
    {

    }

    private void DEBUGRemovaDatasets()
    {
        //chartConfig.RemoveDatasets(chartConfig.Data.Datasets.ToList());
        foreach (var dataset in chartConfig.Data.Datasets.ToArray())
        {
            chartConfig.RemoveDataset(dataset);
        }
    }

    public class IconsChartJsConfig : ChartJsConfig
    {
        public new IconsChartJsOptions? Options { get; set; }
    }

    public record IconsChartJsOptions : ChartJsOptions
    {
        public new IconsPlugins? Plugins { get; set; }
    }

    public record IconsPlugins : Plugins
    {
        public ICollection<ChartIconsConfig>? BarIcons { get; set; }
    }
}
