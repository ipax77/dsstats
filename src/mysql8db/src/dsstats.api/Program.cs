
using AutoMapper;
using dsstats.db;
using dsstats.db.Services.Players;
using dsstats.db.Services.Replays;
using dsstats.db8;
using dsstats.shared8;
using dsstats.shared8.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dsstats.api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);

            var config = builder.Configuration.GetSection("ServerConfig");
            var connectionString = config.GetValue<string>("Dsstats8ConnectionString");
            var oldConnectionString = config.GetValue<string>("DsstatsConnectionString");
            var importConnectionString = config.GetValue<string>("Import8ConnectionString");
            var mySqlImportDir = config.GetValue<string>("MySqlImportDir");
            ArgumentNullException.ThrowIfNull(mySqlImportDir);

            builder.Services.AddLogging(l =>
                {
                    l.AddSimpleConsole(o => o.TimestampFormat = "yyyy-MM-dd HH:mm:ss: ");
                });

            // Add services to the container.

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: "dsstatsOrigin",
                                  policy =>
                                  {
                                      policy.WithOrigins("https://dsstats.pax77.org",
                                                         "https://dsstats-dev.pax77.org",
                                                         "http://localhost:5124")
                                      .AllowAnyHeader()
                                      .AllowAnyMethod();
                                  });
            });

            builder.Services.AddDbContext<ReplayContext>(options =>
            {
                options.UseMySql(oldConnectionString, ServerVersion.AutoDetect(oldConnectionString), p =>
                {
                    p.CommandTimeout(30);
                    p.EnableRetryOnFailure();
                    p.MigrationsAssembly("MysqlMigrations");
                    p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                })
                //.EnableDetailedErrors()
                //.EnableSensitiveDataLogging()
                ;
            });

            builder.Services.AddDbContext<DsstatsContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), p =>
                {
                    p.CommandTimeout(600);
                    p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                })
                ;
            });

            builder.Services.AddOptions<DbImportOptions8>()
                .Configure(x =>
                {
                    x.ImportConnectionString = importConnectionString ?? "";
                    x.MySqlImportDir = mySqlImportDir;
                });

            builder.Services.AddHttpClient("sc2arcardeClient")
                .ConfigureHttpClient(options =>
                {
                    options.BaseAddress = new Uri("https://api.sc2arcade.com");
                    options.DefaultRequestHeaders.Add("Accept", "application/json");
                });

            builder.Services.AddHttpClient("decode")
                .ConfigureHttpClient(options =>
                {
                    // options.BaseAddress = new Uri("http://localhost:5240");
                    options.BaseAddress = new Uri(builder.Configuration["ServerConfig:DecodeUrl"] ?? "");
                });

            builder.Services.AddMemoryCache();
            builder.Services.AddAutoMapper(typeof(DsstatsAutoMapperProfile));

            builder.Services.AddScoped<IPlayerService, PlayerService>();
            builder.Services.AddScoped<IReplaysService, ReplaysService>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            using var scope = app.Services.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            mapper.ConfigurationProvider.AssertConfigurationIsValid();

            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            context.Database.Migrate();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("dsstatsOrigin");

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
