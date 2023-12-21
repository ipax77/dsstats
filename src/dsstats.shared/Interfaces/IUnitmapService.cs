namespace dsstats.shared.Interfaces;

public interface IUnitmapService
{
    Task<Unitmap> GetUnitMap(UnitmapRequest request);
}
