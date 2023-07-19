using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.parser;
using pax.dsstats.shared;
using s2protocol.NET;
using System.Reflection;
using System.Text.RegularExpressions;

namespace dsstats.worker;

public partial class DsstatsService
{
    private async Task<int> Decode(List<string> replayFiles, CancellationToken token)
    {
        await ssDecode.WaitAsync(token);

        await SetUnitsAndUpgrades();
        SetPlayerIds();

        int decoded = 0;

        List<string> errorReplayFileNames = new();
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
                    errorReplayFileNames.Add(decodeResult.ReplayPath);
                    continue;
                }

                try
                {
                    var dsRep = Parse.GetDsReplay(decodeResult.Sc2Replay);

                    if (dsRep == null)
                    {
                        errorReplayFileNames.Add(decodeResult.ReplayPath);
                        continue;
                    }

                    var dtoRep = Parse.GetReplayDto(dsRep);

                    if (dtoRep == null)
                    {
                        errorReplayFileNames.Add(decodeResult.ReplayPath);
                        continue;
                    }

                    _ = SaveReplay(dtoRep);
                    Interlocked.Increment(ref decoded);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Message}", ex.Message);
                    errorReplayFileNames.Add(decodeResult.ReplayPath);
                }
            }
        }
        finally
        {
            ssDecode.Release();
            AddExcludeReplays(errorReplayFileNames);
        }
        return decoded;
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

        foreach (var bnetString in AppConfigOptions.BattlenetStrings)
        {
            var match = BnetIdRegex().Match(bnetString);
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
            decoder = new ReplayDecoder(libPath);
        }
        return decoder;
    }

    private int GetCpuCores()
    {
        return Math.Min(1, AppConfigOptions.CPUCores);
    }

    [GeneratedRegex("^(\\d+)-S2-(\\d+)\\-(\\d+)")]
    private static partial Regex BnetIdRegex();
}