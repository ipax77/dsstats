using pax.dsstats.shared;

namespace sc2dsstats.maui.Services;

public class FromServerSwitchService : IFromServerSwitchService
{
    private static bool _fromServer;
    public void SetFromServer(bool fromServer)
    {
        _fromServer = fromServer;
    }

    public bool GetFromServer()
    {
        return _fromServer;
    }
}
