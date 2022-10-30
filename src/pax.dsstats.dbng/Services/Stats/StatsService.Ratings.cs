using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<PlayerRatingDto?> GetPlayerRating(int toonId)
    {
        var players = GetQueriablePlayers(new());

        return await players
            .Where(x => x.ToonId == toonId)
            .ProjectTo<PlayerRatingDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token = default)
    {
        var players = GetQueriablePlayers(request);

        players = FilterRatingPlayers(players, request.Search);

        return await players.CountAsync(token);
    }

    public async Task<List<PlayerRatingDto>> GetRatings(RatingsRequest request, CancellationToken token = default)
    {
        var players = GetQueriablePlayers(request);

        players = FilterRatingPlayers(players, request.Search);
        players = SetOrder(players, request.Orders);

        return await players
        .Skip(request.Skip)
            .Take(request.Take)
            .ProjectTo<PlayerRatingDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);
    }

    private IQueryable<Player> GetQueriablePlayers(RatingsRequest request)
    {
        return context.Players
            .OrderBy(o => o.PlayerId)
            .Where(x => x.GamesCmdr >= 20 || x.GamesStd >= 20)
            .AsNoTracking();
    }

    private static IQueryable<Player> FilterRatingPlayers(IQueryable<Player> players, string? searchString)
    {
        if (string.IsNullOrEmpty(searchString))
        {
            return players;
        }
        return players.Where(x => x.Name.ToUpper().Contains(searchString.ToUpper()));
    }

    private static IQueryable<Player> SetOrder(IQueryable<Player> players, List<TableOrder> orders)
    {
        foreach (var order in orders)
        {
            if (order.Property == "WinrateCmdr")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.WinsCmdr / o.GamesCmdr);
                }
                else
                {
                    players = players.OrderByDescending(o => o.WinsCmdr / o.GamesCmdr);
                }
            }
            else if (order.Property == "MvprateCmdr")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.MvpCmdr / o.GamesCmdr);
                }
                else
                {
                    players = players.OrderByDescending(o => o.MvpCmdr / o.GamesCmdr);
                }
            }
            else if (order.Property == "TeamgamesCmdr")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.TeamGamesCmdr / o.GamesCmdr);
                }
                else
                {
                    players = players.OrderByDescending(o => o.TeamGamesCmdr / o.GamesCmdr);
                }
            }
            else if (order.Property == "WinrateStd")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.WinsStd / o.GamesStd);
                }
                else
                {
                    players = players.OrderByDescending(o => o.WinsStd / o.GamesStd);
                }
            }
            else if (order.Property == "MvprateStd")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.MvpStd / o.GamesStd);
                }
                else
                {
                    players = players.OrderByDescending(o => o.MvpStd / o.GamesStd);
                }
            }
            else if (order.Property == "TeamgamesStd")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.TeamGamesStd / o.GamesStd);
                }
                else
                {
                    players = players.OrderByDescending(o => o.TeamGamesStd / o.GamesStd);
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

    public async Task<string?> GetPlayerRatings(int toonId)
    {
        return await context.Players
            .Where(x => x.ToonId == toonId)
            .Select(s => $"{s.MmrOverTime}X{s.MmrStdOverTime}")
            .FirstOrDefaultAsync();
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        var devs = await context.Players
            .GroupBy(g => Math.Round(g.Mmr, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.Mmr, 0))
            }).ToListAsync();

        return devs;
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        var devs = await context.Players
            .GroupBy(g => Math.Round(g.MmrStd, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.MmrStd, 0))
            }).ToListAsync();

        return devs;
    }
}
