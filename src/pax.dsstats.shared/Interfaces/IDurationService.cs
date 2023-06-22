
namespace pax.dsstats.shared.Interfaces;

public interface IDurationService
{
    Task<DurationResponse> GetDuration(DurationRequest request, CancellationToken token = default);
}