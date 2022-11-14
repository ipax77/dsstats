
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class MmrService
{
    public async Task<PlayerRatingDto?> GetPlayerRating(int toonId)
    {
        if (ToonIdRatings.ContainsKey(toonId))
        {
            return await Task.FromResult(ToonIdRatings[toonId]);
        }
        return null;
    }

    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token = default)
    {
        var players = GetQueriablePlayers(request);

        players = FilterRatingPlayers(players, request.Search);

        return await Task.FromResult(players.Count());
    }

    public async Task<List<PlayerRatingDto>> GetRatings(RatingsRequest request, CancellationToken token = default)
    {
        var players = GetQueriablePlayers(request);

        players = FilterRatingPlayers(players, request.Search);
        players = SetOrder(players, request.Orders);

        var ratings = players
        .Skip(request.Skip)
            .Take(request.Take)
            .ToList();

        return await Task.FromResult(ratings);
    }

    public async Task<string?> GetPlayerRatings(int toonId)
    {
        string cmdr = "";
        string std = "";
        if (ToonIdStdRatingOverTime.ContainsKey(toonId))
        {
            std = ToonIdStdRatingOverTime[toonId];
        }

        if (ToonIdCmdrRatingOverTime.ContainsKey(toonId))
        {
            cmdr = ToonIdCmdrRatingOverTime[toonId];
        }

        if (String.IsNullOrEmpty(cmdr) && String.IsNullOrEmpty(std))
        {
            return null;
        }

        return await Task.FromResult($"{cmdr}X{std}");
    }

    private IQueryable<PlayerRatingDto> GetQueriablePlayers(RatingsRequest request)
    {
        return ToonIdRatings.Values
            .OrderBy(o => o.PlayerId)
            .Where(x => x.CmdrRatingStats.Games >= 20 || x.StdRatingStats.Games >= 20)
            .AsQueryable();
    }

    private static IQueryable<PlayerRatingDto> FilterRatingPlayers(IQueryable<PlayerRatingDto> players, string? searchString)
    {
        if (string.IsNullOrEmpty(searchString))
        {
            return players;
        }

        if (int.TryParse(searchString, out int toonId))
        {
            return players.Where(x => x.ToonId == toonId);
        }

        return players.Where(x => x.Name.ToUpper().Contains(searchString.ToUpper()));
    }

    private static IQueryable<PlayerRatingDto> SetOrder(IQueryable<PlayerRatingDto> players, List<TableOrder> orders)
    {
        foreach (var order in orders)
        {
            if (order.Property == "WinrateCmdr")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.CmdrRatingStats.Games > 0 ? o.CmdrRatingStats.Wins * 100.0 / o.CmdrRatingStats.Games : o.CmdrRatingStats.Wins);
                }
                else
                {
                    players = players.OrderByDescending(o => o.CmdrRatingStats.Games > 0 ? o.CmdrRatingStats.Wins * 100.0 / o.CmdrRatingStats.Games : o.CmdrRatingStats.Wins);
                }
            }
            else if (order.Property == "MvprateCmdr")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.CmdrRatingStats.Games > 0 ? o.CmdrRatingStats.Mvp * 100.0 / o.CmdrRatingStats.Games : o.CmdrRatingStats.Mvp);
                }
                else
                {
                    players = players.OrderByDescending(o => o.CmdrRatingStats.Games > 0 ? o.CmdrRatingStats.Mvp * 100.0 / o.CmdrRatingStats.Games : o.CmdrRatingStats.Mvp);
                }
            }
            else if (order.Property == "TeamgamesCmdr")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o =>  o.CmdrRatingStats.Games > 0 ? o.CmdrRatingStats.TeamGames * 100.0 / o.CmdrRatingStats.Games : o.CmdrRatingStats.Games);
                }
                else
                {
                    players = players.OrderByDescending(o => o.CmdrRatingStats.Games > 0 ? o.CmdrRatingStats.TeamGames * 100.0 / o.CmdrRatingStats.Games : o.CmdrRatingStats.Games);
                }
            }
            else if (order.Property == "WinrateStd")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.StdRatingStats.Games > 0 ? o.StdRatingStats.Wins * 100.0 / o.StdRatingStats.Games : o.CmdrRatingStats.Wins);
                }
                else
                {
                    players = players.OrderByDescending(o => o.StdRatingStats.Wins > 0 ? o.StdRatingStats.Wins * 100.0 / o.StdRatingStats.Games : o.StdRatingStats.Wins);
                }
            }
            else if (order.Property == "MvprateStd")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.StdRatingStats.Games > 0 ? o.StdRatingStats.Mvp * 100.0 / o.StdRatingStats.Games : o.StdRatingStats.Mvp);
                }
                else
                {
                    players = players.OrderByDescending(o => o.StdRatingStats.Games > 0 ? o.StdRatingStats.Mvp * 100.0 / o.StdRatingStats.Games : o.StdRatingStats.Mvp);
                }
            }
            else if (order.Property == "TeamgamesStd")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.StdRatingStats.Games > 0 ? o.StdRatingStats.TeamGames * 100.0 / o.StdRatingStats.Games : o.StdRatingStats.TeamGames);
                }
                else
                {
                    players = players.OrderByDescending(o => o.StdRatingStats.Games > 0 ? o.StdRatingStats.TeamGames * 100.0 / o.StdRatingStats.Games : o.StdRatingStats.TeamGames);
                }
            }
            else
            {
                if (order.Ascending)
                {
                    players = players.AppendOrderBy(order.Property);
                }
                else
                {
                    players = players.AppendOrderByDescending(order.Property);
                }
            }
        }
        return players;
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        var devs = ToonIdRatings.Values
            .GroupBy(g => Math.Round(g.CmdrRatingStats.Mmr, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.CmdrRatingStats.Mmr, 0))
            })
            .OrderBy(o => o.Mmr)
            .ToList();

        return await Task.FromResult(devs);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        var devs = ToonIdRatings.Values
            .GroupBy(g => Math.Round(g.StdRatingStats.Mmr, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.StdRatingStats.Mmr, 0))
            })
            .OrderBy(o => o.Mmr)
            .ToList();

        return await Task.FromResult(devs);
    }
}

