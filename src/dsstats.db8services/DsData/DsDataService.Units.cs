using AutoMapper.QueryableExtensions;
using dsstats.shared;
using dsstats.shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services.DsData;

public partial class DsDataService
{
    public async Task<DsUnitDto?> GetUnitDetails(UnitDetailRequest request, CancellationToken token = default)
    {
        return await context.DsUnits
            .ProjectTo<DsUnitDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(f => f.Name == request.Name && f.Commander == request.Commander);
    }

    public async Task<int> GetUnitsListCount(UnitRequest request, CancellationToken token = default)
    {
        var units = GetUnitsQueriable(request);
        return await units.CountAsync(token);
    }

    public async Task<List<DsUnitListDto>> GetUnitsList(UnitRequest request, CancellationToken token = default)
    {
        var units = GetUnitsQueriable(request);
        units = SortUnits(request, units);

        if (request.Skip < 0 || request.Take < 0)
        {
            request.Skip = 0;
            request.Take = 20;
        }

        return await units
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);
    }

    private IQueryable<DsUnitListDto> SortUnits(UnitRequest request, IQueryable<DsUnitListDto> units)
    {
        if (request.Orders.Count == 0)
        {
            return units.OrderBy(o => o.Commander).ThenBy(o => o.Name);
        }

        foreach (var order in request.Orders)
        {
            if (order.Ascending)
            {
                units = units.AppendOrderBy(order.Property);
            }
            else
            {
                units = units.AppendOrderByDescending(order.Property);
            }
        }
        return units;
    }

    private IQueryable<DsUnitListDto> GetUnitsQueriable(UnitRequest request)
    {
        var units = context.DsUnits.AsNoTracking();

        if (!string.IsNullOrEmpty(request.Search))
        {
            units = units.Where(x => x.Name.Contains(request.Search));
        }

        if (request.Commander != Commander.None)
        {
            units = units.Where(x => x.Commander == request.Commander);
        }

        return units.ProjectTo<DsUnitListDto>(mapper.ConfigurationProvider);
    }
}
