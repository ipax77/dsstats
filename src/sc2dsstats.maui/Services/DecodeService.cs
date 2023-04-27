using AutoMapper;
using Blazored.Toast.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng.Services.Ratings;
using pax.dsstats.parser;
using pax.dsstats.shared;
using s2protocol.NET;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace sc2dsstats.maui.Services;

public partial class DecodeService : IDisposable
{
    public DecodeService(ILogger<DecodeService> logger, IMapper mapper, IServiceScopeFactory serviceScopeFactory)
    {
        this.mapper = mapper;
        this.logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;

        decoderOptions = new ReplayDecoderOptions()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            MessageEvents = false,
            TrackerEvents = true,
            GameEvents = false,
            AttributeEvents = false
        };

        if (UserSettingsService.UserSettings.AutoScanForNewReplays)
        {
            WatchService = new WatchService();
            WatchService.WatchForNewReplays();
            WatchService.NewFileDetected += WatchService_NewFileDetected;
        }

        SetSessionStart();
    }

    private void WatchService_NewFileDetected(object? sender, ReplayDetectedEventArgs e)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var toastService = scope.ServiceProvider.GetRequiredService<IToastService>();
        toastService.ShowWarning("New replay detected");
        _ = DecodeParallel();
    }

    public int NewReplays { get; private set; }
    public int DbReplays { get; private set; }

    private readonly IMapper mapper;
    private readonly ILogger<DecodeService> logger;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ReplayDecoderOptions decoderOptions;

    private ReplayDecoder? decoder;
    public WatchService? WatchService { get; private set; }

    public ConcurrentDictionary<string, string> errorReplays { get; private set; } = new();

    private SemaphoreSlim semaphoreSlim = new(1, 1);
    private HashSet<Unit> Units = new();
    private HashSet<Upgrade> Upgrades = new();

    private int decodeCounter;
    private int dbCounter;
    private int total;
    private int errorCounter;
    private DateTime startTime = DateTime.UtcNow;

    private object lockobject = new object();

    private CancellationTokenSource? decodeCts = null;
    private CancellationTokenSource? notifyCts = null;
    public bool IsRunning { get; private set; }

    public event EventHandler<DecodeEventArgs>? DecodeStateChanged;
    protected virtual void OnDecodeStateChanged(DecodeEventArgs e)
    {
        EventHandler<DecodeEventArgs>? handler = DecodeStateChanged;
        handler?.Invoke(this, e);
    }

    public event EventHandler<ScanEventArgs>? ScanStateChanged;
    protected virtual void OnScanStateChanged(ScanEventArgs e)
    {
        EventHandler<ScanEventArgs>? handler = ScanStateChanged;
        handler?.Invoke(this, e);
    }
    public event EventHandler<EventArgs>? ErrorRaised;
    protected virtual void OnErrorRaised(EventArgs e)
    {
        EventHandler<EventArgs>? handler = ErrorRaised;
        handler?.Invoke(this, e);
    }

    public async Task DecodeParallel()
    {
        lock (lockobject)
        {
            if (IsRunning)
            {
                return;
            }
            IsRunning = true;
        }

        decodeCounter = 0;
        dbCounter = 0;
        errorReplays.Clear();
        errorCounter = 0;
        DateTime latestReplay = DateTime.MinValue;

        var replays = await ScanForNewReplays(true);

        if (!replays.Any())
        {
            OnDecodeStateChanged(new()
            {
                Start = startTime,
                Total = total,
                Decoded = decodeCounter,
                Error = errorCounter,
                Saved = dbCounter,
                Done = true
            });
            IsRunning = false;
            return;
        }

        total = replays.Count;
        startTime = DateTime.UtcNow;

        decodeCts = new();
        notifyCts = new();
        _ = Notify();

        Stopwatch sw = Stopwatch.StartNew();

        if (decoder == null)
        {
            var _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            decoder = new ReplayDecoder(_assemblyPath);
        }

        try
        {
            await foreach (var decodeResult in decoder.DecodeParallelWithErrorReport(replays, UserSettingsService.UserSettings.CpuCoresUsedForDecoding, decoderOptions, decodeCts.Token))
            {
                if (decodeCts.IsCancellationRequested)
                {
                    break;
                }

                if (decodeResult.Sc2Replay == null)
                {
                    logger.DecodeError($"failed decoding replay {decodeResult.ReplayPath}: {decodeResult.Exception}");
                    errorReplays[decodeResult.ReplayPath] = decodeResult.Exception ?? "unknown";
                    Interlocked.Increment(ref errorCounter);
                    OnErrorRaised(new());
                    continue;
                }

                try
                {
                    var dsRep = Parse.GetDsReplay(decodeResult.Sc2Replay);
                    if (dsRep != null)
                    {
                        var dtoRep = Parse.GetReplayDto(dsRep);
                        if (dtoRep != null)
                        {
                            Interlocked.Increment(ref decodeCounter);
                            _ = SaveReplay(dtoRep);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.DecodeError($"failed parsing replay {decodeResult.ReplayPath}: {ex.Message}");
                    errorReplays[decodeResult.ReplayPath] = ex.Message;
                    Interlocked.Increment(ref errorCounter);
                    OnErrorRaised(new());
                }

                if (decodeCounter % 100 == 0)
                {
                    logger.DecodeInformation($"replays decoded: {decodeCounter}/{total}, replays in db: {dbCounter}/{total}");
                }
            }

            // report missing replays
            if (dbCounter != replays.Count - errorCounter)
            {
                using var scope = serviceScopeFactory.CreateScope();
                var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

                var dbPaths = await replayRepository.GetReplayPaths();
                var failedReplays = replays.Except(dbPaths).ToList();
                for (int i = 0; i < failedReplays.Count; i++)
                {
                    errorReplays[failedReplays[i]] = "unknown";
                    Interlocked.Increment(ref errorCounter);
                }
                OnErrorRaised(new());
            }

        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.DecodeError($"failed decoding replays: {ex.Message}");
            errorReplays["replays"] = ex.Message;
            OnErrorRaised(new());
        }
        finally
        {
            sw.Stop();

            logger.DecodeInformation($"Got dsReplays in {sw.ElapsedMilliseconds} ms");

            await ScanForNewReplays();

            using var scope = serviceScopeFactory.CreateScope();
            var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

            var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
            statsService.ResetStatsCache();

            await ratingsService.ProduceRatings();

            notifyCts.Cancel();

            IsRunning = false;
            decodeCts.Dispose();
            decodeCts = null;

            if (UserSettingsService.UserSettings.AllowUploads)
            {
                var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();
                _ = uploadService.UploadReplays();
            }
        }
    }

    private List<int> GetPlayerToonIds(Replay replay)
    {
        return replay.ReplayPlayers.Select(s => s.Player.ToonId).ToList();
    }

    public void StopDecoding()
    {
        decodeCts?.Cancel();
    }

    private async Task Notify()
    {
        while (notifyCts != null && !notifyCts.IsCancellationRequested)
        {
            OnDecodeStateChanged(new()
            {
                Start = startTime,
                Total = total,
                Decoded = decodeCounter,
                Error = errorCounter,
                Saved = dbCounter,
            });
            try
            {
                await Task.Delay(1000, notifyCts.Token);
            }
            catch (OperationCanceledException) { }
        }

        OnDecodeStateChanged(new()
        {
            Start = startTime,
            Total = total,
            Decoded = decodeCounter,
            Error = errorCounter,
            Saved = dbCounter,
            Done = true,
        });

        notifyCts?.Dispose();
        notifyCts = null;
    }

    internal async Task<List<string>> ScanForNewReplays(bool ordered = false)
    {
        Stopwatch sw = new();
        sw.Start();

        using var scope = serviceScopeFactory.CreateScope();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        var dbReplayPaths = await replayRepository.GetReplayPaths();
        var hdReplayPaths = GetHdReplayPaths(ordered);

        var newReplays = hdReplayPaths.Except(dbReplayPaths).ToList();

        sw.Stop();
        logger.DecodeInformation($"got new replays list (db: {dbReplayPaths.Count}, hd: {hdReplayPaths.Count} => {newReplays.Count}) in {sw.ElapsedMilliseconds} ms");

        NewReplays = newReplays.Count;
        DbReplays = dbReplayPaths.Count;

        OnScanStateChanged(new() { NewReplays = NewReplays, DbReplays = DbReplays });

        return newReplays;
    }

    private async Task<DateTime> GetLatestReplayTime()
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Replays
            .OrderByDescending(o => o.GameTime)
            .Select(s => s.GameTime)
            .FirstOrDefaultAsync();
    }

    private ICollection<string> GetHdReplayPaths(bool ordered)
    {
        if (ordered)
        {
            List<KeyValuePair<string, DateTime>> files = new();
            foreach (var dir in UserSettingsService.UserSettings.ReplayPaths)
            {
                DirectoryInfo info = new DirectoryInfo(dir);
                files.AddRange(info.GetFiles($"{UserSettingsService.UserSettings.ReplayStartName}*.SC2Replay", SearchOption.AllDirectories)
                    .OrderBy(p => p.CreationTime)
                    .Select(s => new KeyValuePair<string, DateTime>(s.FullName, s.CreationTime)));
            }
            return files.OrderBy(o => o.Value).Select(s => s.Key).ToList();
        }
        else
        {

            List<string> fileNames = new();
            foreach (var dir in UserSettingsService.UserSettings.ReplayPaths)
            {
                fileNames.AddRange(Directory.GetFiles(dir, $"{UserSettingsService.UserSettings.ReplayStartName}*.SC2Replay", SearchOption.AllDirectories));
            }
            return fileNames;
        }
    }

    private void SetIsUploader(ReplayDto replayDto)
    {
        var playerToonIds = UserSettingsService.UserSettings.BattleNetInfos?.SelectMany(s => s.ToonIds);

        if (playerToonIds != null && playerToonIds.Any())
        {
            for (int i = 0; i < replayDto.ReplayPlayers.Count; i++)
            {
                var replayPlayer = replayDto.ReplayPlayers.ElementAt(i);
                if (playerToonIds.Any(a => 
                    a.ToonId == replayPlayer.Player.ToonId
                    && a.RealmId == replayPlayer.Player.RealmId
                    && a.RegionId == replayPlayer.Player.RegionId))
                {
                    replayPlayer.IsUploader = true;
                    replayDto.PlayerResult = replayPlayer.PlayerResult;
                    replayDto.PlayerPos = replayPlayer.GamePos;
                }
            }
        }
        else
        {
            for (int i = 0; i < replayDto.ReplayPlayers.Count; i++)
            {
                var player = replayDto.ReplayPlayers.ElementAt(i);
                if (UserSettingsService.UserSettings.PlayerNames.Contains(player.Name))
                {
                    player.IsUploader = true;
                    replayDto.PlayerResult = player.PlayerResult;
                    replayDto.PlayerPos = player.GamePos;
                }
            }
        }
    }

    private async Task SaveReplay(ReplayDto replayDto)
    {
        await semaphoreSlim.WaitAsync();
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            if (!Units.Any())
            {
                Units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
            }

            if (!Upgrades.Any())
            {
                Upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();
            }

            SetIsUploader(replayDto);

            var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
            (Units, Upgrades, var replay) = await replayRepository.SaveReplay(replayDto, Units, Upgrades, null);

            Interlocked.Increment(ref dbCounter);
        }
        catch (Exception ex)
        {
            logger.DecodeError($"failed saving replay: {ex.Message}");
            errorReplays[replayDto.FileName] = ex.Message + ex.InnerException?.Message;
            Interlocked.Increment(ref errorCounter);
            OnErrorRaised(new());
        }
        finally
        {
            if (!IsRunning)
            {
                OnDecodeStateChanged(new()
                {
                    Start = startTime,
                    Total = total,
                    Decoded = decodeCounter,
                    Saved = dbCounter,
                    Done = true
                });
            }
            semaphoreSlim.Release();
        }
    }

    public void DEBUGEmulateErrorsTest()
    {
        List<string> errors = new List<string>()
        {
            @"C:\data\ds\replays\test1.SC2Replay",
            @"C:\data\ds\replays\test2.SC2Replay",
            @"C:\data\ds\replays\test3.SC2Replay",
            @"C:\data\ds\replays\test4.SC2Replay",
            @"C:\data\ds\replays\test5.SC2Replay",
            @"C:\data\ds\replays\test6.SC2Replay",
            @"C:\data\ds\replays\test7.SC2Replay",
            @"C:\data\ds\replays\test8.SC2Replay",
            @"C:\data\ds\replays\test9.SC2Replay",
            @"C:\data\ds\replays\test10.SC2Replay",
            @"C:\data\ds\replays\test11.SC2Replay",
            @"C:\data\ds\replays\test12.SC2Replay",
            @"C:\data\ds\replays\test13.SC2Replay",
            @"C:\data\ds\replays\test14.SC2Replay",
            @"C:\data\ds\replays\test15.SC2Replay",
            @"C:\data\ds\replays\test16.SC2Replay",
        };

        foreach (var errorReplay in errors)
        {
            errorReplays[errorReplay] = "debuge test error";
            Interlocked.Increment(ref errorCounter);
            OnErrorRaised(new());
        }
        OnDecodeStateChanged(new()
        {
            Start = DateTime.UtcNow.AddMinutes(-5),
            Total = 100,
            Decoded = 70,
            Saved = 69,
            Error = errorCounter,
            Done = false,
        });
    }

    public void DEBUGDeleteLatestReplay()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replay = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
            .OrderByDescending(o => o.GameTime)
            .Take(1)
            .AsSplitQuery()
            .FirstOrDefault();

        if (replay != null)
        {
            context.Replays.Remove(replay);
            context.SaveChanges();
        }
    }

    public void Dispose()
    {
        notifyCts?.Cancel();
        decodeCts?.Cancel();
        notifyCts?.Dispose();
        decodeCts?.Dispose();

        if (WatchService != null)
        {
            WatchService.NewFileDetected -= WatchService_NewFileDetected;
            WatchService.Dispose();
        }
        decoder?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class ScanEventArgs : EventArgs
{
    public int NewReplays { get; init; }
    public int DbReplays { get; init; }
}

public class DecodeEventArgs : EventArgs
{
    public DateTime Start { get; set; }
    public int Total { get; set; }
    public int Decoded { get; set; }
    public int Error { get; set; }
    public int Saved { get; set; }
    public bool Done { get; set; }
}
