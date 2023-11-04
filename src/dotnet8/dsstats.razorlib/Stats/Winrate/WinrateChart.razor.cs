using dsstats.razorlib.Models;
using dsstats.razorlib.Services;
using dsstats.shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using pax.BlazorChartJs;

namespace dsstats.razorlib.Stats.Winrate;

public partial class WinrateChart : ComponentBase
{
    [Parameter, EditorRequired]
    public WinrateResponse Response { get; set; } = default!;

    [Parameter, EditorRequired]
    public WinrateRequest Request { get; set; } = default!;

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;

    IconsChartJsConfig chartConfig = null!;
    bool chartReady;
    bool iconsReady;
    private int iconX = 30;
    private int iconY = 30;

    ChartComponent? chartComponent;
    private readonly string mainColor = "#3F5FFA";

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

            if (!iconsReady)
            {
                JSRuntime.InvokeVoidAsync("registerImagePlugin", iconX, iconY);
                JSRuntime.InvokeVoidAsync("increaseChartHeight", chartConfig.ChartJsConfigGuid, iconY);
                iconsReady = true;
            }

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

        if (chartConfig.Options?.Plugins?.Title != null
            && chartConfig.Options?.Scales?.Y?.Title != null)
        {
            chartConfig.Options.Plugins.Title.Text = GetTitle(request);
            chartConfig.Options!.Scales!.Y!.Title!.Text = GetYAxisDesc(request);
            chartConfig.UpdateChartOptions();
        }

        chartConfig.SetLabels(response.WinrateEnts.Select(s => s.Commander.ToString()).ToList());

        chartConfig.AddDataset(GetAvgGainDataset(response, request));
        // chartConfig.AddDataset(GetWinrateDataset(response));

        SetIcons(response);
        JSRuntime.InvokeVoidAsync("setDatalabelsFormatter", chartConfig.ChartJsConfigGuid);

        //if (request.WinrateType == WinrateType.AvgGain || response.WinrateEnts.Any())
        //{
        //    var avg = Math.Round(response.WinrateEnts.Sum(a => a.Count * a.AvgGain) / response.WinrateEnts.Sum(s => s.Count), 2);
        //    JSRuntime.InvokeVoidAsync("drawYValueLine", chartConfig.ChartJsConfigGuid, avg);
        //}
    }

    private IndexableOption<string> GetTitle(WinrateRequest request)
    {
        string title = request.WinrateType switch
        {
            WinrateType.AvgGain => "Average rating gain",
            WinrateType.Matchups => "Count",
            WinrateType.AvgRating => "Average rating",
            WinrateType.Winrate => "Winrate",
            _ => ""
        };
        title += $" - {GetTimeInfo(request)}";

        if (request.Interest != Commander.None)
        {
            title = $"{request.Interest}'s " + title;
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

    private string GetTimeInfo(WinrateRequest request)
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

    private IndexableOption<string> GetYAxisDesc(WinrateRequest request)
    {
        var text = request.WinrateType switch
        {
            WinrateType.AvgGain => "Average rating gain",
            WinrateType.Winrate => "Winrate",
            WinrateType.Matchups => "Matchups",
            WinrateType.AvgRating => "Average player rating",
            _ => "Average rating gain"
        };
        return new IndexableOption<string>(text);
    }

    private List<object> GetData(WinrateResponse response)
    {
        return Request.WinrateType switch
        {
            WinrateType.AvgGain => response.WinrateEnts.Select(s => s.AvgGain).Cast<object>().ToList(),
            WinrateType.Matchups => response.WinrateEnts.Select(s => s.Count).Cast<object>().ToList(),
            WinrateType.AvgRating => response.WinrateEnts.Select(s => s.AvgRating).Cast<object>().ToList(),
            WinrateType.Winrate => response.WinrateEnts.Select(s => Math.Round(s.Wins * 100.0 / s.Count, 2)).Cast<object>().ToList(),
            _ => new List<object>()
        };
    }

    public void ChangeOrder(WinrateResponse response)
    {
        PrepareData(response, Request);
    }

    private ChartJsDataset GetAvgGainDataset(WinrateResponse response, WinrateRequest request)
    {
        var barDataset = new BarDataset()
        {
            Label = request.WinrateType.ToString(),
            Data = GetData(response),
            BackgroundColor = new IndexableOption<string>(response.WinrateEnts.Select(s => Data.GetBackgroundColor(s.Commander)).ToList()),
            BorderColor = new IndexableOption<string>(response.WinrateEnts.Select(s => Data.CmdrColor[s.Commander]).ToList()),
            BorderWidth = new IndexableOption<double>(2),
            // Stack = "Stack 0"
        };

        return barDataset;
    }

    private void SetIcons(WinrateResponse response)
    {
        if (chartConfig.Options?.Plugins != null
            && chartConfig.Options?.Plugins is IconsPlugins iconPlugins)
        {
            var icons = response.WinrateEnts.Select(s => new ChartIconsConfig()
            {
                XWidth = iconX,
                YWidth = iconY,
                // YOffset = s.AvgGain < 0 ? iconY : 0,
                YOffset = 0,
                ImageSrc = HelperService.GetImageSrc(s.Commander),
                Cmdr = s.Commander.ToString().ToLower()
            }).ToList();

            iconPlugins.BarIcons = icons;
            chartConfig.UpdateChartOptions();
        }
    }

    private IconsChartJsConfig GetChartConfig()
    {
        return new IconsChartJsConfig()
        {
            Type = ChartType.bar,
            Options = new IconsChartJsOptions()
            {
                MaintainAspectRatio = true,
                Responsive = true,
                Plugins = new IconsPlugins()
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
                        // Stacked = true,
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
                        // Stacked = true  
                        Title = new Title()
                        {
                            Display = true,
                            Text = new IndexableOption<string>("Average rating gain"),
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
