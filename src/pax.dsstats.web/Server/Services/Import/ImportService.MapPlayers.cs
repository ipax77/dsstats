
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;

namespace pax.dsstats.web.Server.Services.Import;

public partial class ImportService
{
    private async Task<int> MapPlayers(List<Replay> replays, int uploaderId)
    {
        int newPlayers = await CreateMissingPlayers(replays);
        MapReplayPlayerPlayers(replays, uploaderId);
        return newPlayers;
    }

    private async void MapReplayPlayerPlayers(List<Replay> replays, int uploaderId)
    {
        Uploader? uploader = null;
        for (int i = 0; i < replays.Count; i++)
        {
            ReplayPlayer? uploaderPlayer = null;
            Dictionary<int, shared.Arcade.PlayerId> playerIds = new();

            foreach (var rp in replays[i].ReplayPlayers)
            {
                if (rp.Player == null)
                {
                    continue;
                }

                if (rp.IsUploader)
                {
                    uploaderPlayer = rp;
                }

                var playerId = dbCache.Players[new(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId)];
                playerIds[playerId] = new(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId);

                // todo: fix regionId !?

                rp.PlayerId = playerId;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                rp.Player = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }

            if (uploaderPlayer == null)
            {
                uploader = await SetUploader(replays[i], uploader, uploaderId, playerIds);
            }
        }
    }

    private async Task<Uploader?> SetUploader(Replay replay,
                                              Uploader? uploader,
                                              int uploaderId,
                                              Dictionary<int, shared.Arcade.PlayerId> playerIds)
    {
        if (uploader == null)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            uploader = await context.Uploaders
                .Include(i => i.Players)
                .FirstOrDefaultAsync(f => f.UploaderId == uploaderId);
        }

        if (uploader == null)
        {
            return null;
        }

        foreach (var rp in replay.ReplayPlayers)
        {
            if (playerIds.TryGetValue(rp.PlayerId, out var playerId)
                && uploader.Players.Any(a => a.ToonId == playerId.ToonId
                    && a.RegionId == playerId.RegionId
                    && a.RealmId == playerId.RealmId))
            {
                rp.IsUploader = true;
            }
        }

        return uploader;
    }

    private async Task<int> CreateMissingPlayers(List<Replay> replays)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<Player> newPlayers = new();

        for (int i = 0; i < replays.Count; i++)
        {
            var replay = replays[i];
            int regionId = 0;

            foreach (var rp in replay.ReplayPlayers)
            {
                if (rp.Player.RealmId == 0)
                {
                    rp.Player.RealmId = 1;

                    if (regionId == 0)
                    {
                        regionId = GetReplayRegion(replay);
                    }

                    rp.Player.RegionId = regionId;
                }

                if (!dbCache.Players.ContainsKey(new(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId)))
                {
                    Player player = new()
                    {
                        Name = rp.Player.Name,
                        ToonId = rp.Player.ToonId,
                        RegionId = rp.Player.RegionId,
                        RealmId = rp.Player.RealmId,
                    };
                    context.Players.Add(player);
                    newPlayers.Add(player);
                    if (newPlayers.Count % 1000 == 0)
                    {
                        await context.SaveChangesAsync();
                    }
                    dbCache.Players[new(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId)] = 0;
                }
            }
        }

        await context.SaveChangesAsync();
        newPlayers.ForEach(f => dbCache.Players[new(f.ToonId, f.RealmId, f.RegionId)] = f.PlayerId);
        return newPlayers.Count;
    }

    public async Task<int> CreatePlayer(Player player)
    {
        if (!dbCache.Players.TryGetValue(new(player.ToonId, player.RealmId, player.RegionId), out int playerId))
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            context.Players.Add(player);
            await context.SaveChangesAsync();
            playerId = dbCache.Players[new(player.ToonId, player.RealmId, player.RegionId)] = player.PlayerId;
        }
        return playerId;
    }

    public async Task<int> CreateUploader(Uploader uploader)
    {
        if (!dbCache.Uploaders.TryGetValue(uploader.AppGuid, out int uploaderId))
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            context.Uploaders.Add(uploader);
            await context.SaveChangesAsync();

            uploaderId = dbCache.Uploaders[uploader.AppGuid] = uploader.UploaderId;
        }
        return uploaderId;
    }

    public int GetReplayRegion(Replay replay)
    {
        Dictionary<int, int> regionCounts = new()
        {
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
        };

        foreach (var rp in replay.ReplayPlayers)
        {
            if (rp.Player.RegionId <= 0 || rp.Player.RegionId > 3)
            {
                logger.LogWarning($"Unknown player region found {replay.ReplayHash} => Pos: {rp.GamePos}, Region: {rp.Player.RegionId}, {rp.PlayerId}");
                continue;
            }
            regionCounts[rp.Player.RegionId]++;
        }

        return regionCounts.OrderByDescending(o => o.Value).First().Key;
    }
}