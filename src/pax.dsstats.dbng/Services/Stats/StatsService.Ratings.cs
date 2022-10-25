using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token = default)
    {
        var players = context.Players.AsNoTracking();

        players = FilterRatingPlayers(players, request.Search);

        return await players.CountAsync(token);
    }

    public async Task<List<PlayerRatingDto>> GetRatings(RatingsRequest request, CancellationToken token = default)
    {
        var players = context.Players
            .OrderBy(o => o.PlayerId)
            .AsNoTracking();

        players = FilterRatingPlayers(players, request.Search);
        players = SetOrder(players, request.Orders);

        return await players
        .Skip(request.Skip)
            .Take(request.Take)
            .ProjectTo<PlayerRatingDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);
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
            if (order.Ascending)
            {
                players = players.AppendOrderBy(order.Property);
            }
            else
            {
                players = players.AppendOrderByDescending(order.Property);
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
