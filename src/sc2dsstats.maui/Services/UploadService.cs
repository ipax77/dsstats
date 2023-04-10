using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace sc2dsstats.maui.Services;

public class UploadService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<UploadService> logger;
    private readonly SemaphoreSlim ss = new(1, 1);

    private readonly string uploaderController = "api/Upload/";

    public UploadService(IServiceProvider serviceProvider, IMapper mapper, ILogger<UploadService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }

    public event EventHandler<UploadeEventArgs>? UploadStateChanged;
    protected virtual void OnUploadStateChanged(UploadeEventArgs e)
    {
        EventHandler<UploadeEventArgs>? handler = UploadStateChanged;
        handler?.Invoke(this, e);
    }

    private HttpClient GetHttpClient()
    {
        var httpClient = new HttpClient();
        // httpClient.BaseAddress = new Uri("https://localhost:7174");
        httpClient.BaseAddress = new Uri("https://dsstats.pax77.org");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DSupload77");
        return httpClient;
    }

    private void UploadAnonymizedReplays()
    {
        throw new NotImplementedException();
    }

    public async Task UploadReplays()
    {
        await ss.WaitAsync();

        if (!UserSettingsService.UserSettings.AllowUploads)
        {
            return;
        }

        if (!UserSettingsService.UserSettings.AllowCleanUploads)
        {
            UploadAnonymizedReplays();
            return;
        }

        OnUploadStateChanged(new() { UploadStatus = UploadStatus.Uploading });

        try
        {
            int skip = 0;
            int take = 1000;

            var replays = await GetUploadReplays(skip, take);

            if (!replays.Any())
            {
                OnUploadStateChanged(new() { UploadStatus = UploadStatus.Success });
                return;
            }

            var latestReplayDate = await GetLastReplayDate();
            if (latestReplayDate == null)
            {
                return;
            }
            bool success = false;

            List<string> importedReplayHashes = new();

            while (replays.Any())
            {
                var myLatestReplayDate = replays.Last().GameTime.AddSeconds(10);
                myLatestReplayDate = new DateTime(myLatestReplayDate.Year,
                    myLatestReplayDate.Month,
                    myLatestReplayDate.Day,
                    myLatestReplayDate.Hour,
                    myLatestReplayDate.Minute,
                    myLatestReplayDate.Second);


                replays.ForEach(f =>
                    {
                        f.FileName = string.Empty;
                        f.PlayerResult = PlayerResult.None;
                        f.PlayerPos = 0;
                    });
                replays.SelectMany(s => s.ReplayPlayers).ToList().ForEach(f =>
                {
                    f.MmrChange = 0;
                });

                var base64string = GetBase64String(replays);
                var httpClient = GetHttpClient();

                UploadDto uploadDto = new()
                {
                    AppGuid = UserSettingsService.UserSettings.AppGuid,
                    LatestReplays = myLatestReplayDate,
                    Base64ReplayBlob = base64string
                };

                var response = await httpClient.PostAsJsonAsync($"{uploaderController}ImportReplays", uploadDto);

                if (response.IsSuccessStatusCode)
                {
                    importedReplayHashes.AddRange(replays.Select(s => s.ReplayHash));
                    success = true;
                }
                else
                {
                    logger.LogError($"failed uploading replays: {response.StatusCode}");
                    success = false;
                    break;
                }

                skip += take;
                replays = await GetUploadReplays(skip, take);
            }

            if (success)
            {
                await SetUploadedFlag(importedReplayHashes);
                OnUploadStateChanged(new() { UploadStatus = UploadStatus.Success });
            }
            else
            {
                OnUploadStateChanged(new() { UploadStatus = UploadStatus.Error });
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed uploading replays: {ex.Message}");
            OnUploadStateChanged(new() { UploadStatus = UploadStatus.Error });
        }
        finally
        {
            ss.Release();
        }
    }

    private async Task<List<ReplayDto>> GetUploadReplays(int skip, int take)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Spawns)
                    .ThenInclude(t => t.Units)
                        .ThenInclude(t => t.Unit)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Player)
                .AsNoTracking()
                .AsSplitQuery()
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .Where(x => !x.Uploaded)
            .Skip(skip)
            .Take(take)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    private async Task SetUploadedFlag(List<string> importedReplayHashes)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        string replayHashString = String.Join(", ", importedReplayHashes.Select(s => $"'{s}'"));

        string updateCommand = $"UPDATE {nameof(ReplayContext.Replays)} SET {nameof(Replay.Uploaded)} = 1 WHERE {nameof(Replay.ReplayHash)} IN ({replayHashString});";

        await context.Database.ExecuteSqlRawAsync(updateCommand);
    }

    private string GetBase64String(List<ReplayDto> replays)
    {
        var json = JsonSerializer.Serialize(replays);
        return Zip(json);
    }

    private async Task<DateTime?> GetLastReplayDate()
    {
        UploaderDto uploaderDto = new()
        {
            AppGuid = UserSettingsService.UserSettings.AppGuid,
            AppVersion = UpdateService.CurrentVersion.ToString(),
            BattleNetInfos = UserSettingsService.UserSettings.BattleNetInfos?.Select(s => new BattleNetInfoDto()
            {
                BattleNetId = s.BattleNetId,
                PlayerUploadDtos = GetPlayerUploadDtos(s.ToonIds),
            }).ToList() ?? new()
        };

        var httpClient = GetHttpClient();

        try
        {
            var response = await httpClient.PostAsJsonAsync($"{uploaderController}GetLatestReplayDate", uploaderDto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DateTime>();
            }
            else
            {
                logger.LogError($"failed getting latest replay: {response.StatusCode}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    OnUploadStateChanged(new() { UploadStatus = UploadStatus.Forbidden });
                }
                else
                {
                    OnUploadStateChanged(new() { UploadStatus = UploadStatus.Error });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting latest replays: {ex.Message}");
            OnUploadStateChanged(new() { UploadStatus = UploadStatus.Error });
        }
        return null;
    }

    private List<PlayerUploadDto> GetPlayerUploadDtos(List<ToonIdInfo> toonIdInfos)
    {
        List<PlayerUploadDto> playerUploadDtos = new();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        foreach (var info in toonIdInfos)
        {
            playerUploadDtos.Add(new()
            {
                Name = context.Players.FirstOrDefault(f => 
                    f.ToonId == info.ToonId
                    && f.RealmId == info.RealmId
                    && f.RegionId == info.RegionId)?.Name ?? "Anonymous",
                RegionId = info.RegionId,
                ToonId = info.ToonId,
                RealmId = info.RealmId,
            });
        }
        return playerUploadDtos;
    }

    private static string Zip(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                msi.CopyTo(gs);
            }
            return Convert.ToBase64String(mso.ToArray());
        }
    }

    internal void ProduceTestData()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Spawns)
                    .ThenInclude(t => t.Units)
                        .ThenInclude(t => t.Unit)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Player)
                .AsNoTracking()
                .AsSplitQuery()
            .OrderByDescending(o => o.GameTime)
            .Take(33)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .ToList();

        replays.ForEach(f => f.FileName = string.Empty);

        var base64string = GetBase64String(replays);
        var testDataPath = "/data/ds/testdata";
        File.WriteAllText(Path.Combine(testDataPath, "uploadtest.base64"), base64string);
        base64string = GetBase64String(replays.Take(1).ToList());
        File.WriteAllText(Path.Combine(testDataPath, "uploadtest2.base64"), base64string);

        var testReplay1 = replays.ElementAt(7);
        var testReplay2 = replays.ElementAt(17);

        File.WriteAllText(Path.Combine(testDataPath, "replayDto1.json"), JsonSerializer.Serialize(new List<ReplayDto>() { testReplay1 }));
        File.WriteAllText(Path.Combine(testDataPath, "replayDto2.json"), JsonSerializer.Serialize(new List<ReplayDto>() { testReplay2 }));
    }

    internal void ProduceMauiTestData()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Spawns)
                    .ThenInclude(t => t.Units)
                        .ThenInclude(t => t.Unit)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Player)
                .AsNoTracking()
                .AsSplitQuery()
            .OrderByDescending(o => o.GameTime)
            .Take(33)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .ToList();

        replays.ForEach(f => f.FileName = string.Empty);

        var json1 = JsonSerializer.Serialize(replays.Take(30));
        var json2 = JsonSerializer.Serialize(replays.Skip(30));

        var testDataPath = "/data/ds/testdata";
        File.WriteAllText(Path.Combine(testDataPath, "testreplays1.json"), json1);
        File.WriteAllText(Path.Combine(testDataPath, "testreplays2.json"), json2);

    }

    public async Task<DateTime> DisableUploads()
    {
        var httpClient = GetHttpClient();
        try
        {
            var response = await httpClient.GetAsync($"{uploaderController}DisableUploader/{UserSettingsService.UserSettings.AppGuid}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DateTime>();
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed disabling uploads: {ex.Message}");
        }
        return DateTime.MinValue;
    }

    public async Task<bool> DeleteMe()
    {
        var httpClient = GetHttpClient();
        try
        {
            var response = await httpClient.GetAsync($"{uploaderController}DeleteUploader/{UserSettingsService.UserSettings.AppGuid}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<bool>();
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed deleting uploader: {ex.Message}");
        }
        return false;
    }
}

public enum UploadStatus
{
    None = 0,
    Uploading = 1,
    Error = 2,
    Success = 3,
    Forbidden = 4
}

public class UploadeEventArgs : EventArgs
{
    public UploadStatus UploadStatus { get; init; }
}