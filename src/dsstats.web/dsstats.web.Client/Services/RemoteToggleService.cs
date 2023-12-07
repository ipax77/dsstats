using dsstats.shared.Interfaces;

namespace dsstats.web.Client.Services;

public class RemoteToggleService : IRemoteToggleService
{
    private string _culture = "en";
    public bool FromServer => true;

    public bool IsMaui => false;

    public string Culture => _culture;

    public event EventHandler? FromServerChanged = null;
    public event EventHandler? CultureChanged;

    protected void OnCultureChanged()
    {
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetCulture(string culture)
    {
        _culture = culture;
        OnCultureChanged();
    }

    public void SetFromServer(bool fromServer)
    {

    }
}