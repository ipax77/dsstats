
using dsstats.shared.Interfaces;

namespace dsstats.maui8.Services;

public class RemoteToggleService : IRemoteToggleService
{
    private bool _fromServer;

    public bool FromServer => _fromServer;
    public bool IsMaui => true;

    public event EventHandler? FromServerChanged;

    protected virtual void OnFromServerChanged(EventArgs e)
    {
        FromServerChanged?.Invoke(this, e);
    }

    public void SetFromServer(bool fromServer)
    {
        _fromServer = fromServer;
        OnFromServerChanged(new());
    }
}
