using Microsoft.Extensions.DependencyInjection;

namespace sc2arcade.crawler;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSC2ArcadeCrawler(this IServiceCollection services)
    {
        services.AddHttpClient("sc2arcardeClient")
            .ConfigureHttpClient(options =>
            {
                options.BaseAddress = new Uri("https://api.sc2arcade.com");
                options.DefaultRequestHeaders.Add("Accept", "application/json");
            });

        services.AddScoped<ICrawlerService, CrawlerService>();
        return services;
    }
}
