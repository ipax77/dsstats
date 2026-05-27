using System.Collections.Frozen;
using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices;

public sealed class UnitLifeCostService(
    IDbContextFactory<DsstatsContext> contextFactory,
    IMemoryCache memoryCache) : IUnitLifeCostService
{
    private const string CacheKey = "unit_life_costs_by_commander";
    private static readonly IReadOnlyDictionary<string, DsUnitLifeCostDto> Empty = new Dictionary<string, DsUnitLifeCostDto>();

    public async Task<IReadOnlyDictionary<string, DsUnitLifeCostDto>> GetUnitLifeCosts(
        Commander commander,
        CancellationToken token = default)
    {
        var maps = await memoryCache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

            await using var context = await contextFactory.CreateDbContextAsync(token);
            var units = await context.DsUnits
                .AsNoTracking()
                .Select(unit => new DsUnitLifeCostDto
                {
                    Name = unit.Name,
                    Commander = unit.Commander,
                    Cost = unit.Cost,
                    Life = unit.Life
                })
                .ToListAsync(token);

            return units
                .GroupBy(unit => unit.Commander)
                .ToFrozenDictionary(
                    group => group.Key,
                    group => (IReadOnlyDictionary<string, DsUnitLifeCostDto>)group
                        .GroupBy(unit => unit.Name, StringComparer.Ordinal)
                        .Select(group => group.First())
                        .ToFrozenDictionary(unit => unit.Name, StringComparer.Ordinal));
        });

        return maps is not null && maps.TryGetValue(commander, out var map)
            ? map
            : Empty;
    }
}
