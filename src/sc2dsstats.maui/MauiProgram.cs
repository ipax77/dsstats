using Blazored.Toast;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.BlazorChartJs;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng.Services.Ratings;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
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

        var sqliteConnectionString = $"Data Source={Path.Combine(FileSystem.Current.AppDataDirectory, DbName)}";

        builder.Services.AddOptions<DbImportOptions>()
            .Configure(x => x.ImportConnectionString = sqliteConnectionString);

        builder.Services.AddDbContext<ReplayContext>(options => options
            .UseSqlite(sqliteConnectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("SqliteMigrations");
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
        //.EnableDetailedErrors()
        //.EnableSensitiveDataLogging()
        );

        builder.Services.AddMemoryCache();
        builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
        builder.Services.AddBlazoredToast();
        builder.Services.AddChartJs(options =>
        {
            options.ChartJsLocation = "/js/chart.js";
            options.ChartJsPluginDatalabelsLocation = "/js/chartjs-plugin-datalabels.js";
        });

        builder.Services.AddTransient<IStatsService, StatsService>();
        
        builder.Services.AddSingleton<UserSettingsService>();
        builder.Services.AddSingleton<DecodeService>();
        builder.Services.AddSingleton<UploadService>();
        builder.Services.AddSingleton<IFromServerSwitchService, FromServerSwitchService>();

        builder.Services.AddScoped<IRatingRepository, pax.dsstats.dbng.Services.RatingRepository>();
        builder.Services.AddSingleton<RatingsService>();
        builder.Services.AddScoped<PlayerService>();
        builder.Services.AddScoped<IArcadeService, sc2dsstats.maui.Services.ArcadeService>();

        builder.Services.AddTransient<IReplayRepository, ReplayRepository>();
        builder.Services.AddTransient<IStatsRepository, StatsRepository>();
        builder.Services.AddTransient<BuildService>();
        builder.Services.AddTransient<IDataService, DataService>();

        // init services
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.Migrate();

        // DEBUG

        var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();
        uploadService.ProduceMauiTestData();

        // END DEBUG

        var build =  builder.Build();

        Data.IsMaui = true;
        var userSettingsService = build.Services.GetRequiredService<UserSettingsService>();

        if (!context.PlayerRatings.Any())
        {
            foreach (var replay in context.Replays.Include(i => i.ReplayPlayers))
            {
                int playerPos = replay.ReplayPlayers.FirstOrDefault(f => f.IsUploader)?.GamePos ?? 0;
                if (playerPos > 0)
                {
                    replay.PlayerPos = playerPos;
                }
            }
            context.SaveChanges();

            var ratingsService = build.Services.GetRequiredService<RatingsService>();
            ratingsService.ProduceRatings(true).Wait();
        }
        
        return build;
    }
}
