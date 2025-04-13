
using dsstats.shared;

namespace dsstats.db8services;

public partial class PlayerService
{
    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token)
    {
        return await GetComboRatingsListCount(request, token);
    }

    public async Task<List<ComboPlayerRatingDto>> GetRatings(RatingsRequest request, CancellationToken token)
    {
        if (request.Take <= 0)
        {
            return new();
        }
        return await GetComboRatingsList(request, token);

    }
}