using Blazored.Toast;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.shared;
using sc2dsstats.maui.Data;
using sc2dsstats.maui.Services;

namespace sc2dsstats.maui;

public static class MauiProgram
{
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
        // builder.Logging.AddDebug();
#endif

        var sqliteConnectionString = $"Data Source={Path.Combine(FileSystem.Current.AppDataDirectory, "dsstats.db")}";
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

        builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);
		builder.Services.AddSingleton<ConfigService>();
		builder.Services.AddSingleton<DecodeService>();

        builder.Services.AddSingleton<WeatherForecastService>();

        builder.Services.AddScoped<IReplayRepository, ReplayRepository>();

        var app = builder.Build();

		var configService = app.Services.GetRequiredService<ConfigService>();
        var context = app.Services.GetRequiredService<ReplayContext>();
        context.Database.Migrate();

        pax.dsstats.shared.Data.IsMaui = true;

        return app;
	}
}
