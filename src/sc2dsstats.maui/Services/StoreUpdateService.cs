
using Windows.Foundation;
using Windows.Services.Store;

namespace sc2dsstats.maui.Services;

public static class StoreUpdateService
{
    private static StoreContext? context = null;

    public static EventHandler<UpdateProgressEvent>? UpdateProgress;
    public static void OnUpdateProgress(UpdateProgressEvent e)
    {
        EventHandler<UpdateProgressEvent>? handler = UpdateProgress;
        handler?.Invoke(typeof(UpdateService), e);
    }

    public static async Task CheckForUpdates(bool init)
    {
        if (context == null)
        {
            context = StoreContext.GetDefault();
        }

        // Get the updates that are available.
        IReadOnlyList<StorePackageUpdate> updates =
            await context.GetAppAndOptionalStorePackageUpdatesAsync();

        if (updates.Count > 0)
        {
            if (Application.Current != null && Application.Current.MainPage != null)
            {
                bool answer = await Application.Current.MainPage.DisplayAlert("New Version Available!", "Would you like to update now?", "Yes", "No");
                if (answer)
                {
                    //await DownloadAndInstallAllUpdatesAsync(updates);
                    await UpdateApp(updates);
                }
            }
        }
        else
        {
            if (!init && Application.Current != null && Application.Current.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Update Result", "Current Version is up to date.", "Ok");
            }
        }
    }

    public static async Task<bool> UpdateApp(IReadOnlyList<StorePackageUpdate> updates)
    {
        if (context == null)
        {
            context = StoreContext.GetDefault();
        }

        if (updates.Count > 0)
        {
            IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> downloadOperation =
                context.TrySilentDownloadAndInstallStorePackageUpdatesAsync(updates);

            downloadOperation.Progress = (asyncInfo, progress) =>
            {
                OnUpdateProgress(new() { Progress = (uint)(progress.PackageDownloadProgress * 100.0) });
            };

            StorePackageUpdateResult result = await downloadOperation.AsTask();

            if (result.OverallState == StorePackageUpdateState.Completed)
            {
                return true;
            }
        }
        return false;
    }

    // Downloads and installs package updates in separate steps.
    private static async Task DownloadAndInstallAllUpdatesAsync(IReadOnlyList<StorePackageUpdate> updates)
    {
        if (context == null)
        {
            context = StoreContext.GetDefault();
        }

        if (updates.Count != 0)
        {
            // Download the packages.
            bool downloaded = await DownloadPackageUpdatesAsync(updates);

            if (downloaded)
            {
                // Install the packages.
                await InstallPackageUpdatesAsync(updates);
            }
        }
    }

    // Helper method for downloading package updates.
    private static async Task<bool> DownloadPackageUpdatesAsync(IEnumerable<StorePackageUpdate> updates)
    {
        bool downloadedSuccessfully = false;

        if (context == null)
        {
            return false;
        }

        IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> downloadOperation =
            context.RequestDownloadStorePackageUpdatesAsync(updates);


        // The Progress async method is called one time for each step in the download process for each
        // package in this request.
        downloadOperation.Progress = (asyncInfo, progress) =>
        {
            OnUpdateProgress(new() { Progress = (uint)(progress.PackageDownloadProgress * 100.0) });
        };

        StorePackageUpdateResult result = await downloadOperation.AsTask();

        switch (result.OverallState)
        {
            case StorePackageUpdateState.Completed:
                downloadedSuccessfully = true;
                break;
            default:
                // Get the failed updates.
                var failedUpdates = result.StorePackageUpdateStatuses.Where(
                    status => status.PackageUpdateState != StorePackageUpdateState.Completed);

                // See if any failed updates were mandatory
                if (updates.Any(u => u.Mandatory && failedUpdates.Any(
                    failed => failed.PackageFamilyName == u.Package.Id.FamilyName)))
                {
                    // At least one of the updates is mandatory. Perform whatever actions you
                    // want to take for your app: for example, notify the user and disable
                    // features in your app.
                    HandleMandatoryPackageError();
                }
                break;
        }

        return downloadedSuccessfully;
    }

    // Helper method for installing package updates.
    private static async Task InstallPackageUpdatesAsync(IEnumerable<StorePackageUpdate> updates)
    {
        if (context == null)
        {
            return;
        }

        IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> installOperation =
            context.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);

        // The package updates were already downloaded separately, so this method skips the download
        // operatation and only installs the updates; no download progress notifications are provided.
        StorePackageUpdateResult result = await installOperation.AsTask();

        switch (result.OverallState)
        {
            case StorePackageUpdateState.Completed:
                break;
            default:
                // Get the failed updates.
                var failedUpdates = result.StorePackageUpdateStatuses.Where(
                    status => status.PackageUpdateState != StorePackageUpdateState.Completed);

                // See if any failed updates were mandatory
                if (updates.Any(u => u.Mandatory && failedUpdates.Any(failed => failed.PackageFamilyName == u.Package.Id.FamilyName)))
                {
                    // At least one of the updates is mandatory, so tell the user.
                    HandleMandatoryPackageError();
                }
                break;
        }
    }

    // Helper method for handling the scenario where a mandatory package update fails to
    // download or install. Add code to this method to perform whatever actions you want
    // to take, such as notifying the user and disabling features in your app.
    private static void HandleMandatoryPackageError()
    {
    }
}
