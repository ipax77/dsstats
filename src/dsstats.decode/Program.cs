using dsstats.decode;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.Configure<DecodeSettings>(builder.Configuration.GetSection("DecodeSettings"));
builder.Services.AddSingleton<DecodeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
