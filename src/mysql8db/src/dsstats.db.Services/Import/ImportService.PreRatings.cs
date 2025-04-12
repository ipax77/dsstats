
using dsstats.db.Services.Ratings;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.db.Services.Import;

public partial class ImportService
{
    public async Task SetPreRatings()
    {
        if (IsMaui)
        {
            return;
        }
        var scope = serviceProvider.CreateAsyncScope();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
        await ratingsService.ContinueCalculateRatings();
    }
}