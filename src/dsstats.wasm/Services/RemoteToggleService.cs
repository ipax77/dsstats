using dsstats.shared.Interfaces;

namespace dsstats.wasm.Services;

public class RemoteToggleService : IRemoteToggleService
{
    public bool FromServer => true;

    public bool IsMaui => false;

#pragma warning disable CS0414 // dummy for server side, only useful for maui
    public event EventHandler? FromServerChanged = null;
#pragma warning restore CS0414

    public void SetFromServer(bool fromServer)
    {

    }
}
