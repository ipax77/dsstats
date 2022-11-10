
using AutoMapper.QueryableExtensions;
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
        if (ToonIdCmdrRatingOverTime.ContainsKey(toonId))
        {
            return await Task.FromResult($"{ToonIdCmdrRatingOverTime[toonId]}X");
        }
        return null;
    }

    private IQueryable<PlayerRatingDto> GetQueriablePlayers(RatingsRequest request)
    {
        return ToonIdRatings.Values
            .OrderBy(o => o.PlayerId)
            .Where(x => x.GamesCmdr >= 20 || x.GamesStd >= 20)
            .AsQueryable();
    }

    private static IQueryable<PlayerRatingDto> FilterRatingPlayers(IQueryable<PlayerRatingDto> players, string? searchString)
    {
        if (string.IsNullOrEmpty(searchString))
        {
            return players;
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
                    players = players.OrderBy(o => o.GamesCmdr > 0 ? o.WinsCmdr / o.GamesCmdr : o.WinsCmdr);
                }
                else
                {
                    players = players.OrderByDescending(o => o.GamesCmdr > 0 ? o.WinsCmdr / o.GamesCmdr : o.WinsCmdr);
                }
            }
            else if (order.Property == "MvprateCmdr")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.GamesCmdr > 0 ? o.MvpCmdr / o.GamesCmdr : o.MvpCmdr);
                }
                else
                {
                    players = players.OrderByDescending(o => o.GamesCmdr > 0 ? o.MvpCmdr / o.GamesCmdr : o.MvpCmdr);
                }
            }
            else if (order.Property == "TeamgamesCmdr")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o =>  o.GamesCmdr > 0 ? o.TeamGamesCmdr / o.GamesCmdr : o.TeamGamesCmdr);
                }
                else
                {
                    players = players.OrderByDescending(o => o.GamesCmdr > 0 ? o.TeamGamesCmdr / o.GamesCmdr : o.TeamGamesCmdr);
                }
            }
            else if (order.Property == "WinrateStd")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.GamesStd > 0 ? o.WinsStd / o.GamesStd : o.WinsCmdr);
                }
                else
                {
                    players = players.OrderByDescending(o => o.WinsStd > 0 ? o.WinsStd / o.GamesStd : o.WinsStd);
                }
            }
            else if (order.Property == "MvprateStd")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.GamesStd > 0 ? o.MvpStd / o.GamesStd : o.MvpStd);
                }
                else
                {
                    players = players.OrderByDescending(o => o.GamesStd > 0 ? o.MvpStd / o.GamesStd : o.MvpStd);
                }
            }
            else if (order.Property == "TeamgamesStd")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.GamesStd > 0 ? o.TeamGamesStd / o.GamesStd : o.TeamGamesStd);
                }
                else
                {
                    players = players.OrderByDescending(o => o.GamesStd > 0 ? o.TeamGamesStd / o.GamesStd : o.TeamGamesStd);
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
            .GroupBy(g => Math.Round(g.Mmr, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.Mmr, 0))
            }).ToList();

        return await Task.FromResult(devs);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        var devs = ToonIdRatings.Values
            .GroupBy(g => Math.Round(g.MmrStd, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.MmrStd, 0))
            }).ToList();

        return await Task.FromResult(devs);
    }
}

