using Blazored.Toast;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.BlazorChartJs;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using sc2dsstats.maui.Services;

namespace sc2dsstats.maui;

public static class MauiProgram
{
    public static readonly string DbName = "dsstats3.db";
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        builder.Services.AddDbContext<ReplayContext>(options => options
            .UseSqlite($"Data Source={Path.Combine(FileSystem.Current.AppDataDirectory, DbName)}", sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("SqliteMigrations");
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
        //.EnableDetailedErrors()
        //.EnableSensitiveDataLogging()
        );

        // builder.Services.AddSingleton<HttpClient>();

        builder.Services.AddMemoryCache();
        builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
        builder.Services.AddBlazoredToast();
        builder.Services.AddChartJs();

        builder.Services.AddTransient<IStatsService, StatsService>();
        
        builder.Services.AddSingleton<UserSettingsService>();
        builder.Services.AddSingleton<DecodeService>();
        builder.Services.AddSingleton<UploadService>();

        builder.Services.AddScoped<MmrService>();

        builder.Services.AddTransient<IReplayRepository, ReplayRepository>();
        builder.Services.AddTransient<IStatsRepository, StatsRepository>();
        builder.Services.AddTransient<BuildService>();
        builder.Services.AddTransient<IDataService, DataService>();

        // init services
        using var scope = builder.Services.BuildServiceProvider().CreateScope();

        var userSettingsService = scope.ServiceProvider.GetRequiredService<UserSettingsService>();

        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.Migrate();

        // ??? when to do
        //var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
        //statsService.SeedPlayerInfos().GetAwaiter().GetResult();

        // DEBUG
        //var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();
        //uploadService.ProduceTestData();

        //var mmrService = scope.ServiceProvider.GetRequiredService<MmrService>();
        //mmrService.ReCalculateWithTimes(DateTime.MinValue).Wait();

        return builder.Build();
    }
}
