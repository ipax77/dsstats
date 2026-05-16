using dsstats.db;
using dsstats.shared;
using dsstats.shared.Units;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices;

public class UnitRepository(IDbContextFactory<DsstatsContext> contextFactory)
{
    private Dictionary<UnitMapKey, UnitBuildInfo> _unitBuildInfoMap = [];

    private async Task Init()
    {
        if (_unitBuildInfoMap.Count > 0)
        {
            return;
        }
        await using var context = await contextFactory.CreateDbContextAsync();
        _unitBuildInfoMap = await context.DsUnits
            .AsNoTracking()
            .ToDictionaryAsync(
                k => new UnitMapKey(k.Name, k.Commander),
                v => new UnitBuildInfo(v.Name, v.UnitSize, v.MapUnitType, v.Cost, v.Life)
            );
    }

    public async Task<UnitBuildInfo?> GetUnitBuildInfo(string unitName, Commander commander)
    {
        await Init();
        _unitBuildInfoMap.TryGetValue(new UnitMapKey(unitName, commander), out var unitBuildInfo);
        return unitBuildInfo;
    }
}
