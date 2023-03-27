using AutoMapper;
using pax.dsstats.dbng.Services;

namespace dsstats.ratings.api.Services;

public partial class RatingsService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<ImportService> logger;

    private SemaphoreSlim ratingSs;

    public RatingsService(IServiceProvider serviceProvider, IMapper mapper, ILogger<ImportService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;

        ratingSs = new(1, 1);
    }

    public async Task ProduceRatings()
    {
        await ratingSs.WaitAsync();

        try
        {

        }
        finally
        {
            ratingSs.Release();
        }
    }
}
