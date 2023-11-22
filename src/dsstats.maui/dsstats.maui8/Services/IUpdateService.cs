
namespace dsstats.maui8.Services;

public interface IUpdateService
{
    event EventHandler<UpdateProgressEvent>? UpdateProgress;
    Task<bool> CheckForUpdates(bool init = false);
    Task<bool> UpdateApp();
}