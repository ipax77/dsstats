using dsstats.shared;

namespace dsstats.builder;

public static class CmdrBuildFactory
{
    public static CmdrBuild? Create(Commander commander)
    {
        return commander switch
        {
            Commander.Protoss => new ProtossBuild(),
            Commander.Terran => new TerranBuild(),
            Commander.Zerg => new ZergBuild(),
            _ => null
        };
    }
}
