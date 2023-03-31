
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
        var connectionString = builder.Configuration["ServerConfig:TestConnectionString"];
        var importConnectionString = builder.Configuration["ServerConfig:ImportTestConnectionString"] ?? "";

        builder.Services.AddOptions<DbImportOptions>()
            .Configure(x => x.ImportConnectionString = importConnectionString);

        builder.Services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(120);
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

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            // using var scope = app.Services.CreateScope();
            // var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

            // Stopwatch sw = Stopwatch.StartNew();
            // ratingsService.ProduceRatings().Wait();
            // sw.Stop();
            // Console.WriteLine($"ratings produced in {sw.ElapsedMilliseconds} ms");

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
