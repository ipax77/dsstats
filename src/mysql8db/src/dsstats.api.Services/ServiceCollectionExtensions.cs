using dsstats.shared8.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.api.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, bool isProduction)
    {
        services.AddHttpClient("dsstats8", client =>
        {
            client.BaseAddress = new Uri(isProduction
                ? "https://dsstats.pax77.org/"
                : "http://localhost:5289");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddScoped<IPlayerService, PlayerService>();
        
        return services;
    }
}
