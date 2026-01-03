using dsstats.shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using pax.BlazorChartJs;

namespace dsstats.weblib.Replays;

public partial class ReplayMiddleChart : ComponentBase, IDisposable
{
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Parameter, EditorRequired]
    public ReplayDto Replay { get; set; } = null!;

    [Parameter]
    public EventCallback OnCloseRequest { get; set; }

    CustomChartJsConfig chartJsConfig = null!;
    private Lazy<Task<IJSObjectReference>> moduleTask = null!;
    bool isRegistered;

    bool chartReady;

    protected override void OnInitialized()
    {
        moduleTask = new(() => JSRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/dsstats.weblib/js/annotationTimeChart.js").AsTask());
        chartJsConfig = new()
        {
            Type = ChartType.line,
            Options = new CustomChartJsOptions()
            {
                Responsive = true,
                MaintainAspectRatio = true,
                Plugins = new CustomPlugins()
                {
                    ArbitraryLines = new List<ArbitraryLineConfig>(),
                    Legend = new Legend()
                    {
                        Position = "top"
                    },
                    Title = new Title()
                    {
                        Display = true,
                        Text = new IndexableOption<string>($"Middle Control - {Replay.MiddleChanges.Count} changes"),
                        Color = "white",
                        Font = new Font()
                        {
                            Size = 16
                        }
                    }
                },
                Scales = new ChartJsOptionsScales()
                {
                    X = new LinearAxis()
                    {
                        Display = true,
                        Position = "bottom",
                        BeginAtZero = true,
                        Title = new Title()
                        {
                            Display = true,
                            Text = new IndexableOption<string>("GameTime"),
                            Color = "yellow",
                        },
                        Ticks = new LinearAxisTick()
                        {
                            Color = "yellow",
                        }
                    },
                    Y = new LinearAxis()
                    {
                        Display = true,
                        BeginAtZero = true,
                        Title = new Title()
                        {
                            Display = true,
                            Text = new IndexableOption<string>("%"),
                            Color = "white",
                        },
                        Ticks = new LinearAxisTick()
                        {
                            Color = "yellow",
                        },
                        Grid = new()
                        {
                            Display = true,
                            Color = "rgba(107, 107, 107, 0.3)",
                            LineWidth = 1,
                            DrawOnChartArea = true,
                            TickLength = 8,
                            TickWidth = 1,
                            TickColor = "#4E58A0",
                            Offset = false,
                        },
                        Border = new()
                        {
                            Display = true,
                            Width = 1,
                            Color = "rgba(255, 255, 255, 0.2)",
                            Dash = [4, 4],
                        }
                    }
                }
            }
        };
    }

    public void SetupChart()
    {
        if (!chartReady)
        {
            return;
        }
        var helper = new MiddleControlHelper(Replay);
        List<ChartMiddleData> team1mid = [];
        List<ChartMiddleData> team2mid = [];

        if (Replay.Duration > 0)
        {
            int seconds = 0;
            while (seconds < Replay.Duration)
            {
                while (seconds < Replay.Duration)
                {
                    var (p1, p2) = helper.GetPercent(seconds);

                    var x = DateTimeOffset.FromUnixTimeSeconds(seconds).ToString("o");
                    team1mid.Add(new ChartMiddleData { X = x, Y = p1 });
                    team2mid.Add(new ChartMiddleData { X = x, Y = p2 });

                    seconds += 30;
                }

                // Final point
                var (lp1, lp2) = helper.GetPercent(Replay.Duration);
                var xt = DateTimeOffset.FromUnixTimeSeconds(Replay.Duration).ToString("o");
                team1mid.Add(new ChartMiddleData { X = xt, Y = lp1 });
                team2mid.Add(new ChartMiddleData { X = xt, Y = lp2 });
            }
        }

        List<ArbitraryLineConfig> objectives = new();
        if (Replay.Cannon > 0 && Replay.Duration > Replay.Cannon)
        {
            objectives.Add(new()
            {
                XPosition = (int)DateTimeOffset.FromUnixTimeSeconds(Replay.Cannon).ToUnixTimeMilliseconds(),
                ArbitraryLineColor = "yellow",
                Text = "Cannon"
            });
        }
        if (Replay.Bunker > 0 && Replay.Duration > Replay.Bunker)
        {
            objectives.Add(new()
            {
                XPosition = (int)DateTimeOffset.FromUnixTimeSeconds(Replay.Bunker).ToUnixTimeMilliseconds(),
                ArbitraryLineColor = "yellow",
                Text = "Bunker"
            });
        }

        var team1Dataset = CreateTeamDataset(1, team1mid);
        var team2Dataset = CreateTeamDataset(2, team2mid);

        if (chartJsConfig.Data.Datasets.Count > 0)
        {
            chartJsConfig.RemoveDatasets(chartJsConfig.Data.Datasets);
        }

        chartJsConfig.AddDatasets(new List<ChartJsDataset>() { team1Dataset, team2Dataset });

        if (chartJsConfig.Options?.Plugins?.ArbitraryLines is not null)
        {
            chartJsConfig.Options.Plugins.ArbitraryLines = objectives;
            chartJsConfig.UpdateChartOptions();
        }
    }

    private LineDataset CreateTeamDataset(int teamNumber, List<ChartMiddleData> data)
    {
        var isWinner = Replay.WinnerTeam == teamNumber;
        var borderColor = isWinner ? "green" : "red";
        // var backgroundColor = isWinner ? "rgba(0, 255, 0, 0.2)" : "rgba(255, 0, 0, 0.2)";

        return new LineDataset()
        {
            Label = $"Team {teamNumber}",
            Data = data.OrderBy(o => o.X).Cast<object>().ToList(),
            BorderColor = borderColor,
            BorderWidth = 3.5,
            Fill = false,
            // BackgroundColor = backgroundColor,
            PointBackgroundColor = "white",
            PointBorderColor = "yellow",
            PointRadius = 1,
            PointBorderWidth = 1,
            PointHitRadius = 1,
            Tension = 0.2
        };
    }

    private void AddAnnotations(
        bool showHide,
        string contentFilter,
        Func<ReplayPlayerDto, IEnumerable<LabelAnnotation>> annotationFactory,
        int resetThreshold,
        int baseYAdjust,
        int yStep,
        string labelPrefix)
    {
        if (showHide)
        {
            var annotations = Replay.Players
                .SelectMany(annotationFactory)
                .OrderBy(a => a.XValue)
                .ToList();

            if (chartJsConfig.Options?.Plugins is not null)
            {
                var chartAnnotations = new Dictionary<string, object>();
                int i = 0, j = 0, prevX = 0;

                foreach (var a in annotations)
                {
                    if (prevX != 0 && Math.Abs(prevX - (int)a.XValue) > resetThreshold)
                    {
                        j = 0;
                    }

                    prevX = (int)a.XValue;
                    a.YAdjust = baseYAdjust - (j * yStep);
                    chartAnnotations.Add($"{labelPrefix}{i}", a);
                    i++;
                    j++;
                }

                if (chartJsConfig.Options.Plugins.Annotation?.Annotations is null)
                {
                    chartJsConfig.Options.Plugins.Annotation = new()
                    {
                        Annotations = chartAnnotations
                    };
                }
                else
                {
                    foreach (var a in chartAnnotations)
                    {
                        chartJsConfig.Options.Plugins.Annotation.Annotations!.TryAdd(a.Key, a.Value);
                    }
                }

                chartJsConfig.UpdateChartOptions();
            }
        }
        else
        {
            if (chartJsConfig.Options?.Plugins?.Annotation?.Annotations is not null)
            {
                foreach (var a in chartJsConfig.Options.Plugins.Annotation.Annotations!.ToArray())
                {
                    if (a.Value is LabelAnnotation la && la.Content.Contains(contentFilter))
                    {
                        chartJsConfig.Options.Plugins.Annotation.Annotations!.Remove(a.Key);
                    }
                }
                chartJsConfig.UpdateChartOptions();
            }
        }
    }

    public void AddTierUpgradeAnnotations(bool showHide)
    {
        AddAnnotations(
            showHide,
            " Tier",
            rp =>
            {
                if (rp.TierUpgrades.Count == 0) return Enumerable.Empty<LabelAnnotation>();
                return rp.TierUpgrades.Select((t, i) => new LabelAnnotation
                {
                    XValue = (int)DateTimeOffset.FromUnixTimeSeconds(t).ToUnixTimeMilliseconds(),
                    Position = "start",
                    Color = "white",
                    BackgroundColor = Replay.WinnerTeam == 1
                        ? (rp.GamePos <= 3 ? "#00bc8c" : "#e74c3c")
                        : (rp.GamePos <= 3 ? "#e74c3c" : "#00bc8c"),
                    BorderRadius = 10,
                    Content = $"#{rp.GamePos} Tier{i + 2} {rp.Race}",
                    Font = new() { Size = 12 }
                });
            },
            resetThreshold: 150000,
            baseYAdjust: -50,
            yStep: 24,
            labelPrefix: "labelT"
        );
    }

    public void AddGasAnnotations(bool showHide)
    {
        AddAnnotations(
            showHide,
            " Gas",
            rp =>
            {
                if (rp.Refineries.Count == 0) return Enumerable.Empty<LabelAnnotation>();
                return rp.Refineries.Select((t, i) => new LabelAnnotation
                {
                    XValue = (int)DateTimeOffset.FromUnixTimeSeconds(t).ToUnixTimeMilliseconds(),
                    Position = "start",
                    Color = "white",
                    BackgroundColor = Replay.WinnerTeam == 1
                        ? (rp.GamePos <= 3 ? "#00bc8c" : "#e74c3c")
                        : (rp.GamePos <= 3 ? "#e74c3c" : "#00bc8c"),
                    BorderRadius = 10,
                    Content = $"#{rp.GamePos} Gas{i + 1} {rp.Race}",
                    Font = new() { Size = 10 }
                });
            },
            resetThreshold: 125000,
            baseYAdjust: -50,
            yStep: 20,
            labelPrefix: "labelG"
        );
    }

    public void AddLeaverAnnotations(bool showHide)
    {
        AddAnnotations(
            showHide,
            " left",
            rp =>
            {
                if (rp.Duration > Replay.Duration - 90) return Enumerable.Empty<LabelAnnotation>();
                return new[]
                {
                new LabelAnnotation
                {
                    XValue = (int)DateTimeOffset.FromUnixTimeSeconds(rp.Duration).ToUnixTimeMilliseconds(),
                    Position = "start",
                    Color = "white",
                    BackgroundColor = "#BF40BF",
                    BorderRadius = 10,
                    Content = $"#{rp.GamePos} left",
                    Font = new() { Size = 12 }
                }
                };
            },
            resetThreshold: 125000,
            baseYAdjust: -50,
            yStep: 20,
            labelPrefix: "labelL"
        );
    }

    private async void ChartEventTriggered(ChartJsEvent chartEvent)
    {
        if (chartEvent is ChartJsInitEvent initEvent)
        {
            var success = await RegisterPlugin();
            chartReady = !success;
            SetupChart();
        }
    }

    private async Task<bool> RegisterPlugin()
    {
        if (!isRegistered)
        {
            var module = await moduleTask.Value.ConfigureAwait(false);
            await module.InvokeVoidAsync("registerPlugin")
                .ConfigureAwait(false);

            isRegistered = true;

            if (chartJsConfig.Options?.Scales?.X != null)
            {
                chartJsConfig.Options.Scales.X = new TimeCartesianAxis()
                {
                    Display = true,
                    Position = "bottom",
                    Type = "time",
                    Time = new TimeCartesianAxisTime()
                    {
                        Unit = "second",
                        TooltipFormat = "mm:ss",
                        DisplayFormats = new
                        {
                            millisecond = "mm:ss.SSS",
                            second = "mm:ss",
                            minute = "mm",
                            hour = "HH"
                        }
                    },
                    Title = new Title()
                    {
                        Display = true,
                        Text = "Duration",
                        Color = "white",
                    },
                    Ticks = new TimeCartesianAxisTicks()
                    {
                        Color = "yellow",
                        StepSize = 30,
                    },
                    Grid = new ChartJsGrid()
                    {
                        Display = true,
                        Color = "rgba(107, 107, 107, 0.3)",
                        LineWidth = 1,
                        DrawOnChartArea = true,
                        TickLength = 8,
                        TickWidth = 1,
                        TickColor = "#4E58A0",
                        Offset = false,
                    },
                    Border = new ChartJsAxisBorder()
                    {
                        Display = true,
                        Width = 1,
                        Color = "rgba(255, 255, 255, 0.2)",
                        Dash = [4, 4],
                    }
                };
            }
            chartJsConfig.ReinitializeChart();
            return true;
        }
        return false;
    }

    public (double, double) GetChartMiddle(ReplayDto replay, int atSecond)
    {
        var (t1, t2) = replay.GetMiddleIncome(atSecond);
        double total = replay.Duration;
        if (total <= 0)
            return (0, 0);

        double p1 = Math.Round(t1 * 100.0 / total, 2);
        double p2 = Math.Round(t2 * 100.0 / total, 2);

        return (p1, p2);
    }

    public void Dispose()
    {
        if (moduleTask.IsValueCreated)
        {
            _ = moduleTask.Value.ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    task.Result.DisposeAsync();
                }
            });
        }
    }
}

public record ChartMiddleData
{
    public string X { get; set; } = string.Empty;
    public double Y { get; set; }
}

public class CustomChartJsConfig : ChartJsConfig
{
    public new CustomChartJsOptions? Options { get; set; }
}

public record CustomChartJsOptions : ChartJsOptions
{
    public new CustomPlugins? Plugins { get; set; }
}

public record CustomPlugins : Plugins
{
    public AnnotationsSettings? Annotation { get; set; }
}

public record AnnotationsSettings
{
    public AnnotationCommon? Common { get; set; }
    public Dictionary<string, object>? Annotations { get; set; }
}

public record AnnotationCommon
{
    public string? DrawTime { get; set; }
}

public record BoxAnnotation
{
    public string? Type { get; set; }
    public double? XMin { get; set; }
    public double? XMax { get; set; }
    public double? YMin { get; set; }
    public double? YMax { get; set; }
    public string? BackgroundColor { get; set; }
}

public record PointAnnotation
{
    public string Type { get; set; } = "point";
    public int XValue { get; set; }
    public int YValue { get; set; }
    public string BackgroundColor { get; set; } = "red";
}

public record LabelAnnotation
{
    public string Type { get; set; } = "label";
    public double XValue { get; set; }
    public double YValue { get; set; }
    public int XAdjust { get; set; }
    public int YAdjust { get; set; }
    public string? Position { get; set; }
    public int Rotation { get; set; }
    public string Color { get; set; } = "black";
    public string BackgroundColor { get; set; } = "red";
    public int BorderRadius { get; set; }
    public string Content { get; set; } = "Sommer";
    public string TextAlign { get; set; } = "start";
    public Font? Font { get; set; }
    public LabelAnnotationCallout? Callout { get; set; }
}

public record LabelAnnotationCallout
{
    public bool Display { get; set; }
    public int Side { get; set; }
    public string? BorderColor { get; set; }
    public int BorderWidth { get; set; } = 1;
    public int Margin { get; set; } = 5;
    public string? Position { get; set; }
}