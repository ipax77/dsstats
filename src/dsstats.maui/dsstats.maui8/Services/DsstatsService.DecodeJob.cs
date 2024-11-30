using dsstats.db8;
using dsstats.db8services;
using dsstats.db8services.Import;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.parser;
using s2protocol.NET;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;

namespace dsstats.maui8.Services;

public partial class DsstatsService
{
    private readonly SemaphoreSlim ssSave = new(1, 1);
    private HashSet<Unit> Units = new();
    private HashSet<Upgrade> Upgrades = new();
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
        SetPlayerIds();
        if (singleSave)
        {
            await SetUnitsAndUpgrades();
        }

        try
        {
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

                    _ = SaveReplay(dtoRep, singleSave);

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
    }

    private async Task SaveReplay(ReplayDto replayDto, bool singleSave)
    {
        if (singleSave)
        {
            await ssSave.WaitAsync();
            try
            {
                SetIsUploader(replayDto);

                using var scope = scopeFactory.CreateScope();
                var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
                await replayRepository.SaveReplay(replayDto, Units, Upgrades);
            }
            catch (Exception ex)
            {
                OnDecodeStateChanged(new()
                {
                    DecodeError = new()
                    {
                        ReplayPath = replayDto.FileName,
                        Error = ex.Message
                    }
                });
            }
            finally
            {
                ssSave.Release();
            }
        }
        else
        {
            replayBag.Add(replayDto);
            _ = ImportReplays();
        }
    }

    private async Task ImportReplays(bool final = false)
    {
        await ssSave.WaitAsync();
        List<ReplayDto> errorReplays = [];
        try
        {
            if (replayBag.Count >= 100 || final)
            {
                using var scope = scopeFactory.CreateAsyncScope();
                var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

                List<ReplayDto> replays = new();
                if (final)
                {
                    replays = replayBag.ToList();
                    replayBag.Clear();
                }
                else
                {
                    for (int i = 0; i < 100; i++)
                    {
                        if (replayBag.TryTake(out var replayDto))
                        {
                            replays.Add(replayDto);
                        }
                    }
                }
                var result = await importService.Import(replays, PlayerIds.ToList());
                if (!string.IsNullOrEmpty(result.Error))
                {
                    errorReplays.AddRange(replays);
                }

                if (final)
                {
                    replayBag.Clear();
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
        finally
        {
            ssSave.Release();
        }

        if (errorReplays.Count > 0)
        {
            foreach (var errorReplay in errorReplays)
            {
                await SaveReplay(errorReplay, true);
            }
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

    private void SetPlayerIds()
    {
        using var scope = scopeFactory.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();

        PlayerIds = new(configService.AppOptions.ActiveProfiles.Select(s => s.PlayerId));
    }

    private async Task SetUnitsAndUpgrades()
    {
        if (Units.Count > 0 && Upgrades.Count > 0)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if (Units.Count == 0)
        {
            Units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        }

        if (Upgrades.Count == 0)
        {
            Upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();
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
}
