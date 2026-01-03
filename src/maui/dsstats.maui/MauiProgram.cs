using CommunityToolkit.Maui;
using dsstats.db;
using dsstats.dbServices;
using dsstats.maui.Services;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.BlazorChartJs;
using System.Globalization;
using System.Net;

namespace dsstats.maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options =>
                {
                    options.SetShouldEnableSnackbarOnWindows(true);
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddLocalization();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            var sqliteConnectionString = $"Data Source={Path.Combine(FileSystem.Current.AppDataDirectory, "dsstats4.db")}";
            builder.Services.AddDbContext<DsstatsContext>(options => options
                .UseSqlite(sqliteConnectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("dsstats.migrations.sqlite");
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                })
            //.EnableDetailedErrors()
            //.EnableSensitiveDataLogging()
            );

            builder.Services.AddHttpClient("api", httpClient =>
            {
                httpClient.BaseAddress = new Uri("https://dsstats.pax77.org");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            }).ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    AutomaticDecompression =
                        DecompressionMethods.GZip |
                        DecompressionMethods.Brotli,
                });


            builder.Services.AddChartJs(options =>
            {
                var version = "4.5.1";
                options.ChartJsLocation = $"/_content/dsstats.weblib/js/chart.umd.min.js?v={version}";
                options.ChartJsPluginDatalabelsLocation = "/_content/dsstats.weblib/js/chartjs-plugin-datalabels.min.js";
            });
            builder.Services.AddMemoryCache();

            builder.Services.AddSingleton<IImportService, ImportService>();
            builder.Services.AddSingleton<DsstatsService>();
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<ImportState>();
            builder.Services.AddSingleton<SessionProgress>();


            builder.Services.AddScoped<IRatingService, RatingsService>();
            builder.Services.AddScoped<IPlayerService, apiServices.PlayerService>();

            builder.Services.AddScoped<IReplayRepository, ReplayRepository>();
            builder.Services.AddKeyedScoped<IReplayRepository, apiServices.ReplayRepository>("api");

            var app = builder.Build();
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            context.Database.Migrate();

            var culture = context.MauiConfig
                .OrderBy(o => o.MauiConfigId)
                .Select(s => s.Culture)
                .FirstOrDefault() ?? "en";

            var cultureInfo = new CultureInfo(culture);
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            return app;
        }
    }
}
