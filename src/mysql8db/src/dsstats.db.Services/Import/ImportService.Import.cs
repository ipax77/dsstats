using AutoMapper;
using dsstats.db.Extensions;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace dsstats.db.Services.Import;

public partial class ImportService
{
    private readonly SemaphoreSlim importSs = new(1, 1);

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

        Dictionary<PlayerId, string> playerInfos = [];

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
                    rp.LastSpawnHash = rp.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All)?.GenHash(replay, rp, md5Hash);
                }
            }

            foreach (var rp in replay.ReplayPlayers)
            {
                rp.PlayerId = playerIds[new(rp.Player!.ToonId, rp.Player.RealmId, rp.Player.RegionId)];
                rp.Player = null;

                foreach (var upgrade in rp.PlayerUpgrades)
                {
                    upgrade.UpgradeId = GetUpgradeId(upgrade.Upgrade!.Name);
                    upgrade.Upgrade = null;
                }

                foreach (var spawn in rp.Spawns)
                {
                    foreach (var unit in spawn.SpawnUnits)
                    {
                        unit.UnitId = GetUnitId(unit.Unit!.Name);
                        unit.Unit = null;
                    }
                }
            }
        }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

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

                await context.Replays.AddRangeAsync(replays);
                await context.SaveChangesAsync();

                foreach (var replay in replays)
                {
                    foreach (var replayPlayer in replay.ReplayPlayers)
                    {
                        replayPlayer.Opponent = replayPlayer.GamePos switch
                        {
                            1 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 4),
                            2 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 5),
                            3 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 6),
                            4 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 1),
                            5 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 2),
                            6 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 3),
                            _ => null
                        };
                    }
                }
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
