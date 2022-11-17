﻿using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using sc2dsstats.maui.Services;

namespace sc2dsstats.maui.Shared;

public partial class TopRow : ComponentBase, IDisposable
{
    [Inject]
    protected DecodeService decodeService { get; set; } = null!;

    [Inject]
    protected UploadService uploadService { get; set; } = null!;


    [Inject]
    protected NavigationManager navigationManager { get; set; } = null!;

    [Inject]
    protected IToastService ToastService { get; set; } = default!;

    private DecodeEventArgs? decodeEventArgs;
    private TimeSpan elapsed = TimeSpan.Zero;
    private TimeSpan eta = TimeSpan.Zero;
    private UploadStatus uploadStatus;
    private uint updateProgress;

    // private ReplaysFailedModal? replaysFailedModal;

    protected override void OnInitialized()
    {
        decodeService.DecodeStateChanged += DecodeService_DecodeStateChanged;
        decodeService.ScanStateChanged += DecodeService_ScanStateChanged;
        uploadService.UploadStateChanged += UploadService_UploadStateChanged;
        UpdateService.UpdateProgress += UpdateService_UpdateProgress;
        base.OnInitialized();
    }

    private void UpdateService_UpdateProgress(object? sender, UpdateProgressEvent e)
    {
        updateProgress = e.Progress;
        InvokeAsync(() => StateHasChanged());
    }

    private void UploadService_UploadStateChanged(object? sender, UploadeEventArgs e)
    {
        uploadStatus = e.UploadStatus;
        InvokeAsync(() => StateHasChanged());
    }

    private void DecodeService_ScanStateChanged(object? sender, ScanEventArgs e)
    {
        InvokeAsync(() => StateHasChanged());
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await decodeService.ScanForNewReplays();
            await InvokeAsync(() => StateHasChanged());
            CheckForUpdates(true);
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private void DecodeService_DecodeStateChanged(object? sender, DecodeEventArgs e)
    {
        decodeEventArgs = e;

        elapsed = DateTime.UtcNow - e.Start;

        if (!decodeEventArgs.Done)
        {
            if (decodeEventArgs.Decoded < 10)
            {
                if (UserSettingsService.UserSettings.CpuCoresUsedForDecoding > 0)
                    eta = TimeSpan.FromSeconds(decodeEventArgs.Total * 6 / UserSettingsService.UserSettings.CpuCoresUsedForDecoding) - elapsed;
                else
                    eta = TimeSpan.FromSeconds(decodeEventArgs.Total * 6) - elapsed;
            }
            else
            {
                double one = elapsed.TotalSeconds / (double)decodeEventArgs.Decoded;
                eta = TimeSpan.FromSeconds(one * (decodeEventArgs.Total - decodeEventArgs.Decoded));
            }
        }
        else
        {
            ToastService.ShowSuccess("Decoding finished");
            eta = TimeSpan.Zero;
        }

        InvokeAsync(() => StateHasChanged());
    }

    private void StartDecoding()
    {
        if (decodeService.IsRunning)
        {
            navigationManager.NavigateTo("/");
            return;
        }
        ToastService.ShowWarning("Start decoding");
        _ = decodeService.DecodeParallel().ConfigureAwait(false);
    }

    public async void CheckForUpdates(bool init = false)
    {
        if (init)
        {
            await Task.Delay(5000);
        }
        await UpdateService.CheckUpdate(init);
        await InvokeAsync(() => StateHasChanged());
    }

    public void Dispose()
    {
        decodeService.DecodeStateChanged -= DecodeService_DecodeStateChanged;
        decodeService.ScanStateChanged -= DecodeService_ScanStateChanged;
        UpdateService.UpdateProgress -= UpdateService_UpdateProgress;
    }
}
