
using dsstats.authclient.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace dsstats.authclient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDsstatsAuthClient(this IServiceCollection services,
        Action<DsstatsAuthClientOptions> setupAction = default!)
    {
        if (setupAction != null)
        {
            services.Configure(setupAction);
        }

        services.AddScoped<AuthenticationStateProvider, ExternalAuthStateProvider>();
        services.AddCascadingAuthenticationState();
        services.AddAuthorizationCore();
        services.AddTransient<IAuthService, AuthService>();
        services.AddHttpClient("AuthAPI")
        .ConfigureHttpClient((serviceProvider, options) =>
        {
            var authOptions = serviceProvider.GetRequiredService<IOptions<DsstatsAuthClientOptions>>().Value;
            options.BaseAddress = authOptions.ApiBaseUri;
            options.DefaultRequestHeaders.Accept.Clear();
            options.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });


        return services;
    }
}

public record DsstatsAuthClientOptions
{
    public Uri ApiBaseUri { get; set; } = new Uri("http://localhost:5116");
    public string AccountEndpoint { get; set; } = "account";
    public string ProfileEndpoint { get; set; } = "profile";
}
