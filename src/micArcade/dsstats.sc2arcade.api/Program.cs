
using dsstats.sc2arcade.api.Services;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;

namespace dsstats.sc2arcade.api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);

            var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 41));
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

            builder.Services.AddHttpClient("sc2arcardeClient")
                .ConfigureHttpClient(options =>
                {
                    options.BaseAddress = new Uri("https://api.sc2arcade.com");
                    options.DefaultRequestHeaders.Add("Accept", "application/json");
                });

            builder.Services.AddScoped<CrawlerService>();

            var app = builder.Build();

            using var scope = app.Services.CreateScope();
            // var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
            // context.Database.EnsureCreated();
            // context.Database.Migrate();

            var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
            crawlerService.GetLobbyHistory(DateTime.Today.AddMonth(-1)).Wait();
            // crawlerService.GetLobbyHistory(new DateTime(2021, 2, 1)).Wait();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                // crawlerService.GetLobbyHistory(new DateTime(2021, 2, 1)).Wait();
                // crawlerService.AnalyizeLobbyHistory("/data/ds/sc2arcardeLobbyResults.json");
                // crawlerService.DEBUGJson("/data/ds/temp.json");

                // crawlerService.CheckPlayers().Wait();
                // crawlerService.CheckReplays().Wait();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}