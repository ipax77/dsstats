
using dsstats.shared.Interfaces;

namespace dsstats.maui8.Services;

public class RemoteToggleService : IRemoteToggleService
{
    private bool _fromServer;
    private string _culture = "en";

    public bool FromServer => _fromServer;
    public bool IsMaui => true;
    public string Culture => _culture;

    public event EventHandler? FromServerChanged;
    public event EventHandler? CultureChanged;

    protected virtual void OnFromServerChanged(EventArgs e)
    {
        FromServerChanged?.Invoke(this, e);
    }

    protected virtual void OnCultureChanged(EventArgs e)
    {
        CultureChanged?.Invoke(this, e);
    }

    public void SetFromServer(bool fromServer)
    {
        _fromServer = fromServer;
        OnFromServerChanged(new());
    }

    public void SetCulture(string culture)
    {
        _culture = culture;
        OnCultureChanged(new());
    }
}
