
using dsstats.import.api.Services;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;

namespace dsstats.import.api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);

        var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 41));
        var connectionString = builder.Configuration["ServerConfig:DsstatsConnectionString"];
        var importConnectionString = builder.Configuration["ServerConfig:ImportConnectionString"];

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

        builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
        builder.Services.AddSingleton<ImportService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();

        // init singleton service
        using var scope = app.Services.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
    }
}