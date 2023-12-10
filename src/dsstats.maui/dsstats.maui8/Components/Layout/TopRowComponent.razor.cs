using Blazored.Toast.Services;
using dsstats.localization;
using dsstats.maui8.Services;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Globalization;

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
    [Inject]
    public IStringLocalizer<DsstatsLoc> Loc { get; set; } = default!;

    string currentLocation = "Home";
    DecodeInfoEventArgs? decodeInfo = null;
    ConcurrentDictionary<DecodeError, bool> decodeErrors = new();
    List<DecodeError> decodeErrorsList = new();
    DecodeErrorModal? decodeErrorModal;
    UploadAskModal? uploadAskModal;
    int updateDownloadProgress = 0;
    CultureInfo Culture = new CultureInfo("en");
    CultureInfo defaultCulture = new CultureInfo("iv");

    protected override void OnInitialized()
    {
        Culture = new CultureInfo(configService.AppOptions.Culture);
        if (!configService.SupportedCultures.Contains(Culture))
        {
            Culture = defaultCulture;
        }

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
            _ = CheckForUpdates(true);
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
            decodeErrors.TryAdd(e.DecodeError, true);
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
        decodeErrorsList = decodeErrors.Keys.ToList();
        decodeErrorModal?.Show();
    }

    private void RemoveErrors(List<DecodeError> removedDecodeErrors)
    {
        foreach (var decodeError in removedDecodeErrors)
        {
            //decodeErrorsList.Remove(decodeError);
            decodeErrors.TryRemove(decodeError, out var _);
        }
        InvokeAsync(() => StateHasChanged());
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
                bool answer = await Application.Current.MainPage.DisplayAlert(Loc["New Version Available!"], Loc["Would you like to update now?"], Loc["Yes"], Loc["No"]);
                if (answer)
                {
                    updateService.UpdateProgress += UpdateService_UpdateProgress;
                    var updateResult = await updateService.UpdateApp();
                    if (!updateResult)
                    {
                        updateService.UpdateProgress -= UpdateService_UpdateProgress;
                        await Application.Current.MainPage.DisplayAlert(Loc["Update Failed"], ":(", "Ok");
                    }
                }
            }
        }
        else if (!init)
        {
            toastService.ShowInfo(Loc["Your version is up to date."]);
        }
    }

    private void UpdateService_UpdateProgress(object? sender, UpdateProgressEvent e)
    {
        updateDownloadProgress = e.Progress;
        InvokeAsync(() => StateHasChanged());
    }

    private void SetCulture(CultureInfo cultureInfo)
    {
        configService.AppOptions.Culture = cultureInfo.Name;
        configService.UpdateConfig(configService.AppOptions);

        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        remoteToggleService.SetCulture(cultureInfo.Name);
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