namespace dsstats.service.Services;

public partial class DsstatsService(IServiceScopeFactory scopeFactory)
{
    public static readonly string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dsstats.worker");

    private static readonly string configFile = Path.Combine(appFolder, "workerconfig2.json");
}
