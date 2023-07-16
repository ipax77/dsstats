using Blazored.Toast;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
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

		builder.Services.AddBlazoredToast();

		builder.Services.AddSingleton<ConfigService>();
        builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);

        builder.Services.AddSingleton<WeatherForecastService>();

		var app = builder.Build();

		var configService = app.Services.GetRequiredService<ConfigService>();


		return app;
	}
}
