
using System.IO.Compression;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;

namespace dsstats.worker;

public partial class DsstatsService
{
    private readonly string uploaderController = "api/Upload";

    private async Task Upload()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var httpClient = httpClientFactory.CreateClient("dsstats");

        await ssUpload.WaitAsync();
        try
        {
            int skip = 0;
            int take = 1000;

            List<string> uploadedReplayHashes = new();

            var replays = await GetUploadReplays(context, skip, take);

            while (replays.Count > 0)
            {
                CleanReplays(replays);
                var uploadDto = await GetUploadDto(replays);        

                if (await UploadBlob(httpClient, uploadDto))
                {
                    uploadedReplayHashes.AddRange(replays.Select(s => s.ReplayHash));
                }
                skip += take;      
            }

            if (uploadedReplayHashes.Count > 0)
            {
                await SetUploadedFlag(context, uploadedReplayHashes);
            }
        }
        finally
        {
            ssUpload.Release();
        }
    }

    private async Task<UploadDto> GetUploadDto(List<ReplayDto> replayDtos)
    {
        var base64string = await GetBase64String(replayDtos);
        var latestReplayDate = GetLatestReplayDate(replayDtos);
        return new() 
        {
            AppGuid = AppConfigOptions.AppGuid,
            LatestReplays = latestReplayDate,
            Base64ReplayBlob = base64string
        };
    }

    private DateTime GetLatestReplayDate(List<ReplayDto> replayDtos)
    {
        var myLatestReplayDate = replayDtos.Last().GameTime.AddSeconds(10);
        return new DateTime(myLatestReplayDate.Year,
            myLatestReplayDate.Month,
            myLatestReplayDate.Day,
            myLatestReplayDate.Hour,
            myLatestReplayDate.Minute,
            myLatestReplayDate.Second);
    }

    private void CleanReplays(List<ReplayDto> replayDtos)
    {
        replayDtos.ForEach(f =>
        {
            f.FileName = string.Empty;
            f.PlayerResult = PlayerResult.None;
            f.PlayerPos = 0;
        });
        replayDtos.SelectMany(s => s.ReplayPlayers).ToList().ForEach(f =>
        {
            f.MmrChange = 0;
        });
    }

    private async Task<bool> UploadBlob(HttpClient httpClient, UploadDto uploadDto)
    {
        try 
        {
            var response = await httpClient.PostAsJsonAsync($"{uploaderController}/ImportReplays", uploadDto);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);
        }
        return false;
    }

    private async Task<List<ReplayDto>> GetUploadReplays(ReplayContext context, int skip, int take)
    {

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

    private async Task SetUploadedFlag(ReplayContext context, List<string> importedReplayHashes)
    {
        string replayHashString = String.Join(", ", importedReplayHashes.Select(s => $"'{s}'"));

        string updateCommand = $"UPDATE {nameof(ReplayContext.Replays)} SET {nameof(Replay.Uploaded)} = 1 WHERE {nameof(Replay.ReplayHash)} IN ({replayHashString});";

        await context.Database.ExecuteSqlRawAsync(updateCommand);
    }

    private async Task<string> GetBase64String(List<ReplayDto> replays)
    {
        var json = JsonSerializer.Serialize(replays);
        return await ZipAsync(json);
    }

    private static async Task<string> ZipAsync(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using (var gs = new GZipStream(mso, CompressionMode.Compress))
        {
            await msi.CopyToAsync(gs);
        }
        return Convert.ToBase64String(mso.ToArray());
    }


}