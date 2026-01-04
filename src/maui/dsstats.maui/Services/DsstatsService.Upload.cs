using dsstats.db;
using dsstats.maui.Services.Models;
using dsstats.shared.Upload;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace dsstats.maui.Services;

public partial class DsstatsService
{
    public async Task<UploadResult> Upload(ImportState importState, CancellationToken token = default)
    {
        var result = await StartUpload(importState, token);
        return result;
    }

    private async Task<UploadResult> StartUpload(ImportState importState, CancellationToken token = default)
    {
        var config = await GetConfig();
        if (!config.UploadCredential)
        {
            _uploadStatus = UploadStatus.Forbidden;
            importState.UpdateProgress(importState.Progress with
            {
                UploadStatus = _uploadStatus,
            });
            return new();
        }
        _uploadStatus = UploadStatus.Uploading;
        importState.UpdateProgress(importState.Progress with
        {
            UploadStatus = _uploadStatus,
        });
        int take = 1000;

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var httpClient = httpClientFactory.CreateClient("api");
        httpClient.DefaultRequestHeaders.Authorization = new("DS8upload77");

        try
        {
            while (!token.IsCancellationRequested)
            {
                var replays = await context.Replays
                    .Where(x => !x.Uploaded)
                    .AsNoTracking()
                    .OrderByDescending(o => o.Gametime)
                    .Include(i => i.Players)
                        .ThenInclude(i => i.Player)
                    .Include(i => i.Players)
                        .ThenInclude(i => i.Upgrades)
                            .ThenInclude(i => i.Upgrade)
                    .Include(i => i.Players)
                        .ThenInclude(i => i.Spawns)
                            .ThenInclude(i => i.Units)
                                .ThenInclude(i => i.Unit)
                    .AsSplitQuery()
                        .Take(take)
                        .ToListAsync(token);

                if (replays.Count == 0)
                {
                    break;
                }

                UploadRequestDto upload = new()
                {
                    AppGuid = config.AppGuid,
                    AppVersion = config.Version,
                    RequestNames = config.Sc2Profiles
                        .Where(x => x.ToonId.Id > 0)
                        .Select(s => new RequestNames()
                    {
                        Name = s.Name,
                        ToonId = s.ToonId.Id,
                        RegionId = s.ToonId.Region,
                        RealmId = s.ToonId.Realm,
                    }).ToList(),
                    Replays = replays.Select(s => s.ToDto()).ToList(),
                };
                upload.Replays.ForEach(f => f.FileName = string.Empty);

                if (replays.Count > 1)
                {
                    var json = JsonSerializer.Serialize(upload);

                    var content = new ByteArrayContent(CompressBrotli(json));

                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/json");

                    content.Headers.ContentEncoding.Add("br");

                    var result = await httpClient.PostAsync("api10/Upload", content, token);
                    result.EnsureSuccessStatusCode();
                }
                else
                {
                    var result = await httpClient.PostAsJsonAsync("api10/Upload", upload, token);
                    result.EnsureSuccessStatusCode();
                }

                var replayIds = replays.Select(s => s.ReplayId).ToList();
                await context.Replays
                    .Where(x => replayIds.Contains(x.ReplayId))
                    .ExecuteUpdateAsync(e => e.SetProperty(p => p.Uploaded, true));
            }
            _uploadStatus = UploadStatus.Success;
            importState.UpdateProgress(importState.Progress with
            {
                UploadStatus = _uploadStatus,
            });
            return new() { Success = true };
        }
        catch (OperationCanceledException)
        {
            _uploadStatus = UploadStatus.None;
            return new() { Success = true };
        }
        catch (Exception ex)
        {
            _uploadStatus = UploadStatus.Failed;
            importState.UpdateProgress(importState.Progress with
            {
                UploadStatus = _uploadStatus,
            });
            return new()
            {
                Error = ex.Message,
            };
        }
    }

    static byte[] Compress(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Fastest))
        {
            gz.Write(bytes, 0, bytes.Length);
        }
        return ms.ToArray();
    }

    static byte[] CompressBrotli(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        using var ms = new MemoryStream();
        using (var br = new BrotliStream(ms, CompressionLevel.Fastest, leaveOpen: true))
        {
            br.Write(bytes, 0, bytes.Length);
        }

        return ms.ToArray();
    }
}
