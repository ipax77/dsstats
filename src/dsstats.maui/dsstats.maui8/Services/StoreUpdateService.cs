using Microsoft.Extensions.Logging;
using Windows.Services.Store;

namespace dsstats.maui8.Services;

public class StoreUpdateService(ILogger<StoreUpdateService> logger) : IUpdateService
{
    private readonly object lockobject = new();

    public EventHandler<UpdateProgressEvent>? UpdateProgress;
    event EventHandler<UpdateProgressEvent>? IUpdateService.UpdateProgress
    {
        add
        {
            lock (lockobject)
            {
                UpdateProgress += value;
            }
        }

        remove
        {
            lock (lockobject)
            {
                UpdateProgress -= value;
            }
        }
    }

    private void OnUpdateProgress(UpdateProgressEvent e)
    {
        var handler = UpdateProgress;
        handler?.Invoke(this, e);
    }

    public async Task<bool> CheckForUpdates(bool init = false)
    {
        try
        {
            var storeContext = StoreContext.GetDefault();
            var availableUpdates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
            ArgumentNullException.ThrowIfNull(availableUpdates);

            return availableUpdates.Count > 0;
        }
        catch (Exception ex)
        {
            logger.LogError("latest version check failed: {error}", ex.Message);
        }
        return false;
    }

    public async Task<bool> UpdateApp()
    {
        try
        {
            var result = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var storeContext = StoreContext.GetDefault();
                var availableUpdates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();

                if (availableUpdates is null || availableUpdates.Count == 0)
                {
                    logger.LogWarning("No updates available.");
                    return null;
                }

                var progress = new Progress<StorePackageUpdateStatus>(progress =>
                    OnUpdateProgress(new() { Progress = Convert.ToInt32(progress.PackageDownloadProgress * 100.0) }));

                return await storeContext.RequestDownloadStorePackageUpdatesAsync(availableUpdates)
                    .AsTask(progress);
            });

            if (result == null)
                return false;

            if (result.OverallState != StorePackageUpdateState.Completed)
            {
                var failedUpdates = result.StorePackageUpdateStatuses.Where(
                    status => status.PackageUpdateState != StorePackageUpdateState.Completed)
                    .ToList();

                if (failedUpdates.Count > 0)
                {
                    var errorDetails = failedUpdates.Select(s => new
                    {
                        PackageFamilyName = s.PackageFamilyName,
                        Error = s.PackageUpdateState
                    });
                    logger.LogError("App update failed for packages: {errorDetails}", string.Join(", ", errorDetails));
                }
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("App update failed: {error}", ex);
        }
        return false;
    }

}
