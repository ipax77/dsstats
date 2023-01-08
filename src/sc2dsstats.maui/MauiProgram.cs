using Blazored.Toast;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
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

        var sqliteConnectionString = $"Data Source={Path.Combine(FileSystem.Current.AppDataDirectory, DbName)}";

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
        builder.Services.AddChartJs();

        builder.Services.AddTransient<IStatsService, StatsService>();
        
        builder.Services.AddSingleton<UserSettingsService>();
        builder.Services.AddSingleton<DecodeService>();
        builder.Services.AddSingleton<UploadService>();
        builder.Services.AddSingleton<IFromServerSwitchService, FromServerSwitchService>();

        builder.Services.AddScoped<IRatingRepository, pax.dsstats.dbng.Services.RatingRepository>();
        builder.Services.AddScoped<MmrProduceService>();

        builder.Services.AddTransient<IReplayRepository, ReplayRepository>();
        builder.Services.AddTransient<IStatsRepository, StatsRepository>();
        builder.Services.AddTransient<BuildService>();
        builder.Services.AddTransient<IDataService, DataService>();

        // init services
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.Migrate();

        // DEBUG

        //foreach (var replay in context.Replays.Include(i => i.ReplayPlayers))
        //{
        //    int playerPos = replay.ReplayPlayers.FirstOrDefault(f => f.IsUploader)?.GamePos ?? 0;
        //    if (playerPos > 0)
        //    {
        //        replay.PlayerPos = playerPos;
        //    }
        //}
        //context.SaveChanges();

        //var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();
        //uploadService.ProduceMauiTestData();

        //var replays = context.Replays
        //    .Include(i => i.ReplayPlayers)
        //        .ThenInclude(i => i.Spawns)
        //            .ThenInclude(i => i.Units)
        //    .Include(i => i.ReplayPlayers)
        //        .ThenInclude(i => i.Upgrades)
        //    .OrderByDescending(o => o.GameTime)
        //    .Take(4)
        //    .ToList();

        //context.Replays.RemoveRange(replays);
        //context.SaveChanges();

        //OcrService ocrService = new();
        //ocrService.GetTextFromOcr().Wait();

        // END DEBUG

        var build =  builder.Build();

        Data.IsMaui = true;
        Data.SqliteConnectionString = sqliteConnectionString;
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

            var mmrProduceService = build.Services.GetRequiredService<MmrProduceService>();
            mmrProduceService.ProduceRatings(new(true)).Wait();
        }
        
        return build;
    }
}
