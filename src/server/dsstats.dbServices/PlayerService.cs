using dsstats.db;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices;

public partial class PlayerService(
    IDbContextFactory<DsstatsContext> contextFactory,
    IImportService importService,
    IMemoryCache memoryCache,
    ILogger<PlayerService> logger) : IPlayerService
{
}
