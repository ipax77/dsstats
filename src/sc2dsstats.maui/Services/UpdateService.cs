﻿using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace sc2dsstats.maui.Services;

internal static class UpdateService
{
    private static readonly string packageUri = "https://github.com/ipax77/dsstats/releases/latest/download/";
    public static Version NewVersion { get; private set; } = new Version(0, 2, 8, 0);
    public static Version CurrentVersion { get; private set; } = new Version(0, 2, 8, 0);

    public static EventHandler<UpdateProgressEvent>? UpdateProgress;

    public static void OnUpdateProgress(UpdateProgressEvent e)
    {
        EventHandler<UpdateProgressEvent>? handler = UpdateProgress;
        handler?.Invoke(typeof(UpdateService), e);
    }

    public static async Task CheckUpdate(bool init = false)
    {
        await SetNewVersion();
        try
        {
            Package package = Package.Current;
            PackageVersion packageVersion = package.Id.Version;
            CurrentVersion = new Version(string.Format("{0}.{1}.{2}.{3}", packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision));


            //compare package versions
            if (NewVersion.CompareTo(CurrentVersion) > 0)
            {
                if (Application.Current != null && Application.Current.MainPage != null)
                {
                    bool answer = await Application.Current.MainPage.DisplayAlert("New Version Available!", "Would you like to update now?", "Yes", "No");
                    if (answer)
                    {
                        Update();
                    }
                }
            }
            else
            {
                if (!init && Application.Current != null && Application.Current.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Update Reuslt", "Current Version is up to date.", "Ok");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"failed doing UI things: {ex.Message}");
            if (!init && Application.Current != null && Application.Current.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Update Failed", ex.Message, "Ok");
            }
        }
    }

    private static async Task SetNewVersion()
    {
        HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(packageUri);

        try
        {
            var stream = await httpClient.GetStreamAsync("latest.yml");

            StreamReader reader = new StreamReader(stream);
            string versionInfo = await reader.ReadLineAsync() ?? "";


            if (Version.TryParse(versionInfo.Split(' ').LastOrDefault(), out Version? newVersion))
            {
                if (newVersion != null)
                {
                    NewVersion = newVersion;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"failed getting latest version: {ex.Message}");
        }
    }

    private static async void Update()
    {
        try
        {
            PackageManager packagemanager = new PackageManager();

            var progress = new Progress<DeploymentProgress>(report => OnUpdateProgress(new() { Progress = report.percentage }));

            var updateTask = packagemanager.AddPackageAsync(
                new Uri($"{packageUri}sc2dsstats.maui_{NewVersion}_x64.msix"),
                null,
                DeploymentOptions.ForceApplicationShutdown
            ).AsTask(progress);
            await updateTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"failed updating: {ex.Message}");
            if (Application.Current != null && Application.Current.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Update Failed", ex.Message, "Ok");
            }
        }
    }
}

internal class UpdateProgressEvent : EventArgs
{
    public uint Progress { get; init; }
}
