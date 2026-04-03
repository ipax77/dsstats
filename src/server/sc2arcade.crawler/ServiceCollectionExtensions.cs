using Microsoft.Extensions.DependencyInjection;

namespace sc2arcade.crawler;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSC2ArcadeCrawler(this IServiceCollection services)
    {
        services.AddHttpClient("sc2arcardeClient")
            .ConfigureHttpClient(options =>
            {
                options.BaseAddress = new Uri("https://sc2arcade.com/api/");
                options.DefaultRequestHeaders.Add("Accept", "application/json");
                options.DefaultRequestHeaders.Add("User-Agent", "dsstats-crawler/1.0");
            });

        services.AddScoped<ICrawlerService, CrawlerService>();
        return services;
    }
}
