using dsstats.db8;
using dsstats.db8services.Import;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace dsstats.api.Services;

public partial class UploadService
{
    Channel<UploadJob> uploadChannel = Channel.CreateUnbounded<UploadJob>(new UnboundedChannelOptions()
    {
        SingleWriter = false,
        SingleReader = true,
        //AllowSynchronousContinuations = false,
        //FullMode = BoundedChannelFullMode.DropWrite
    });

    bool consuming;
    private readonly object uploadlock = new();

    private bool ProduceUploadJob(UploadDto uploadDto, string replayBlob)
    {
        if (string.IsNullOrEmpty(replayBlob))
        {
            return false;
        }

        if (uploadChannel.Writer.TryWrite(new(uploadDto, replayBlob)))
        {
            lock (uploadlock)
            {
                if (!consuming)
                {
                    _ = ConsumeUploadJobs();
                    consuming = true;
                }
            }
            return true;
        }
        return false;
    }

    private async ValueTask ConsumeUploadJobs()
    {
        using var scope = scopeFactory.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

        while (true)
        {
            var uploadJob = await uploadChannel.Reader.ReadAsync();
            bool success = false;
            ImportResult? importResult = null;
            try
            {
                await CreateOrUpdateUploader(uploadJob.UploadDto);

                var replays = await GetReplaysFromBase64String(uploadJob.UploadDto.Base64ReplayBlob);
                importResult = await importService.Import(replays, uploadJob.UploadDto.RequestNames
                    .Select(s => new PlayerId(s.ToonId, s.RealmId, s.RegionId)).ToList());
                if (string.IsNullOrEmpty(importResult.Error))
                {
                    File.Move(uploadJob.ReplayBlob, uploadJob.ReplayBlob + ".done");
                    success = true;
                }
                else
                {
                    File.Move(uploadJob.ReplayBlob, uploadJob.ReplayBlob + ".error");
                    success = false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError("failed comsuming upload job: {error}", ex.Message);
            }
            finally
            {
                ReplayBlobsInQueue.TryRemove(uploadJob.ReplayBlob, out _);
                OnBlobImported(new()
                {
                    ReplayBlob = uploadJob.ReplayBlob,
                    Success = success,
                    Imported = importResult?.Imported ?? 0,
                    Duplicates = importResult?.Duplicates ?? 0
                });
                logger.LogWarning("Replays imported {count}, duplicates: {dups}",
                    importResult?.Imported, importResult?.Duplicates);
            }
        }
    }

    private async Task CreateOrUpdateUploader(UploadDto uploadDto)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var dbUploader = await context.Uploaders
                .Include(i => i.Players)
                .FirstOrDefaultAsync(f => f.AppGuid == uploadDto.AppGuid);

            if (dbUploader == null)
            {
                dbUploader = new()
                {
                    AppGuid = uploadDto.AppGuid,
                };
                context.Uploaders.Add(dbUploader);
                await context.SaveChangesAsync();
            }

            dbUploader.AppVersion = uploadDto.AppVersion;
            dbUploader.LatestUpload = DateTime.UtcNow;
            await SetupUploaderPlayers(dbUploader, uploadDto.RequestNames, context);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed setting upload players: {error}", ex.Message);
        }
    }

    private async Task SetupUploaderPlayers(Uploader uploader, List<RequestNames> requestNames, ReplayContext context)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

            List<int> playerIds = new();
            foreach (var rn in requestNames)
            {
                var id = await importService.GetPlayerIdAsync(new(rn.ToonId, rn.RealmId, rn.RegionId), rn.Name);
                playerIds.Add(id);

                // PezaUkraine fix
                if (rn.ToonId == 2474605 && rn.RegionId == 2 && rn.RealmId == 1)
                {
                    var id2 = await importService.GetPlayerIdAsync(new(rn.ToonId, rn.RealmId, 2), rn.Name);
                    playerIds.Add(id2);
                }
            }

            foreach (var player in uploader.Players.ToArray())
            {
                if (playerIds.Contains(player.PlayerId))
                {
                    playerIds.Remove(player.PlayerId);
                }
                else
                {
                    uploader.Players.Remove(player);
                }
            }

            foreach (var playerId in playerIds)
            {
                var dbPlayer = await context.Players
                    .FirstOrDefaultAsync(f => f.PlayerId == playerId);

                if (dbPlayer == null)
                {
                    continue;
                }
                dbPlayer.UploaderId = uploader.UploaderId;
                dbPlayer.Uploader = uploader;
                uploader.Players.Add(dbPlayer);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed setting uploader players: {error}", ex.Message);
        }
    }
}

internal record UploadJob
{
    public UploadJob(UploadDto uploadDto, string replayBlob)
    {
        UploadDto = uploadDto;
        ReplayBlob = replayBlob;
    }

    public UploadDto UploadDto { get; init; } = null!;
    public string ReplayBlob { get; init; } = string.Empty;
}