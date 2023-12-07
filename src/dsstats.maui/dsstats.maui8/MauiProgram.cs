using Microsoft.Extensions.Logging;
using dsstats.db8.AutoMapper;
using dsstats.db8;
using Microsoft.EntityFrameworkCore;
using pax.BlazorChartJs;
using dsstats.shared.Interfaces;
using dsstats.shared;
using dsstats.db8services;
using dsstats.maui8.Services;
using Blazored.Toast;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using dsstats.db8services.Import;
using dsstats.ratings;
using dsstats.maui8.WinUI;
using System.Globalization;
using Microsoft.AspNetCore.Builder;

namespace dsstats.maui8
{
    public static class MauiProgram
    {
        public static readonly string DbName = "dsstats3.db";

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif
            var sqliteConnectionString = $"Data Source={Path.Combine(FileSystem.Current.AppDataDirectory, DbName)}";
            // var sqliteConnectionString = "Data Source=/data/ds/dsstats.db";
            builder.Services.AddDbContext<ReplayContext>(options => options
                .UseSqlite(sqliteConnectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("SqliteMigrations");
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                })
            //.EnableDetailedErrors()
            //.EnableSensitiveDataLogging()
            );

            builder.Services.AddOptions<DbImportOptions>()
                .Configure(x => {
                    x.ImportConnectionString = sqliteConnectionString ?? "";
                    x.IsSqlite = true;
                });

            // builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7048") });
            // builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://dsstats-dev.pax77.org") });
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://dsstats.pax77.org") });
            builder.Services.AddMemoryCache();
            builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
            builder.Services.AddChartJs(options =>
            {
                options.ChartJsLocation = "/_content/dsstats.razorlib/js/chart.js";
                options.ChartJsPluginDatalabelsLocation = "/_content/dsstats.razorlib/js/chartjs-plugin-datalabels.js";
            });
            builder.Services.AddBlazoredToast();
            builder.Services.AddLocalization();
            builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);
            builder.Services.AddSingleton<IFilePicker>(FilePicker.Default);

            builder.Services.AddSingleton<IRemoteToggleService, RemoteToggleService>();
            builder.Services.AddSingleton<ConfigService>();
            builder.Services.AddSingleton<DsstatsService>();
            builder.Services.AddSingleton<ImportService>();
            builder.Services.AddSingleton<IRatingService, RatingService>();
            builder.Services.AddSingleton<IRatingsSaveService, Services.RatingsSaveService>();
            builder.Services.AddSingleton<IUpdateService, StoreUpdateService>();
            // builder.Services.AddSingleton<IUpdateService, StoreUpdateService>();

            builder.Services.AddScoped<BackupService>();
            builder.Services.AddScoped<IReplayRepository, ReplayRepository>();

            builder.Services.AddKeyedScoped<IWinrateService, MauiWinrateService>("local");
            builder.Services.AddKeyedScoped<IWinrateService, apiServices.WinrateService>("remote");
            builder.Services.AddScoped<IWinrateService, Services.WinrateService>();

            builder.Services.AddKeyedScoped<IReplaysService, db8services.ReplaysService>("local");
            builder.Services.AddKeyedScoped<IReplaysService, apiServices.ReplaysService>("remote");
            builder.Services.AddScoped<IReplaysService, Services.ReplaysService>();

            builder.Services.AddKeyedScoped<IPlayerService, db8services.PlayerService>("local");
            builder.Services.AddKeyedScoped<IPlayerService, apiServices.PlayerService>("remote");
            builder.Services.AddScoped<IPlayerService, Services.PlayerService>();

            builder.Services.AddKeyedScoped<IBuildService, db8services.BuildService>("local");
            builder.Services.AddKeyedScoped<IBuildService, apiServices.BuildService>("remote");
            builder.Services.AddScoped<IBuildService, Services.BuildService>();


            var app = builder.Build();

            var configService = app.Services.GetRequiredService<ConfigService>();

            var culture = configService.AppOptions.Culture;

            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
            context.Database.Migrate();

            var cultureInfo = new CultureInfo(culture);
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            //var supportedCultures = configService.SupportedCultures.Select(s => s.TwoLetterISOLanguageName).ToArray();
            //var localizationOptions = new RequestLocalizationOptions()
            //    .SetDefaultCulture(supportedCultures[0])
            //    .AddSupportedCultures(supportedCultures)
            //    .AddSupportedUICultures(supportedCultures);
            
            //app.UseRequestLocalization(localizationOptions);

            return app;
        }
    }
}
