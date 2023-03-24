using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using System.Text.Json;

namespace dsstats.import;
class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("ImportTestConnectionString").GetString();
        var serverVersion = new MySqlServerVersion(new System.Version(5, 0, 41));

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.EnableRetryOnFailure();
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        var serviceProvider = services.BuildServiceProvider();
    }
}
