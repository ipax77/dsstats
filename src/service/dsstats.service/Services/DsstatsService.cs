using dsstats.db;
using dsstats.dbServices;
using dsstats.decode;
using dsstats.service.Models;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;

namespace dsstats.service.Services;

internal sealed partial class DsstatsService(IServiceScopeFactory scopeFactory,
                                    IHttpClientFactory httpClientFactory,
                                    IOptions<DsstatsConfig> dsstatsConfig,
                                    ILogger<DsstatsService> logger) : IDisposable
{
    public static readonly string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dsstats.worker");

    private static readonly string configFile = Path.Combine(appFolder, "workerconfig.json");
    private readonly SemaphoreSlim _dbSemaphore = new(1, 1);
    internal readonly Version CurrentVersion = new(3, 0, 1);

    public async Task StartImportAsync(CancellationToken token)
    {
        var config = await GetConfig();
        try
        {
            var toDoReplayPaths = await GetToDoReplayPaths(config, token);
            if (toDoReplayPaths.Count == 0)
            {
                logger.LogWarning("no replay to decode found.");
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();
            var channel = Channel.CreateBounded<ReplayResult>(
            new BoundedChannelOptions(dsstatsConfig.Value.BatchSize)
            {
                SingleWriter = false,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });

            var decodeTask = DecodeService.DecodeReplaysToWriter(toDoReplayPaths, config.CPUCores, channel.Writer, token);
            var importTask = ImportFromChannelAsync(channel.Reader, GetToonIds(config), token);

            await decodeTask;
            channel.Writer.TryComplete();
            await importTask;

            await Upload(config, token);
            sw.Stop();
            logger.LogWarning("{count} replays decoded in {time}.", toDoReplayPaths.Count, sw.Elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("Failed decoding replays: {error}", ex.Message);
        }
    }

    private async Task ImportFromChannelAsync(
    ChannelReader<ReplayResult> reader,
    List<ToonIdDto> toonIds,
    CancellationToken ct)
    {
        var batch = new List<ReplayDto>(100);

        try
        {
            await foreach (var result in reader.ReadAllAsync(ct))
            {
                if (result.Replay is null)
                {
                    continue;
                }
                result.Replay.SetUploader(toonIds);
                batch.Add(result.Replay);

                if (batch.Count >= 100)
                {
                    await ImportBatchAsync(batch, ct);
                    batch.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // fall through to final flush
        }
        finally
        {
            if (batch.Count > 0)
            {
                await ImportBatchAsync(batch, CancellationToken.None);
            }
        }
    }

    private async Task ImportBatchAsync(
        List<ReplayDto> batch,
        CancellationToken ct)
    {
        await _dbSemaphore.WaitAsync(ct);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
            await importService.InsertReplays(batch);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    private async Task<IReadOnlyCollection<string>> GetToDoReplayPaths(AppOptions config, CancellationToken ct)
    {
        var existingReplayPaths = await GetExistingReplayPaths(ct);
        var replayPaths = GetHdReplayPaths(config);
        replayPaths.ExceptWith(existingReplayPaths);
        replayPaths.ExceptWith(config.IgnoreReplays);
        return replayPaths.Take(dsstatsConfig.Value.BatchSize).ToList();
    }

    private async Task<HashSet<string>> GetExistingReplayPaths(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        return await context.Replays
            .Where(x => x.FileName != null)
            .Select(x => x.FileName!)
            .ToHashSetAsync(ct);
    }

    private static HashSet<string> GetHdReplayPaths(AppOptions config)
    {
        var folders = GetReplayFolders(config);
        HashSet<string> replayPaths = [];

        List<FileInfo> fileInfos = [];
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }
            var dir = new DirectoryInfo(folder);
            fileInfos.AddRange(dir.GetFiles(
                    $"{config.ReplayStartName}*.SC2Replay",
                    SearchOption.AllDirectories)
                );
        }

        return fileInfos
                .OrderByDescending(o => o.CreationTimeUtc).Select(s => s.FullName)
                .ToHashSet();
    }

    public void Dispose()
    {
        _dbSemaphore.Dispose();
        configSemaphore.Dispose();
    }
}
