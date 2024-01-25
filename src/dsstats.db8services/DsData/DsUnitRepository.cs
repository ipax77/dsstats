using dsstats.db8;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.db8services.DsData;

public class DsUnitRepository(IServiceScopeFactory scopeFactory)
{
    private List<DsUnit> dsUnits = [];

    public async Task<List<DsUnit>> GetDsUnits()
    {
        await LoadUnits();
        return new(dsUnits);
    }

    private async Task LoadUnits()
    {
        if (dsUnits.Count > 0)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        dsUnits = await context.DsUnits
            .AsNoTracking()
            .ToListAsync();
    }
}
