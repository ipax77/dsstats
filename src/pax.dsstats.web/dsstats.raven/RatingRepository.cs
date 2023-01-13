using pax.dsstats.shared;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Documents.Linq;

namespace dsstats.raven;

public partial class RatingRepository
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

    public async Task<UpdateResult> UpdateRavenPlayers(HashSet<PlayerDsRDto> players, Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        //using BulkInsertOperation bulkInsert = DocumentStoreHolder.Store.BulkInsert();

        //foreach (var ent in ravenPlayerRatings)
        //{
        //    await bulkInsert.StoreAsync(ent.Key, $"RavenPlayer/{ent.Key.ToonId}");
        //    ent.Value.Type = ratingType;
        //    ent.Value.ToonId = ent.Key.ToonId;
        //    await bulkInsert.StoreAsync(ent.Value, $"RavenRating/{ratingType}/{ent.Key.ToonId}");

        //    if (ent.Key.IsUploader)
        //    {
        //        StoreRating(ent.Key, ent.Value);
        //    }
        //}
        //return new UpdateResult() { Total = ravenPlayerRatings.Count };
        return await Task.FromResult(new UpdateResult());
    }

    public async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetCalcRatings(List<ReplayDsRDto> replayDsRDtos)
    {
        List<int> cmdrToonIds = replayDsRDtos
            .Where(x => x.GameMode == GameMode.Commanders || x.GameMode == GameMode.CommandersHeroic)
            .SelectMany(x => x.ReplayPlayers.Select(y => y.Player.ToonId)).ToList();

        List<int> stdToonIds = replayDsRDtos
            .Where(x => x.GameMode == GameMode.Standard)
            .SelectMany(x => x.ReplayPlayers.Select(y => y.Player.ToonId)).ToList();

        return new Dictionary<RatingType, Dictionary<int, CalcRating>>()
        {
            { RatingType.Cmdr, await GetCalcRatings(RatingType.Cmdr, cmdrToonIds) },
            { RatingType.Std, await GetCalcRatings(RatingType.Std, stdToonIds) }
        };
    }
    public async Task<Dictionary<int, CalcRating>> GetCalcRatings(RatingType ratingType, List<int> toonIds)
    {
        List<string> ravenIds = toonIds
            .Select(s => $"RavenRating/{ratingType}/{s}")
            .ToList();

        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var ratings = await session.Query<RavenRating>()
            .Where(x => x.Id.In(ravenIds))
            .ToListAsync();

        return ratings.ToDictionary(k => k.ToonId, v => new CalcRating()
        {
            Games = v.Games,
            Wins = v.Wins,
            Mvp = v.Mvp,
            TeamGames = v.TeamGames,
            Mmr = v.Mmr,
            Consistency = v.Consistency,
            Confidence = v.Confidence,
            MmrOverTime = GetTimeRatings(v.MmrOverTime),
            CmdrCounts = GetFakeCmdrDic(v.Main, v.MainPercentage, v.Games)
        });
    }

    private List<TimeRating> GetTimeRatings(string? mmrOverTime)
    {
        if (string.IsNullOrEmpty(mmrOverTime))
        {
            return new();
        }

        List<TimeRating> timeRatings = new();

        foreach (var ent in mmrOverTime.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var timeMmr = ent.Split(',');
            if (timeMmr.Length == 2)
            {
                if (double.TryParse(timeMmr[0], out double mmr))
                {
                    timeRatings.Add(new TimeRating()
                    {
                        Mmr = mmr,
                        Date = timeMmr[1]
                    });
                }
            }
        }
        return timeRatings;
    }

    private Dictionary<Commander, int> GetFakeCmdrDic(Commander main, double mainPercentage, int games)
    {
        Dictionary<Commander, int> cmdrDic = new();

        if (mainPercentage > 99)
        {
            cmdrDic.Add(main, games);
            return cmdrDic;
        }

        if ((int)main <= 3)
        {
            foreach (var cmdr in Data.GetCommanders(Data.CmdrGet.Std).Where(x => x != main))
            {
                cmdrDic[cmdr] = games / 3;
            }
        }
        else
        {
            int total = Data.GetCommanders(Data.CmdrGet.NoStd).Count;
            foreach (var cmdr in Data.GetCommanders(Data.CmdrGet.NoStd).Where(x => x != main))
            {
                cmdrDic[cmdr] = games / total;
            }
        }

        cmdrDic[main] = (int)((cmdrDic.Sum(s => s.Value) * mainPercentage) / (100 - mainPercentage));
        return cmdrDic;
    }

    public List<int> GetNameToonIds(string name)
    {
        return new();
    }

    public Task<int> UpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges, int appendId)
    {
        throw new NotImplementedException();
    }
}
