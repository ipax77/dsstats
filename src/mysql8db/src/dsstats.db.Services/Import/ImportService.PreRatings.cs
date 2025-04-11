
namespace dsstats.db.Services.Import;

public partial class ImportService
{
    public async Task SetPreRatings()
    {
        if (IsMaui)
        {
            return;
        }
    }
}