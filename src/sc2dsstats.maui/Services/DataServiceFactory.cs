using pax.dsstats.shared;

namespace sc2dsstats.maui.Services;

public class DataServiceFactory : IDataServiceFactory
{
    private readonly IEnumerable<IDataService> dataServices;

    public DataServiceFactory(IEnumerable<IDataService> dataService)
    {
        this.dataServices = dataService;
    }

    public IDataService GetInstance(ConnectionType connectionType)
    {
        return connectionType switch
        {
            ConnectionType.Maui => this.GetService(typeof(DataService)),
            ConnectionType.Server => this.GetService(typeof(ServerDataService)),
            _ => this.GetService(typeof(DataService)),
        }; ;
    }

    public IDataService GetService(Type type)
    {
        return this.dataServices.FirstOrDefault(x => x.GetType() == type)!;
    }
}

