using dsstats.db;
using dsstats.service.Models;
using dsstats.shared.Upload;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace dsstats.service.Services;

internal sealed partial class DsstatsService
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
                        .Where(x => x.PlayerId.ToonId > 0)
                        .Select(s => new RequestNames()
                        {
                            Name = s.Name,
                            ToonId = s.PlayerId.ToonId,
                            RegionId = s.PlayerId.RegionId,
                            RealmId = s.PlayerId.RealmId,
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

#pragma warning disable CA2234 // Pass system uri objects instead of strings
                    var result = await httpClient.PostAsync("api10/Upload", content, ct);
#pragma warning restore CA2234 // Pass system uri objects instead of strings
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
