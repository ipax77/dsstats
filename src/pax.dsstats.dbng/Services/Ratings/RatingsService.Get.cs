using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;
using System.Globalization;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class RatingsService
{
    private async Task<List<ReplayDsRDto>> GetReplayData(DateTime startTime, int skip, int take, bool recalc)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<GameMode> gameModes = new() { GameMode.Commanders, GameMode.Standard, GameMode.CommandersHeroic };

        var replays = context.Replays
            .Where(r => r.Playercount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && r.GameTime >= startTime
                && gameModes.Contains(r.GameMode));

        if (!recalc)
        {
            replays = replays.Where(x => x.ReplayRatingInfo == null);
        }

        return await replays
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .Skip(skip)
            .Take(take)
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    private async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetCalcRatings(DateTime fromDate)
    {
        Dictionary<RatingType, Dictionary<int, CalcRating>> calcRatings = new();
        foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
        {
            if (ratingType == RatingType.None)
            {
                continue;
            }
            calcRatings[ratingType] = new();
        }

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var playersRatingsQuery = from r in context.Replays
                                  from rp in r.ReplayPlayers
                                  from pr in rp.Player.PlayerRatings
                                  where r.GameTime >= fromDate
                                   && r.ReplayRatingInfo == null
                                  select pr;
        var playerRatings = await playersRatingsQuery
            .Distinct()
            .Select(s => new
            {
                s.PlayerId,
                s.Games,
                s.Wins,
                s.Consistency,
                s.Confidence,
                s.RatingType,
                s.Rating
            })
            .ToListAsync();

        foreach (var pr in playerRatings)
        {
            calcRatings[pr.RatingType][pr.PlayerId] = new()
            {
                PlayerId = pr.PlayerId,
                Games = pr.Games,
                Wins = pr.Wins,
                Mmr = pr.Rating,
                Consistency = pr.Consistency,
                Confidence = pr.Confidence,
            };
        }

        return calcRatings;
    }

    private static int GetMmrId(Player player)
    {
        return player.PlayerId; // todo
    }

    private Dictionary<Commander, int> GetFakeCmdrDic(Commander main, int mainCount, int games)
    {
        Dictionary<Commander, int> cmdrDic = new();

        var mainPercentage = mainCount * 100.0 / games;

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
            int avg = (games - mainCount) / (total - 1);
            foreach (var cmdr in Data.GetCommanders(Data.CmdrGet.NoStd).Where(x => x != main))
            {
                cmdrDic[cmdr] = avg;
            }
        }

        cmdrDic[main] = (int)(((games - mainCount) * mainPercentage) / (100.0 - mainPercentage));
        return cmdrDic;
    }
}
