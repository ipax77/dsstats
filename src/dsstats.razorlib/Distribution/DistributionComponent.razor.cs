using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using pax.BlazorChartJs;

namespace dsstats.razorlib.Distribution
{
    public partial class DistributionComponent : ComponentBase
    {
        [Inject]
        public IPlayerService playerService { get; set; } = default!;

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Parameter, EditorRequired]
        public DistributionRequest Request { get; set; } = default!;

        bool isLoading;
        private readonly string mainColor = "#3F5FFA";
        DistributionResponse? response = null;
        ChartJsConfig chartConfig = null!;
        bool chartReady;
        bool dataReady;

        protected override void OnInitialized()
        {
            _ = LoadData();
            chartConfig = GetChartConfig();
            base.OnInitialized();
        }

        public void Update(DistributionRequest request)
        {
            Request.RatingCalcType = request.RatingCalcType;
            Request.RatingType = request.RatingType;
            Request.TimePeriod = request.TimePeriod;
            Request.Interest = request.Interest;
            _ = LoadData();
        }

        private async Task LoadData()
        {
            isLoading = true;
            await InvokeAsync(() => StateHasChanged());
            response = await playerService.GetDistribution(Request);
            dataReady = true;
            if (chartReady && dataReady)
            {
                SetupChart();
            }
            isLoading = false;
            await InvokeAsync(() => StateHasChanged());
        }

        private void ChartEventTriggered(ChartJsEvent chartJsEvent)
        {
            if (chartJsEvent is ChartJsInitEvent)
            {
                chartReady = true;
                if (chartReady && dataReady)
                {
                    SetupChart();
                }
            }
        }

        private void SetupChart()
        {
            if (chartConfig.Data.Datasets.Count > 0)
            {
                chartConfig.RemoveDatasets(chartConfig.Data.Datasets);
            }

            if (chartConfig.Options?.Plugins?.Title is not null)
            {
                chartConfig.Options.Plugins.Title.Text = new IndexableOption<string>($"{Request.RatingCalcType} {Request.RatingType} Distribution");
                chartConfig.UpdateChartOptions();
            }

            if (response is null || response.MmrDevs.Count == 0)
            {
                return;
            }

            chartConfig.SetLabels(response.MmrDevs.Select(s => s.Mmr.ToString()).ToArray());

            List<int> ratings = new();
            List<int> counts = new();

            int count = 0;
            for (int i = 0; i < response.MmrDevs.Count; i++)
            {
                var mmrDev = response.MmrDevs[i];
                ratings.Add(mmrDev.Count);
                count += mmrDev.Count;
                counts.Add(count);
            }

            var lineDataset = GetLineDataset(ratings);
            var sumDataset = GetSumLineDataset(counts);
            chartConfig.AddDatasets([lineDataset, sumDataset]);
            JSRuntime.InvokeVoidAsync("scrollToElementId", "distributionChart");
        }

        private LineDataset GetLineDataset(List<int> ratings)
        {
            return new()
            {
                Label = "",
                Data = new List<object>(ratings.Cast<object>()),
                BackgroundColor = "#4E58A066",
                BorderColor = "#4E58A0",
                BorderWidth = 4,
                Fill = true,
                PointBackgroundColor = new IndexableOption<string>("blue"),
                PointBorderColor = new IndexableOption<string>("blue"),
                PointRadius = new IndexableOption<double>(1),
                PointBorderWidth = new IndexableOption<double>(1),
                PointHitRadius = new IndexableOption<double>(1),
                Tension = 0.4,
                YAxisID = "y"
            };
        }

        private LineDataset GetSumLineDataset(List<int> counts)
        {
            return new()
            {
                Label = "",
                Data = new List<object>(counts.Cast<object>()),
                BackgroundColor = "yellow",
                BorderColor = "yellow",
                BorderWidth = 2,
                Fill = false,
                PointBackgroundColor = new IndexableOption<string>("yellow"),
                PointBorderColor = new IndexableOption<string>("yellow"),
                PointRadius = new IndexableOption<double>(0),
                PointBorderWidth = new IndexableOption<double>(0),
                PointHitRadius = new IndexableOption<double>(0),
                BorderDash = new List<double>() { 10, 5 },
                Tension = 0.2,
                YAxisID = "y1"
            };
        }

        private ChartJsConfig GetChartConfig()
        {
            return new()
            {
                Type = ChartType.line,
                Data = new ChartJsData()
                {
                    Labels = new List<string>()
                    {
                    },
                    Datasets = new List<ChartJsDataset>()
                    {
                    }
                },
                Options = new ChartJsOptions()
                {
                    Responsive = true,
                    MaintainAspectRatio = true,
                    Plugins = new Plugins()
                    {
                        ArbitraryLines = new List<ArbitraryLineConfig>(),
                        Title = new Title()
                        {
                            Display = true,
                            Text = new IndexableOption<string>($"{Request.RatingType} distribution"),
                            Color = "#CED0DD",
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
                                Color = "#CED0DD",
                                BoxHeight = 0,
                                BoxWidth = 0
                            }
                        }
                    },
                    Interaction = new Interactions()
                    {
                        Intersect = false,
                        Mode = "index"
                    },
                    Scales = new ChartJsOptionsScales()
                    {
                        X = new LinearAxis()
                        {
                            Display = true,
                            Title = new Title()
                            {
                                Display = true,
                                Text = new IndexableOption<string>("Rating"),
                                Color = "#4E58A0"
                            },
                            Ticks = new LinearAxisTick()
                            {
                                Color = mainColor,
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
                            Display = true,
                            Type = "linear",
                            Position = "left",
                            Title = new Title()
                            {
                                Display = true,
                                Text = new IndexableOption<string>("Players"),
                                Color = "#4E58A0"
                            },
                            Ticks = new LinearAxisTick()
                            {
                                Color = mainColor,
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
                        Y1 = new LinearAxis()
                        {
                            Display = true,
                            Title = new Title()
                            {
                                Display = true,
                                Text = new IndexableOption<string>("Sum"),
                                Color = "yellow"
                            },
                            Type = "linear",
                            Position = "right",
                            Ticks = new LinearAxisTick()
                            {
                                Color = "yellow",
                            },
                            Grid = new ChartJsGrid()
                            {
                                DrawOnChartArea = false
                            }
                        }
                    }
                }
            };
        }
    }
}