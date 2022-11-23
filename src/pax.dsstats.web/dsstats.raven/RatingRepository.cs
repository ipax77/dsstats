using pax.dsstats.shared;
using pax.dsstats.shared.Raven;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace dsstats.raven;

public partial class RatingRepository : IRatingRepository
{
    public RatingRepository()
    {

    }

    public async Task<RavenPlayerDetailsDto> GetPlayerDetails(int toonId, CancellationToken token = default)
    {
        //var players = GetPlayerRatingFromMemory(toonId);

        //if (players.Any())
        //{
        //    return players;
        //}

        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var player = await session.Query<RavenPlayer>()
            .Where(x => x.Id == $"RavenPlayer/{toonId}")
            .FirstOrDefaultAsync(token);

        var cmdrRating = await session.Query<RavenRating>()
            .Where(x => x.Id == $"RavenRating/{RatingType.Cmdr}/{toonId}")
            .FirstOrDefaultAsync(token);

        var stdRating = await session.Query<RavenRating>()
            .Where(x => x.Id == $"RavenRating/{RatingType.Std}/{toonId}")
            .FirstOrDefaultAsync(token);

        RavenPlayerDetailsDto dto = new()
        {
            Name = player.Name,
            ToonId = player.ToonId,
            RegionId = player.RegionId,
            IsUploader = player.IsUploader,
        };

        if (cmdrRating != null)
        {
            dto.Ratings.Add(new()
            {
                Type = cmdrRating.Type,
                Games = cmdrRating.Games,
                Wins = cmdrRating.Wins,
                Mvp = cmdrRating.Mvp,
                TeamGames = cmdrRating.TeamGames,
                Main = cmdrRating.Main,
                MainPercentage = cmdrRating.MainPercentage,
                Mmr = cmdrRating.Mmr,
                MmrOverTime = cmdrRating.MmrOverTime,
            });
        }

        if (stdRating != null)
        {
            dto.Ratings.Add(new()
            {
                Type = stdRating.Type,
                Games = stdRating.Games,
                Wins = stdRating.Wins,
                Mvp = stdRating.Mvp,
                TeamGames = stdRating.TeamGames,
                Main = stdRating.Main,
                MainPercentage = stdRating.MainPercentage,
                Mmr = stdRating.Mmr,
                MmrOverTime = stdRating.MmrOverTime,
            });
        }
        return dto;
    }

    public async Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token)
    {
        return await Task.FromResult(GetRatingsFromMemory(request));
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var results = await session.Query<RatingCmdr_Average_ByMmr.Result, RatingCmdr_Average_ByMmr>()
            .OrderBy(o => o.Mmr)
            .ToListAsync();

        return results.Select(s => new MmrDevDto()
        {
            Mmr = s.Mmr,
            Count = s.Count
        }).ToList();
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var results = await session.Query<RatingStd_Average_ByMmr.Result, RatingStd_Average_ByMmr>()
            .OrderBy(o => o.Mmr)
            .ToListAsync();

        return results.Select(s => new MmrDevDto()
        {
            Mmr = s.Mmr,
            Count = s.Count
        }).ToList();
    }

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

    public async Task<string?> GetToonIdName(int toonId)
    {
        var name = GetToonIdNameFromMemory(toonId);
        if (name != null)
        {
            return name;
        }

        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        return await session.Query<RavenPlayer>()
            .Where(x => x.Id == $"RavenPlayer/{toonId}")
            .Select(s => s.Name)
            .FirstOrDefaultAsync();
    }

    public List<RequestNames> GetTopPlayers(RatingType ratingType, int minGames)
    {
        return GetTopPlayersFromMemory(ratingType, minGames);
    }

    public async Task<UpdateResult> UpdateMmrChanges(List<MmrChange> mmrChanges)
    {
        using BulkInsertOperation bulkInsert = DocumentStoreHolder.Store.BulkInsert();

        for (int i = 0; i < mmrChanges.Count; i++)
        {
            await bulkInsert.StoreAsync(new RavenMmrChange() { Changes = mmrChanges[i].Changes }, $"RavenMmrChange/{mmrChanges[i].Hash}");
        }

        return new UpdateResult() { Total = mmrChanges.Count };
    }

    public async Task<UpdateResult> UpdateRavenPlayers(Dictionary<RavenPlayer, RavenRating> ravenPlayerRatings, RatingType ratingType)
    {
        using BulkInsertOperation bulkInsert = DocumentStoreHolder.Store.BulkInsert();

        foreach (var ent in ravenPlayerRatings)
        {
            await bulkInsert.StoreAsync(ent.Key, $"RavenPlayer/{ent.Key.ToonId}");
            ent.Value.Type = ratingType;
            ent.Value.ToonId = ent.Key.ToonId;
            await bulkInsert.StoreAsync(ent.Value, $"RavenRating/{ratingType}/{ent.Key.ToonId}");

            if (ent.Key.IsUploader)
            {
                StoreRating(ent.Key, ent.Value);
            }
        }
        return new UpdateResult() { Total = ravenPlayerRatings.Count };
    }

    public List<int> GetNameToonIds(string name)
    {
        return new();
    }
}
