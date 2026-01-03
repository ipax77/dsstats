using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using dsstats.db;
using dsstats.maui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;

namespace dsstats.maui.Components.Pages;

public partial class ConfigPage : IDisposable
{
    [Inject]
    private DatabaseService DatabaseService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    private MauiConfig? mauiConfig;
    private EditContext? editContext;
    private bool isLoading;
    private bool hasChanges;

    private static Page? CurrentPage =>
        Application.Current?.Windows.FirstOrDefault()?.Page;


    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += LocationChanged;
        await LoadConfig();
    }

    private async Task LoadConfig()
    {
        isLoading = true;
        StateHasChanged();

        mauiConfig = await DsstatsService.GetConfig();
        editContext = new EditContext(mauiConfig);
        editContext.OnFieldChanged += FieldChanged;

        isLoading = false;
        StateHasChanged();
    }

    private void FieldChanged(object? sender, FieldChangedEventArgs e)
    {
        hasChanges = true;
        if (mauiConfig != null && e.FieldIdentifier.FieldName == nameof(MauiConfig.AutoDecode))
        {
            if (mauiConfig.AutoDecode)
            {
                DsstatsService.StartWatching(mauiConfig);
            }
            else
            {
                DsstatsService.StopWatching();
            }
        }
    }

    private async Task SaveConfig()
    {
        if (mauiConfig is null)
            return;

        isLoading = true;
        StateHasChanged();

        await DsstatsService.SaveConfig(mauiConfig);

        isLoading = false;
        StateHasChanged();
        hasChanges = false;
        await Toast.Make(Loc["Configuration saved successfully"]).Show();
    }

    private void AddProfile()
    {
        if (mauiConfig is null) return;

        mauiConfig.Sc2Profiles.Add(new Sc2Profile
        {
            Active = true,
            ToonId = new()
        });

        editContext?.NotifyFieldChanged(
            new FieldIdentifier(mauiConfig, nameof(mauiConfig.Sc2Profiles)));
    }

    private void RemoveProfile(Sc2Profile profile)
    {
        if (mauiConfig is null) return;

        mauiConfig.Sc2Profiles.Remove(profile);

        editContext?.NotifyFieldChanged(
            new FieldIdentifier(mauiConfig, nameof(mauiConfig.Sc2Profiles)));
    }

    private void RemoveIgnoredReplay(string replayPath)
    {
        if (mauiConfig is null) return;

        mauiConfig.IgnoreReplays = mauiConfig.IgnoreReplays.Where(r => r != replayPath).ToArray();

        editContext?.NotifyFieldChanged(
            new FieldIdentifier(mauiConfig, nameof(mauiConfig.IgnoreReplays)));
    }

    private void RecreateProfilesFromDisk()
    {
        if (mauiConfig is null) return;

        var diskProfiles = DsstatsService.DiscoverProfiles();

        // Remove profiles no longer on disk
        foreach (var existing in mauiConfig.Sc2Profiles.ToArray())
        {
            bool existsOnDisk = diskProfiles.Any(d =>
                d.ToonId.Region == existing.ToonId.Region &&
                d.ToonId.Realm == existing.ToonId.Realm &&
                d.ToonId.Id == existing.ToonId.Id);

            if (!existsOnDisk)
            {
                mauiConfig.Sc2Profiles.Remove(existing);
            }
        }

        // Add new disk profiles
        foreach (var diskProfile in diskProfiles)
        {
            bool alreadyPresent = mauiConfig.Sc2Profiles.Any(p =>
                p.ToonId.Region == diskProfile.ToonId.Region &&
                p.ToonId.Realm == diskProfile.ToonId.Realm &&
                p.ToonId.Id == diskProfile.ToonId.Id);

            if (!alreadyPresent)
            {
                mauiConfig.Sc2Profiles.Add(diskProfile);
            }
        }

        editContext?.NotifyFieldChanged(
            new FieldIdentifier(mauiConfig, nameof(mauiConfig.Sc2Profiles)));
    }


    private async Task BackupDatabase()
    {
        try
        {
            var backupPath = await DatabaseService.BackupDatabase();

            if (!string.IsNullOrEmpty(backupPath))
            {
                await Toast.Make(
                    $"{Loc["Backup successful."]}\nSaved to:\n{backupPath}",
                    ToastDuration.Long)
                    .Show();
            }
            else
            {
                await Toast.Make(Loc["Backup failed."]).Show();
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Backup error: {ex.Message}", ToastDuration.Long).Show();
        }
    }


    private async Task RestoreDatabase()
    {
        try
        {
            var filePickerResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a database backup to restore"
            });

            if (filePickerResult is null)
                return;

            var page = CurrentPage;
            if (page is null)
                return;

            var confirmed = await page.DisplayAlertAsync(
                Loc[Loc["Restore Database?"]],
                Loc[Loc["Are you sure you want to restore the database?"]] + "\n\n" +
                Loc[Loc["The current data will be overwritten."]],
                Loc[Loc["Restore"]],
                Loc[Loc["Cancel"]]);

            if (!confirmed)
                return;

            var success = await DatabaseService.RestoreDatabase(filePickerResult.FullPath);

            if (success)
            {
                editContext?.OnFieldChanged -= FieldChanged;
                mauiConfig = await DsstatsService.GetConfig();
                editContext = new EditContext(mauiConfig);
                editContext.OnFieldChanged += FieldChanged;
                await InvokeAsync(StateHasChanged);
                await Toast.Make(
                    Loc["Backup restore successful."],
                    ToastDuration.Long)
                    .Show();
            }
            else
            {
                await Toast.Make(Loc["Backup restore failed."]).Show();
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Restore error: {ex.Message}", ToastDuration.Long).Show();
        }
    }

    private async void LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (!hasChanges)
        {
            return;
        }

        var page = CurrentPage;
        if (page is null)
            return;

        var confirmed = await page.DisplayAlertAsync(
            Loc["Setting changes not saved!"],
            Loc["Would you like to save the changes, now?"],
            Loc["Save"],
            Loc["Cancel"]);
        if (confirmed)
        {
            await SaveConfig();
        }
    }

    protected override void Dispose(bool disposing)
    {
        NavigationManager.LocationChanged -= LocationChanged;
        editContext?.OnFieldChanged -= FieldChanged;
        base.Dispose(disposing);
    }
}
