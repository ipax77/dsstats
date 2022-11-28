
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace dsstats.raven;

public partial class RatingRepository
{
    public async Task<List<PlChange>> GetReplayPlayerMmrChanges(string replayHash, CancellationToken token = default)
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var result = await session.Query<RavenMmrChange>()
            .Where(x => x.Id == $"RavenMmrChange/{replayHash}")
            .FirstOrDefaultAsync(token);

        if (result == null)
        {
            return new();
        }

        return result.Changes;
    }

    public async Task SetReplayListMmrChanges(List<ReplayListDto> replays, CancellationToken token = default)
    {
        var ids = replays.Select(s => $"RavenMmrChange/{s.ReplayHash}").ToList();

        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var results = await session.Query<RavenMmrChange>()
            .Where(x => x.Id.In(ids))
            .ToListAsync(token);

        replays.ForEach(f =>
        {
            var result = results.FirstOrDefault(g => g.Id.EndsWith(f.ReplayHash));
            f.MmrChange = Math.Round(result?.Changes.FirstOrDefault(g => g.Pos == f.PlayerPos)?.Change ?? 0, 2);
        });
    }

    public async Task<ToonIdsRatingsResponse> GetToonIdsRatings(List<int> toonIds)
    {
        ToonIdsRatingsResponse response = new();

        List<int> toonIdsFomrDb = new();

        for (int i = 0; i < Math.Min(15, toonIds.Count); i++)
        {
            if (RatingMemory.TryGetValue(toonIds[i], out var toonIdRating))
            {
                ToonIdRatingInfo ratingInfo = new()
                {
                    ToonId = toonIds[i],

                };

                if (toonIdRating.CmdrPlayer != null)
                {
                    ratingInfo.Name = toonIdRating.CmdrPlayer.Name;
                    ratingInfo.Ratings.Add(new()
                    {
                        RatingType = RatingType.Cmdr,
                        RegionId = toonIdRating.CmdrPlayer.RegionId,
                        Mmr = Math.Round(toonIdRating.CmdrPlayer.Rating.Mmr, 2)
                    });
                }
                if (toonIdRating.StdPlayer != null)
                {
                    ratingInfo.Name = toonIdRating.StdPlayer.Name;
                    ratingInfo.Ratings.Add(new()
                    {
                        RatingType = RatingType.Cmdr,
                        RegionId = toonIdRating.StdPlayer.RegionId,
                        Mmr = Math.Round(toonIdRating.StdPlayer.Rating.Mmr, 2)
                    });
                }
                response.RatingInfos.Add(ratingInfo);
            }
            else
            {
                toonIdsFomrDb.Add(toonIds[i]);
            }
        }

        //if (toonIdsFomrDb.Any())
        //{
        //    // todo
        //}

        return await Task.FromResult(response);
    }
}

