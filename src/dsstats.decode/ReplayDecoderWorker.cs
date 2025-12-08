
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using dsstats.challenge.Services;
using dsstats.shared;
using Microsoft.Extensions.Options;
using pax.dsstats.parser;
using s2protocol.NET;

namespace dsstats.decode;

public class ReplayDecoderWorker(
    IReplayQueue replayQueue,
    IOptions<DecodeSettings> decodeSettings,
    IHttpClientFactory httpClientFactory,
    ILogger<ReplayDecoderWorker> logger) : BackgroundService
{
    private readonly IReplayQueue replayQueue = replayQueue;
    private readonly ILogger<ReplayDecoderWorker> logger = logger;
    private readonly DecodeSettings decodeSettings = decodeSettings.Value;
    private readonly SemaphoreSlim fileSemaphore = new(1, 1);
    private readonly ReplayDecoder replayDecoder = new();

    public EventHandler<DecodeEventArgs>? DecodeFinished;
    public EventHandler<DecodeRawEventArgs>? DecodeInHouseFinished;

    private async Task RaiseDecodeInHouseFinishedAsync(DecodeEventArgs e, CancellationToken token)
    {
        var httpClient = httpClientFactory.CreateClient("callback");
        try
        {
            var result = await httpClient.PostAsJsonAsync(
                $"/api8/v1/upload/decoderesult/{e.Guid}", e.IhReplays, token);
            result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError("failed reporting decoderesult: {error}", ex.Message);
        }

        DecodeFinished?.Invoke(this, e);
    }

    private async Task RaiseDecodeFinishedAsync(DecodeRawEventArgs e, CancellationToken token)
    {
        var httpClient = httpClientFactory.CreateClient("callback");
        try
        {
            var result = await httpClient.PostAsJsonAsync(
                $"/api8/v1/upload/decoderawresult/{e.Guid}", e.ChallengeResponses, token);
            result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError("failed reporting decoderesult: {error}", ex.Message);
        }

        DecodeInHouseFinished?.Invoke(this, e);
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ReplayDecoderWorker started");

        await foreach (var job in replayQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessReplayJob(job, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unhandled exception processing replay job for file {file}",
                    job.TempFilePath);
            }
            finally
            {
                (replayQueue as ReplayQueue)?.Decrement();
            }
        }

        logger.LogInformation("ReplayDecoderWorker stopped");
    }

    private async Task ProcessReplayJob(ReplayJob job, CancellationToken token)
    {
        logger.LogInformation("Decoding replay: {file}", job.TempFilePath);
        var options = new ReplayDecoderOptions()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            TrackerEvents = true,
        };

        var sc2Replay = await replayDecoder.DecodeAsync(job.TempFilePath, options, token);
        if (sc2Replay == null)
        {
            HandleError(job, "Emty decode result");
            return;
        }

        var meta = job.InHouse ? DecodeService.GetMetaData(sc2Replay) : null;
        var dsReplay = Parse.GetDsReplay(sc2Replay);
        if (dsReplay == null)
        {
            HandleError(job, "Empty parse result");
            return;
        }

        using var md5 = MD5.Create();
        var replayDto = Parse.GetReplayDto(dsReplay, md5);
        if (replayDto == null)
        {
            HandleError(job, "Empty dto result");
            return;
        }

        // Output destination
        string destination =
            Path.Combine(
                decodeSettings.ReplayFolders.Done,
                $"{job.GroupId}_{replayDto.ReplayHash}.SC2Replay");

        // Move atomically
        await fileSemaphore.WaitAsync(token);
        try
        {
            if (!File.Exists(destination))
                File.Move(job.TempFilePath, destination);
        }
        finally
        {
            fileSemaphore.Release();
        }

        // Raise event/callback
        if (job.InHouse)
        {
            await RaiseDecodeInHouseFinishedAsync(new()
            {
                Guid = job.GroupId,
                IhReplays =
                [
                    new IhReplay
                {
                    Replay = replayDto,
                    Metadata = meta ?? new()
                }
                ],
                Error = null
            }, token);
        }
        else
        {
            await RaiseDecodeFinishedAsync(new()
            {
                Guid = job.GroupId,
                ChallengeResponses = [ChallengeService.GetChallengeResponse(sc2Replay)],
            }, token);
        }
    }

    private void HandleError(ReplayJob job, string error)
    {
        logger.LogError("Error decoding replay file {file}, error: {error}", job.TempFilePath, error);

        string errorDest = Path.Combine(
            decodeSettings.ReplayFolders.Error,
            Path.GetFileName(job.TempFilePath));

        try
        {
            File.Move(job.TempFilePath, errorDest);
        }
        catch (Exception ioEx)
        {
            logger.LogWarning(ioEx,
                "Failed moving error file {file}", job.TempFilePath);
        }
    }

    public static async Task MoveWithRetry(string src, string dest, int retries = 5)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                File.Move(src, dest);
                return;
            }
            catch when (i < retries - 1)
            {
                await Task.Delay(100);
            }
        }

        File.Move(src, dest); // final throw
    }
}
