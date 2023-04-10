
using dsstats.ratings.api.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System.Diagnostics;
using System.Threading.RateLimiting;

namespace dsstats.ratings.api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 41));
        // var connectionString = builder.Configuration["ServerConfig:TestConnectionString"];
        // var importConnectionString = builder.Configuration["ServerConfig:ImportTestConnectionString"] ?? "";
        var connectionString = builder.Configuration["ServerConfig:DsstatsConnectionString"];
        var importConnectionString = builder.Configuration["ServerConfig:ImportConnectionString"] ?? "";

        builder.Services.AddOptions<DbImportOptions>()
            .Configure(x => x.ImportConnectionString = importConnectionString);

        builder.Services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(500);
                p.EnableRetryOnFailure();
                p.MigrationsAssembly("MysqlMigrations");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
            //.EnableDetailedErrors()
            //.EnableSensitiveDataLogging()
            ;
        });

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddScoped<AuthenticationFilterAttribute>();

        builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
        builder.Services.AddSingleton<RatingsService>();
        builder.Services.AddSingleton<ArcadeRatingsService>();

        var app = builder.Build();

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        // context.Database.EnsureCreated();
        // context.Database.Migrate();

        var arcadeRatingsService = scope.ServiceProvider.GetRequiredService<ArcadeRatingsService>();
        arcadeRatingsService.ProduceRatings().Wait();

        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
        // ratingsService.ProduceRatings(true).Wait();
        ratingsService.GetArcadeInjectDic().Wait();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            //var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

            //Stopwatch sw = Stopwatch.StartNew();
            //ratingsService.ProduceRatings().Wait();

            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.UseRateLimiter(new RateLimiterOptions()
            .AddConcurrencyLimiter(policyName: "default", options =>
            {
                options.PermitLimit = 2;
                options.QueueLimit = 2;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            })
            .AddTokenBucketLimiter(policyName: "tokenDefault", options =>
            {
                options.TokenLimit = 4;
                options.QueueLimit = 1;
                options.TokensPerPeriod = 1;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.ReplenishmentPeriod = TimeSpan.FromSeconds(20);
                options.AutoReplenishment = true;
            })
        );

        app.MapControllers().RequireRateLimiting("tokenDefault");

        app.Run();

    }
}
