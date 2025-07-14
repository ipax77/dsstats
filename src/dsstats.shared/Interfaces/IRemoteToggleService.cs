using dsstats.shared.DsFen;

namespace dsstats.shared.Interfaces;

public interface IRemoteToggleService
{
    event EventHandler? FromServerChanged;
    event EventHandler? CultureChanged;
    void SetFromServer(bool fromServer);
    void SetCulture(string culture);
    bool FromServer { get; }
    bool IsMaui { get; }
    string Culture { get; }
    void Build(DsBuildRequest buildRequest);
}
