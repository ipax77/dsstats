﻿using Microsoft.Extensions.Logging;
using Windows.Services.Store;

namespace dsstats.maui8.Services;

public class StoreUpdateService(ILogger<StoreUpdateService> logger) : IUpdateService
{
    private IReadOnlyList<StorePackageUpdate>? availableUpdates = null;
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
            availableUpdates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
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
        if (availableUpdates is null || availableUpdates.Count == 0)
        {
            return true;
        }

        try
        {
            var storeContext = StoreContext.GetDefault();
            var progress = new Progress<StorePackageUpdateStatus>(progress => 
                OnUpdateProgress(new() { Progress = Convert.ToInt32(progress.PackageDownloadProgress * 100.0) }));

            var result = await storeContext.RequestDownloadStorePackageUpdatesAsync(availableUpdates)
                .AsTask(progress);

            if (result.OverallState != StorePackageUpdateState.Completed)
            {
                var failedUpdates = result.StorePackageUpdateStatuses.Where(
                    status => status.PackageUpdateState != StorePackageUpdateState.Completed)
                .ToList();

                if (failedUpdates.Count > 0)
                {
                    logger.LogError("app update failed: {errorlist}", string.Join(", ", failedUpdates.Select(s => s.PackageFamilyName)));
                }
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("app update failed: {error}", ex.Message);
        }
        return false;
    }
}
