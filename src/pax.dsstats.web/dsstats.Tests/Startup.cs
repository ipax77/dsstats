using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using System.Reflection;

namespace dsstats.Tests;

public class Startup
{
    private static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAutoMapper(typeof(AutoMapperProfile));
    }

    public static string GetTestFilePath(string fileName)
    {
        return Path.Combine(assemblyPath, "testdata", fileName);
    }

    public static string GetCmdFilePath()
    {
        return Path.Combine(assemblyPath, "cmd.bat");
    }
}
