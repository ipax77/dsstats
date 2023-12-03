using AutoMapper;
using dsstats.db8;
using dsstats.db8.Extensions;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace dsstats.db8services.Import;

public partial class ImportService
{
    private SemaphoreSlim importSs = new(1, 1);

    public async Task<ImportResult> Import(List<ReplayDto> replayDtos, List<PlayerId>? uploaderPlayerIds = null)
    {
        if (replayDtos.Count == 0)
        {
            return new()
            {
                Error = "No replays found."
            };
        }

        if (!IsInit)
        {
            await Init();
        }

        Dictionary<PlayerId, string> playerInfos = new();

        for (int i = 0; i < replayDtos.Count; i++)
        {
            AdjustReplay(replayDtos[i]);
            foreach (var rp in replayDtos[i].ReplayPlayers)
            {
                var playerId = new PlayerId(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId);
                if (!playerInfos.ContainsKey(playerId))
                {
                    playerInfos[playerId] = rp.Name;
                }
                if (uploaderPlayerIds is not null
                    && uploaderPlayerIds.Contains(playerId))
                {
                    rp.IsUploader = true;
                }
            }
        }

        var playerIds = await GetPlayerIds(playerInfos
            .Select(s => new RequestNames(s.Value, s.Key.ToonId, s.Key.RegionId, s.Key.RealmId))
            .ToList());

        using var scope = serviceProvider.CreateAsyncScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var replays = replayDtos.Select(s => mapper.Map<Replay>(s)).ToList();
        MD5 md5Hash = MD5.Create();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        for (int i = 0; i < replays.Count; i++)
        {
            var replay = replays[i];

            if (!IsMaui)
            {
                foreach (var rp in replay.ReplayPlayers)
                {
                    rp.LastSpawnHash = rp.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All)?.GenHashV3(replay, rp, md5Hash);
                }
            }

            foreach (var rp in replay.ReplayPlayers)
            {
                // rp.LastSpawnHash = rp.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All)?.GenHashV2(rp);

                rp.PlayerId = playerIds[new(rp.Player!.ToonId, rp.Player.RealmId, rp.Player.RegionId)];
                rp.Player = null;

                foreach (var upgrade in rp.Upgrades)
                {
                    upgrade.UpgradeId = GetUpgradeId(upgrade.Upgrade!.Name);
                    upgrade.Upgrade = null;
                }

                foreach (var spawn in rp.Spawns)
                {
                    foreach (var unit in spawn.Units)
                    {
                        unit.UnitId = GetUnitId(unit.Unit!.Name);
                        unit.Unit = null;
                    }
                }
            }
        }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        string? error = null;
        int dups = 0;
        await importSs.WaitAsync();
        try
        {
            dups = await HandleDuplicates(replays, context);

            if (replays.Count > 0)
            {
                DateTime import = DateTime.UtcNow;
                replays.ForEach(f => f.Imported = import);

                context.Replays.AddRange(replays);
                await context.SaveChangesAsync();
                
                await SetPreRatings();
            }
        }
        catch (Exception ex)
        {
            error = ex.Message.ToString();
        }
        finally
        {
            importSs.Release();
        }


        return new()
        {
            Imported = replays.Count,
            Duplicates = dups,
            Error = error
        };
    }
}
