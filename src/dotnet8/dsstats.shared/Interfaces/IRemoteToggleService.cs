namespace dsstats.shared.Interfaces;

public interface IRemoteToggleService
{
    event EventHandler? FromServerChanged;
    void SetFromServer(bool fromServer);
    bool FromServer { get; }
    bool IsMaui { get; }
}
