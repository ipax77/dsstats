namespace dsstats.shared.Interfaces;

public interface ICmdrInfoService
{
    Task<List<CmdrPlayerInfo>> GetCmdrPlayerInfos(CmdrInfoRequest request, CancellationToken token = default);
}
