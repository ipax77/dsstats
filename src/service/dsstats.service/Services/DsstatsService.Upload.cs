using dsstats.db;
using dsstats.service.Models;
using dsstats.shared.Upload;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace dsstats.service.Services;

internal partial class DsstatsService
{
    public async Task Upload(AppOptions config, CancellationToken ct)
    {
        if (!config.UploadCredential)
        {
            return;
        }
        int take = 1000;
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var httpClient = httpClientFactory.CreateClient("api");
        httpClient.DefaultRequestHeaders.Authorization = new("DS8upload77");

        try
        {
            while (!ct.IsCancellationRequested)
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
                        .ToListAsync(ct);

                if (replays.Count == 0)
                {
                    break;
                }

                UploadRequestDto upload = new()
                {
                    AppGuid = config.AppGuid,
                    AppVersion = "99.8",
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

                    using var content = new ByteArrayContent(CompressBrotli(json));

                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/json");

                    content.Headers.ContentEncoding.Add("br");

                    var result = await httpClient.PostAsync(new Uri("api10/Upload"), content, ct);
                    result.EnsureSuccessStatusCode();
                }
                else
                {
                    var result = await httpClient.PostAsJsonAsync("api10/Upload", upload, ct);
                    result.EnsureSuccessStatusCode();
                }

                var replayIds = replays.Select(s => s.ReplayId).ToList();
                await context.Replays
                    .Where(x => replayIds.Contains(x.ReplayId))
                    .ExecuteUpdateAsync(e => e.SetProperty(p => p.Uploaded, true), ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError("Upload failed: {error}", ex.Message);
        }
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
