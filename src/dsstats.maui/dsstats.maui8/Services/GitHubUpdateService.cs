using Microsoft.Extensions.Logging;
using Windows.Management.Deployment;

namespace dsstats.maui8.Services;

public class GitHubUpdateService(ILogger<GitHubUpdateService> logger) : IUpdateService
{
    private static readonly string packageUri = "https://github.com/ipax77/dsstats/releases/latest/download/";
    public static readonly Version CurrentVersion = new Version(0, 5, 0);
    private Version latestVersion = new Version(0, 0, 0);
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
        HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(packageUri);

        try
        {
            var stream = await httpClient.GetStreamAsync("latest.yml");

            StreamReader reader = new StreamReader(stream);
            string versionInfo = await reader.ReadLineAsync() ?? "";


            if (Version.TryParse(versionInfo.Split(' ').LastOrDefault(), out Version? newVersion)
                && newVersion is not null)
            {
                latestVersion = newVersion;
                return latestVersion > CurrentVersion;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("latest version check failed: {error}", ex.Message);
        }
        return false;
    }

    public async Task<bool> UpdateApp()
    {
        if (latestVersion <= CurrentVersion)
        {
            return true;
        }

        try
        {
            PackageManager packageManager = new();
            var progress = new Progress<DeploymentProgress>(report => 
                OnUpdateProgress(new() { Progress = (int)report.percentage }));

            await packageManager.AddPackageAsync
            (
                new Uri($"{packageUri}sc2dsstats.maui_{latestVersion}_x64.msix"),
                null,
                DeploymentOptions.ForceApplicationShutdown
            )
            .AsTask(progress);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("app update failed: {error}", ex.Message);
        }
        return false;
    }
}


public class UpdateProgressEvent : EventArgs
{
    public int Progress { get; init; }
}