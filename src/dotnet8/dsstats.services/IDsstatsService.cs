
namespace dsstats.services
{
    public interface IDsstatsService
    {
        Task<DsstatsReplaysResponse> GetReplays(string? page);
    }
}