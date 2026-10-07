using Microsoft.Extensions.DependencyInjection;

namespace sc2arcade.crawler;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSC2ArcadeCrawler(this IServiceCollection services)
    {
        services.AddHttpClient("sc2arcardeClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
            .ConfigureHttpClient(options =>
            {
                options.BaseAddress = new Uri("https://sc2arcade.com/api/");
                options.Timeout = TimeSpan.FromSeconds(60);
                options.DefaultRequestHeaders.Add("Accept", "application/json");
                options.DefaultRequestHeaders.Add("User-Agent", "dsstats-crawler/1.0");
            });

        services.AddSingleton<Sc2ArcadeRequestGate>();
        services.AddScoped<ICrawlerService, CrawlerService>();
        return services;
    }
}
