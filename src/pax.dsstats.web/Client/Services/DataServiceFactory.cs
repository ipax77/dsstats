using pax.dsstats.shared;

namespace pax.dsstats.web.Client.Services;

public class DataServiceFactory : IDataServiceFactory
{
    private readonly IDataService dataService;

    public DataServiceFactory(IDataService dataService)
    {
        this.dataService = dataService;
    }

    public IDataService GetInstance(ConnectionType connectionType)
    {
        return dataService;
    }
}
