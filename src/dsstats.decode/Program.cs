using System.Net.Http.Headers;
using dsstats.decode;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.Configure<DecodeSettings>(builder.Configuration.GetSection("DecodeSettings"));
builder.Services.AddSingleton<DecodeService>();

builder.Services.AddHttpClient("callback")
    .ConfigureHttpClient(options =>
    {
        options.BaseAddress = new Uri(builder.Configuration["DecodeSettings:CallbackUrl"] ?? "");
        options.DefaultRequestHeaders.Add("Accept", "application/json");
        options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DS8upload77");
    });

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
