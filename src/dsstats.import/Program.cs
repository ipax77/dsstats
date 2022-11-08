using AutoMapper;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((context, config) =>
{
    config.AddJsonFile("/data/localserverconfig.json", optional: false, reloadOnChange: false);
});

// Add services to the container.

var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 39));
var connectionString = builder.Configuration["ServerConfig:TestConnectionString"];

builder.Services.AddDbContext<ReplayContext>(options =>
{
    options.UseMySql(connectionString, serverVersion, p =>
    {
        p.CommandTimeout(120);
        p.EnableRetryOnFailure();
        p.MigrationsAssembly("MysqlMigrations");
        p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    })
     .EnableDetailedErrors()
     .EnableSensitiveDataLogging()
    ;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddScoped<ImportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
    mapper.ConfigurationProvider.AssertConfigurationIsValid();

    using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
    //context.Database.EnsureDeleted();
    context.Database.Migrate();

    var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
    //importService.DEBUGSeedUploaders().GetAwaiter().GetResult();
    var result = importService.ImportReplayBlobs().GetAwaiter().GetResult();

    Console.WriteLine(result);

    var uploader = context.Uploaders
        .Include(i => i.Replays)
        .AsNoTracking()
        .FirstOrDefault(f => f.UploaderId == 115);

    var count = uploader?.Replays.Count;
    Console.WriteLine(count);

}

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
