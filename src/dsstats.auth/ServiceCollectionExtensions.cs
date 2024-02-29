using dsstats.auth.Services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace dsstats.auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDsstatsAuth(this IServiceCollection services,
        Action<DsstatsAuthOptions> setupAction = default!)
    {
        if (setupAction != null)
        {
            services.Configure(setupAction);
        }

        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<DsstatsAuthOptions>>().Value;

        services.AddOptions<EMailOptions>()
            .Configure(x =>
            {
                x.Email = authOptions.Email;
                x.Smtp = authOptions.Smtp;
                x.Port = authOptions.Port;
                x.Password = authOptions.Password;
            });

        services.AddDbContext<DsAuthContext>(options =>
        {
            options.UseMySql(authOptions.AuthConnectionString, authOptions.MySqlServerVersion, p =>
            {
                p.CommandTimeout(30);
                p.EnableRetryOnFailure();
                p.MigrationsAssembly("dsstats.auth");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
            .AddBearerToken(options =>
            {
                options.Validate();
            });
        services.AddAuthorizationBuilder();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(DsPolicy.TourneyManager,
                policy => policy.RequireRole(["Admin", "Tourney"]));
            options.AddPolicy(DsPolicy.Admin,
                policy => policy.RequireRole("Admin"));
        });

        //services.AddIdentityCore<DsUser>(options =>
        //    {
        //        options.User.RequireUniqueEmail = true;
        //        options.SignIn.RequireConfirmedEmail = true;
        //    })
        //    .AddRoles<IdentityRole>()
        //    // .AddClaimsPrincipalFactory<DsClaimsFactory>()
        //    .AddEntityFrameworkStores<DsAuthContext>()
        //    .AddApiEndpoints();

        services
            .AddIdentityApiEndpoints<DsUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                // options.Stores.ProtectPersonalData = true; 
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<DsAuthContext>();

        services.AddScoped<UserRepository>();
        services.AddTransient<IEmailSender, EmailSender>();

        return services;
    }
}

public record DsstatsAuthOptions
{
    public string AuthConnectionString { get; set; } = string.Empty;
    public MySqlServerVersion MySqlServerVersion { get; set; } =
        new MySqlServerVersion(new Version(5, 7, 44));
    public string Email { get; set; } = string.Empty;
    public string Smtp { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Password { get; set; } = string.Empty;
    public Dictionary<string, List<string>> UserRoles { get; set; } = [];
}