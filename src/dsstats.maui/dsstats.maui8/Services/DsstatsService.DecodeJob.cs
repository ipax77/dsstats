using dsstats.db8services;
using dsstats.shared;
using dsstats.shared.Interfaces;
using pax.dsstats.parser;
using s2protocol.NET;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;

namespace dsstats.maui8.Services;

public partial class DsstatsService
{
    private readonly SemaphoreSlim ssSave = new(1, 1);
    private readonly SemaphoreSlim ssBatch = new(1, 1);
    private readonly int batchSize = 100;
    private HashSet<PlayerId> PlayerIds = new();

    private ReplayDecoderOptions decoderOptions = new()
    {
        Initdata = true,
        Details = true,
        Metadata = true,
        MessageEvents = false,
        TrackerEvents = true,
        GameEvents = false,
        AttributeEvents = false
    };
    private ReplayDecoder? decoder;
    ConcurrentBag<ReplayDto> replayBag = new();

    public async Task Decode(List<string> replayFiles, CancellationToken token, bool singleSave = true)
    {
        try
        {
            SetPlayerIds();
            using var scope = scopeFactory.CreateAsyncScope();
            var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();
            singleSave = configService.AppOptions.NoBatchImport || singleSave;

            var decoder = GetDecoder();
            using var md5hash = MD5.Create();

            await foreach (var decodeResult in
                decoder.DecodeParallelWithErrorReport(replayFiles, threads, decoderOptions, token))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (decodeResult.Sc2Replay == null)
                {
                    OnDecodeStateChanged(new()
                    {
                        DecodeError = new()
                        {
                            ReplayPath = decodeResult.ReplayPath,
                            Error = "Sc2Replay is null."
                        }
                    });
                    continue;
                }

                try
                {
                    var dsRep = Parse.GetDsReplay(decodeResult.Sc2Replay);

                    if (dsRep == null)
                    {
                        OnDecodeStateChanged(new()
                        {
                            DecodeError = new()
                            {
                                ReplayPath = decodeResult.ReplayPath,
                                Error = "DsReplay is null."
                            }
                        });
                        continue;
                    }

                    var dtoRep = Parse.GetReplayDto(dsRep, md5hash);

                    if (dtoRep == null)
                    {
                        OnDecodeStateChanged(new()
                        {
                            DecodeError = new()
                            {
                                ReplayPath = decodeResult.ReplayPath,
                                Error = "DtoReplay is null."
                            }
                        });
                        continue;
                    }

                    await SaveReplay(dtoRep, singleSave);

                    Interlocked.Increment(ref doneDecoding);
                }
                catch (Exception ex)
                {
                    OnDecodeStateChanged(new()
                    {
                        DecodeError = new()
                        {
                            ReplayPath = decodeResult.ReplayPath,
                            Error = ex.Message
                        }
                    });
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            OnDecodeStateChanged(new()
            {
                DecodeError = new()
                {
                    ReplayPath = "Decode job",
                    Error = ex.Message
                }
            });
        }
        await ImportReplays(true);
    }

    private async Task SaveReplay(ReplayDto replayDto, bool singleSave)
    {
        if (singleSave)
        {
            await SaveReplaySingle(replayDto);
        }
        else
        {
            replayBag.Add(replayDto);
            _ = Task.Run(() => ImportReplays());
        }
    }

    private async Task ImportReplays(bool flush = false)
    {
        await ssBatch.WaitAsync();
        try
        {
            List<ReplayDto> errorReplays = [];
            try
            {
                if (replayBag.Count >= batchSize || flush)
                {
                    List<ReplayDto> replaysToSave = [];

                    if (flush)
                    {
                        replaysToSave = replayBag.ToList();
                        replayBag.Clear();
                    }
                    else
                    {
                        for (int i = 0; i < batchSize; i++)
                        {
                            if (replayBag.TryTake(out var replayDto))
                            {
                                replaysToSave.Add(replayDto);
                            }
                        }
                    }

                    if (replaysToSave.Count > 0)
                    {
                        using var scope = scopeFactory.CreateAsyncScope();
                        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
                        var result = await importService.Import(replaysToSave, PlayerIds.ToList());

                        if (!string.IsNullOrEmpty(result.Error))
                        {
                            errorReplays.AddRange(replaysToSave);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnDecodeStateChanged(new()
                {
                    DecodeError = new()
                    {
                        ReplayPath = "Batch save failed.",
                        Error = ex.Message
                    }
                });
            }

            // Retry error replays individually
            if (errorReplays.Count > 0)
            {
                foreach (var errorReplay in errorReplays)
                {
                    await SaveReplaySingle(errorReplay);
                }
            }
        }
        finally
        {
            ssBatch.Release();
        }
    }

    private async Task SaveReplaySingle(ReplayDto replayDto)
    {
        await ssSave.WaitAsync();
        try
        {
            SetIsUploader(replayDto);

            using var scope = scopeFactory.CreateScope();
            var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
            await replayRepository.SaveReplay(replayDto);
        }
        catch (Exception ex)
        {
            OnDecodeStateChanged(new()
            {
                DecodeError = new()
                {
                    ReplayPath = replayDto.FileName,
                    Error = GetImportantErrorMessage(ex)
                }
            });
        }
        finally
        {
            ssSave.Release();
        }
    }

    private void SetIsUploader(ReplayDto replayDto)
    {
        if (PlayerIds.Count > 0)
        {
            foreach (var replayPlayer in replayDto.ReplayPlayers)
            {
                if (PlayerIds.Any(a => a.ToonId == replayPlayer.Player.ToonId
                    && a.RegionId == replayPlayer.Player.RegionId
                    && a.RealmId == replayPlayer.Player.RealmId))
                {
                    replayPlayer.IsUploader = true;
                    replayDto.PlayerResult = replayPlayer.PlayerResult;
                    replayDto.PlayerPos = replayPlayer.GamePos;
                }
            }
        }
    }

    private ReplayDecoder GetDecoder()
    {
        if (decoder == null)
        {
            var _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            decoder = new ReplayDecoder(_assemblyPath);
        }
        return decoder;
    }

    private void SetPlayerIds()
    {
        using var scope = scopeFactory.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();

        PlayerIds = new(configService.AppOptions.ActiveProfiles.Select(s => s.PlayerId));
    }

    private static string GetImportantErrorMessage(Exception ex)
    {
        // Navigate to the innermost exception
        var innerMost = ex;
        while (innerMost.InnerException != null)
        {
            innerMost = innerMost.InnerException;
        }

        // Return the exception type and message
        return $"{innerMost.GetType().Name}: {innerMost.Message}";
    }
}
