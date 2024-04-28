using dsstats.db8services.Import;
using dsstats.shared;
using pax.dsstats.parser;
using s2protocol.NET;
using System.Reflection;
using System.Security.Cryptography;

namespace dsstats.api.Services;

public class DecodeService(ILogger<DecodeService> logger, IServiceScopeFactory scopeFactory)
{
    private readonly string replayFolder = "/data/ds/decode";

    private readonly SemaphoreSlim ss = new(1, 1);
    private ReplayDecoder? replayDecoder;
    public static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

    public EventHandler<DecodeEventArgs>? DecodeFinished;

    private void OnDecodeFinished(DecodeEventArgs e)
    {
        DecodeFinished?.Invoke(this, e);
    }

    public async Task<bool> SaveReplays(Guid guid, List<IFormFile> files)
    {
        long size = files.Sum(f => f.Length);

        foreach (var formFile in files)
        {
            if (formFile.Length > 0)
            {
                var filePath = Path.Combine(replayFolder, "todo", guid.ToString() + "_" + Guid.NewGuid().ToString() + ".SC2Replay");

                using (var stream = File.Create(filePath))
                {
                    await formFile.CopyToAsync(stream);
                }
            }
        }
        _ = Decode(guid);
        return true;
    }

    public async Task Decode(Guid guid)
    {
        await ss.WaitAsync();
        List<ReplayDto> replays = [];

        try
        {
            var replayPaths = Directory.GetFiles(Path.Combine(replayFolder, "todo"), "*SC2Replay");

            if (replayPaths.Length == 0)
            {
                return;
            }

            if (replayDecoder is null)
            {
                replayDecoder = new(assemblyPath);
            }

            var options = new ReplayDecoderOptions()
            {
                Initdata = true,
                Details = true,
                Metadata = true,
                TrackerEvents = true,
            };

            using var md5 = MD5.Create();

            await foreach(var result in replayDecoder.DecodeParallelWithErrorReport(replayPaths, 2, options))
            {
                if (result.Sc2Replay is null)
                {
                    Error(result);
                    continue;
                }

                var sc2Replay = Parse.GetDsReplay(result.Sc2Replay);

                if (sc2Replay is null)
                {
                    Error(result);
                    continue;
                }

                var replayDto = Parse.GetReplayDto(sc2Replay, md5);

                if (replayDto is null)
                {
                    Error(result);
                    continue;
                }

                File.Move(result.ReplayPath, Path.Combine(replayFolder, "done", Path.GetFileName(result.ReplayPath)));
                replays.Add(replayDto);
            }

            if (replays.Count > 0)
            {
                using var scope = scopeFactory.CreateScope();
                var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
                await importService.Import(replays);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed decoding replays: {error}", ex.Message);
        }
        finally
        {
            ss.Release();
            OnDecodeFinished(new()
            {
                Guid = guid,
                ReplayHashes = replays.Select(s => s.ReplayHash).ToList()
            });
        }
    }

    private void Error(DecodeParallelResult result)
    {
        logger.LogError("failed decoding replay: {path}", result.ReplayPath);
        File.Move(result.ReplayPath, Path.Combine(replayFolder, "error", Path.GetFileName(result.ReplayPath)));
    }
}

public class DecodeEventArgs : EventArgs
{
    public Guid Guid { get; set; }
    public List<string> ReplayHashes { get; set; } = [];
}