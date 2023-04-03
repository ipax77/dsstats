
using dsstats.sc2arcade.api.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace dsstats.sc2arcade.api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHttpClient("sc2arcardeClient")
                .ConfigureHttpClient(options =>
                {
                    options.BaseAddress = new Uri("https://api.sc2arcade.com");
                    options.DefaultRequestHeaders.Add("Accept", "application/json");
                });

            builder.Services.AddScoped<CrawlerService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                using var scope = app.Services.CreateScope();
                var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
                // crawlerService.GetLobbyHistory().Wait();
                crawlerService.AnalyizeLobbyHistory("/data/ds/sc2arcardeLobbyResults.json");
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}