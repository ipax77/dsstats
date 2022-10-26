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


    //private HttpClient GetHttpClient(IServiceScope scope)
    //{
    //    var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
    //    httpClient.BaseAddress = new Uri("https://localhost:7174");
    //    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    //    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DSupload77");
    //    return httpClient;
    //}

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
            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var latestReplayDate = await GetLastReplayDate(context, scope);

            var replays = await context.Replays
                .Include(i => i.Players)
                    .ThenInclude(t => t.Spawns)
                        .ThenInclude(t => t.Units)
                            .ThenInclude(t => t.Unit)
                .Include(i => i.Players)
                    .ThenInclude(t => t.Player)
                    .AsNoTracking()
                    .AsSplitQuery()
                .OrderBy(o => o.GameTime)
                .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
                .Where(x => x.GameTime > latestReplayDate)
                .ToListAsync();

            replays.ForEach(f => f.FileName = string.Empty);

            var base64string = GetBase64String(replays);
            var httpClient = GetHttpClient();

            var response = await httpClient.PostAsJsonAsync($"api/Upload/ImportReplays/{UserSettingsService.UserSettings.AppGuid}", base64string);
            if (response.IsSuccessStatusCode)
            {
                OnUploadStateChanged(new() { UploadStatus = UploadStatus.Success });
            }
            else
            {
                logger.LogError($"failed uploading replays: {response.StatusCode}");
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

    private string GetBase64String(List<ReplayDto> replays)
    {
        var json = JsonSerializer.Serialize(replays);
        return Zip(json);
    }

    private async Task<DateTime> GetLastReplayDate(ReplayContext context, IServiceScope scope)
    {
        UploaderDto uploaderDto = new()
        {
            AppGuid = UserSettingsService.UserSettings.AppGuid,
            AppVersion = UpdateService.CurrentVersion.ToString(),
            Players = await GetPlayerUploadDtos(context),
            BattleNetInfos = UserSettingsService.UserSettings.BattleNetIds?.Select(s => new BattleNetInfoDto()
            {
                BattleNetId = s.Key
            }).ToList()
        };

        var httpClient = GetHttpClient();

        try
        {
            var response = await httpClient.PostAsJsonAsync("api/Upload/GetLatestReplayDate", uploaderDto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DateTime>();
            }
            else
            {
                logger.LogError($"failed getting latest replay: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting latest replays: {ex.Message}");
            OnUploadStateChanged(new() { UploadStatus = UploadStatus.Error });
            throw;
        }
        return new DateTime(2022, 1, 1);
    }

    private async Task<List<PlayerUploadDto>> GetPlayerUploadDtos(ReplayContext context)
    {
        return await context.ReplayPlayers
            .Include(i => i.Player)
            .Where(x => x.IsUploader)
            .AsNoTracking()
            .Select(s => new PlayerUploadDto
            {
                Name = s.Name,
                ToonId = s.Player.ToonId
            }).Distinct()
            .ToListAsync();
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
}

public enum UploadStatus
{
    None = 0,
    Uploading = 1,
    Error = 2,
    Success = 3
}

public class UploadeEventArgs : EventArgs
{
    public UploadStatus UploadStatus { get; init; }
}