namespace pax.dsstats.shared.Interfaces;

public interface IWinrateService
{
    Task<WinrateResponse> GetWinrate(WinrateRequest request, CancellationToken token);
}