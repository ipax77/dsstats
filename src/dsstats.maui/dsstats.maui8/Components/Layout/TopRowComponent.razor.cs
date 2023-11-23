using Blazored.Toast.Services;
using dsstats.maui8.Services;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System.Collections.Concurrent;

namespace dsstats.maui8.Components.Layout;

public partial class TopRowComponent : ComponentBase, IDisposable
{
    [Inject]
    public DsstatsService dsstatsService { get; set; } = default!;
    [Inject]
    public IRemoteToggleService remoteToggleService { get; set; } = default!;
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    public ConfigService configService { get; set; } = default!;
    [Inject]
    public IToastService toastService { get; set; } = default!;
    [Inject]
    public IUpdateService updateService { get; set; } = default!;

    string currentLocation = "Home";
    DecodeInfoEventArgs? decodeInfo = null;
    ConcurrentBag<DecodeError> decodeErrors = new();
    List<DecodeError> decodeErrorsList = new();
    DecodeErrorModal? decodeErrorModal;
    UploadAskModal? uploadAskModal;
    int updateDownloadProgress = 0;

    protected override void OnInitialized()
    {
        dsstatsService.ScanStateChanged += DssstatsService_ScanStateChanged;
        dsstatsService.DecodeStateChanged += DssstatsService_DecodeStateChanged;
        NavigationManager.LocationChanged += NavigationManager_LocationChanged;

        // DEBUG
        //decodeErrors.Add(new()
        //{
        //    ReplayPath = "TestPath",
        //    Error = "TestError"
        //});

        base.OnInitialized();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _ = InitScan();
            _ = CheckForUpdates();
        }
        base.OnAfterRender(firstRender);
    }

    private void DssstatsService_DecodeStateChanged(object? sender, DecodeInfoEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Info))
        {
            toastService.ShowInfo(e.Info);
        }
        if (e.DecodeError is not null)
        {
            decodeErrors.Add(e.DecodeError);
        }
        if (e.Finished)
        {
            uploadAskModal?.Show();
        }
        decodeInfo = e;
        InvokeAsync(() => StateHasChanged());
    }

    private async Task InitScan()
    {
        await Task.Delay(1000);
        await dsstatsService.ScanForNewReplays();
    }

    private void DssstatsService_ScanStateChanged(object? sender, ScanEventArgs e)
    {
        InvokeAsync(() => StateHasChanged());
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentLocation = e.Location.Replace("https://0.0.0.0/", "");
        InvokeAsync(() => StateHasChanged());
    }

    private void ShowErrors()
    {
        decodeErrorsList = decodeErrors.ToList();
        decodeErrorModal?.Show();
    }

    public void Dispose()
    {
        dsstatsService.ScanStateChanged -= DssstatsService_ScanStateChanged;
        dsstatsService.DecodeStateChanged -= DssstatsService_DecodeStateChanged;
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
    }

    public async Task CheckForUpdates(bool init = false)
    {
        if (init)
        {
            if (!configService.AppOptions.CheckForUpdates)
            {
                return;
            }
            await Task.Delay(5000);
        }

        var updateAvailable = await updateService.CheckForUpdates();

        if (updateAvailable)
        {
            if (Application.Current != null && Application.Current.MainPage != null)
            {
                bool answer = await Application.Current.MainPage.DisplayAlert("New Version Available!", "Would you like to update now?", "Yes", "No");
                if (answer)
                {
                    updateService.UpdateProgress += UpdateService_UpdateProgress;
                    var updateResult = await updateService.UpdateApp();
                    if (!updateResult)
                    {
                        updateService.UpdateProgress -= UpdateService_UpdateProgress;
                        await Application.Current.MainPage.DisplayPromptAsync("Update Failed", ":(");
                    }
                }
            }
        }
        else
        {
            toastService.ShowInfo("Your version is up to date.");
        }
    }

    private void UpdateService_UpdateProgress(object? sender, UpdateProgressEvent e)
    {
        updateDownloadProgress = e.Progress;
        InvokeAsync(() => StateHasChanged());
    }

    public record DecodeState
    {
        public int DoneDecoding {  get; set; }
        public TimeSpan Eta { get; set; }
        public TimeSpan Elapsed { get; set; }
        public int Per { get; set; }
        public bool Finished {  get; set; }
    }
}