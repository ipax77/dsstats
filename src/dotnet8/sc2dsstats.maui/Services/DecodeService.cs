using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.parser;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using s2protocol.NET;
using System.Reflection;
using System.Text.RegularExpressions;

namespace sc2dsstats.maui.Services;

public class DecodeService
{
    private ReplayDecoderOptions decoderOptions;
    private ReplayDecoder? decoder;
    private readonly SemaphoreSlim ssDecode = new(1, 1);
    private readonly SemaphoreSlim ssSave = new(1, 1);
    private readonly IServiceScopeFactory scopeFactory;

    private HashSet<Unit> Units = new();
    private HashSet<Upgrade> Upgrades = new();

    private HashSet<PlayerId> PlayerIds = new();

    public DecodeService(IServiceScopeFactory scopeFactory)
    {
        decoderOptions = new()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            MessageEvents = false,
            TrackerEvents = true,
            GameEvents = false,
            AttributeEvents = false
        };
        this.scopeFactory = scopeFactory;
    }

    internal event EventHandler<DecodeEventArgs>? DecodeStateChanged;
    private void OnDecodeStateChanged(DecodeEventArgs e)
    {
        EventHandler<DecodeEventArgs>? handler = DecodeStateChanged;
        handler?.Invoke(this, e);
    }

    public async Task Decode(List<string> replayFiles, CancellationToken token, bool save = true)
    {
        await ssDecode.WaitAsync(token);

        if (save)
        {
            await SetUnitsAndUpgrades();
            SetPlayerIds();
        }

        try
        {
            var decoder = GetDecoder();
            var cpuCores = GetCpuCores();


            await foreach (var decodeResult in
                decoder.DecodeParallelWithErrorReport(replayFiles, cpuCores, decoderOptions, token))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (decodeResult.Sc2Replay == null)
                {
                    OnDecodeStateChanged(new()
                    {
                        ReplayPath = decodeResult.ReplayPath,
                        Error = decodeResult.Exception ?? "unknown"
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
                            ReplayPath = decodeResult.ReplayPath,
                            Error = "dsRep is null"
                        });
                        continue;
                    }

                    var dtoRep = Parse.GetReplayDto(dsRep);

                    if (dtoRep == null)
                    {
                        OnDecodeStateChanged(new()
                        {
                            ReplayPath = decodeResult.ReplayPath,
                            Error = "dtoRep is null"
                        });
                        continue;
                    }

                    if (save)
                    {
                        _ = SaveReplay(dtoRep);
                    }

                    OnDecodeStateChanged(new()
                    {
                        ReplayPath = decodeResult.ReplayPath,
                        ReplayDto = dtoRep
                    });
                }
                catch (Exception ex)
                {
                    OnDecodeStateChanged(new()
                    {
                        ReplayPath = decodeResult.ReplayPath,
                        Error = ex.Message
                    });
                }
            }
        }
        finally
        {
            ssDecode.Release();
            OnDecodeStateChanged(new() { Done = true });
        }
    }

    private async Task SaveReplay(ReplayDto replayDto)
    {
        await ssSave.WaitAsync();
        try
        {
            SetIsUploader(replayDto);

            using var scope = scopeFactory.CreateScope();
            var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
            (Units, Upgrades, var replay) = await replayRepository.SaveReplay(replayDto, Units, Upgrades, null);
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

    private void SetPlayerIds()
    {
        if (PlayerIds.Count > 0)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();

        foreach (var bnetString in configService.AppConfigOptions.BattlenetStrings)
        {
            var match = Regex.Match(bnetString, @"^(\d+)-S2-(\d+)\-(\d+)");
            if (match.Success)
            {
                int regionId = int.Parse(match.Groups[1].Value);
                int realmId = int.Parse(match.Groups[2].Value);
                int toonId = int.Parse(match.Groups[3].Value);
                PlayerIds.Add(new(toonId, realmId, regionId));
            }
        }
    }

    private async Task SetUnitsAndUpgrades()
    {
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

    private int GetCpuCores()
    {
        using var scope = scopeFactory.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();
        return Math.Min(1, configService.AppConfigOptions.CPUCores);
    }
}

internal class DecodeEventArgs : EventArgs
{
    public string ReplayPath { get; init; } = string.Empty;
    public ReplayDto? ReplayDto { get; init; }
    public string? Error { get; init; }
    public bool Done { get; init; }
}