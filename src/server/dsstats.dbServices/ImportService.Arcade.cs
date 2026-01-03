using dsstats.db;
using dsstats.shared;
using dsstats.shared.Arcade;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.dbServices;

public partial class ImportService
{
    HashSet<ArcadeReplayKey> existingArcadeReplayKeys = [];

    public void ClearExistingArcadeReplayKeys()
    {
        existingArcadeReplayKeys.Clear();
    }

    public async Task ImportArcadeReplays(List<ArcadeReplayDto> replays)
    {
        Init();

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        if (existingArcadeReplayKeys.Count == 0)
        {
            existingArcadeReplayKeys = (await context.ArcadeReplays
                .Select(s => new ArcadeReplayKey(s.RegionId, s.BnetBucketId, s.BnetRecordId))
                .ToListAsync()).ToHashSet();

        }

        Dictionary<ToonIdRec, string> players = [];
        foreach (var player in replays.SelectMany(s => s.Players))
        {
            var key = new ToonIdRec(player.Player.ToonId.Region, player.Player.ToonId.Realm, player.Player.ToonId.Id);
            players[key] = player.Player.Name;
        }

        await CreatePlayerIds(players);

        List<ArcadeReplay> arcadeReplays = [];
        foreach (var replay in replays)
        {
            var replayKey = replay.GetKey();
            if (!existingArcadeReplayKeys.Contains(replayKey))
            {
                arcadeReplays.Add(replay.ToEntity(toonIdPlayerIdDict));
                existingArcadeReplayKeys.Add(replayKey);
            }
        }

        await context.ArcadeReplays.AddRangeAsync(arcadeReplays);
        await context.SaveChangesAsync();
    }

    public async Task ImportArcadeReplaysRaw(List<ArcadeReplayDto> replays)
    {
        Init();

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        Dictionary<ToonIdRec, string> players = [];
        foreach (var player in replays.SelectMany(s => s.Players))
        {
            var key = new ToonIdRec(player.Player.ToonId.Region, player.Player.ToonId.Realm, player.Player.ToonId.Id);
            players[key] = player.Player.Name;
        }

        await CreatePlayerIds(players);

        List<ArcadeReplay> arcadeReplays = replays.Select(s => s.ToEntity(toonIdPlayerIdDict)).ToList();


        await context.ArcadeReplays.AddRangeAsync(arcadeReplays);
        await context.SaveChangesAsync();
    }
}
