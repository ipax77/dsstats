using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using System.Diagnostics;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class RatingsService
{
    public async Task NoUploadAdjustment()
    {
        Stopwatch sw = Stopwatch.StartNew();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var dueDate = DateTime.UtcNow.AddDays(-7);

        var noUploadPlayers = await context.Uploaders
            .Where(x => x.LatestUpload < dueDate)
            .Select(s => new NoUplaodPlayer()
            {
                LatestUplaod = s.LatestUpload,
                PlayerIds = s.Players.Select(t => new PlayerId()
                {
                    ToonId = t.ToonId,
                    RegionId = t.RegionId,
                    RealmId = t.RealmId,
                }).ToList()
            })
            .ToListAsync();

        logger.LogInformation("noupload player count: {count}", noUploadPlayers.Count);

        var playerIds = noUploadPlayers.SelectMany(s => s.PlayerIds).ToList();

        List<IQueryable<ArcadePlayerLosResult>> mQueries = new();

        foreach (var noUploadPlayer in noUploadPlayers)
        {
            var latestUpload = noUploadPlayer.LatestUplaod.AddHours(1);
            latestUpload = new DateTime(latestUpload.Year, latestUpload.Month, latestUpload.Day, latestUpload.Hour, 0, 0);

            foreach (var player in noUploadPlayer.PlayerIds)
            {
                
                var arcadeReplays = from r in context.ArcadeReplays
                                    from rp in r.ArcadeReplayPlayers
                                    where r.CreatedAt > latestUpload
                                     && rp.ArcadePlayer.ProfileId == player.ToonId
                                     && rp.ArcadePlayer.RegionId == player.RegionId
                                     && rp.ArcadePlayer.RealmId == player.RealmId
                                     group rp by new { rp.PlayerResult } into g
                                    select new ArcadePlayerLosResult()
                                    {
                                        ProfileId = player.ToonId,
                                        RegionId = player.RegionId,
                                        RealmId = player.RealmId,
                                        PlayerResult = g.Key.PlayerResult,
                                        Count = g.Count()
                                    };
                mQueries.Add(arcadeReplays);
            }
        }

        List<ArcadePlayerLosResult> results = new();
        if (mQueries.Count > 0)
        {
            var query = mQueries.First();

            for (int i = 1; i < mQueries.Count; i++)
            {
                query = query.Concat(mQueries[i]);
            }

            results = await query.ToListAsync();
            results = results.Where(x => x.PlayerResult == PlayerResult.Los && x.Count > 0)
                .ToList();
        }

        if (results.Count > 0)
        {
            var resultToonIds = results.Select(s => s.ProfileId).ToList();
            var players = await context.Players
                .Where(x => resultToonIds.Contains(x.ToonId))
                .ToListAsync();

            foreach (var result in results)
            {
                var player = players.FirstOrDefault(f => f.ToonId == result.ProfileId
                    && f.RegionId == result.RegionId
                    && f.RealmId == result.RealmId);

                if (player is not null)
                {
                    player.NotUploadCount = result.Count;
                }
            }
            await context.SaveChangesAsync();
        }

        sw.Stop();
        logger.LogWarning("Noupload adjustments in {time} ms", sw.ElapsedMilliseconds);

        Console.WriteLine("indahouse");
    }


    private record ArcadePlayerLosResult
    {
        public int ProfileId { get; init; }
        public int RegionId { get; init; }
        public int RealmId { get; init; }
        public PlayerResult PlayerResult { get; init; } = new();
        public int Count { get; init; }
    }

    private record NoUplaodPlayer
    {
        public DateTime LatestUplaod { get; set; }
        public List<PlayerId> PlayerIds { get; set; } = new();
    }
}


