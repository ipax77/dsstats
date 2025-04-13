using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.shared;
using dsstats.shared8.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dsstats.db.Services.Replays;

public class ReplaysService(DsstatsContext context, IMapper mapper, ILogger<ReplaysService> logger) : IReplaysService
{
    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token)
    {
        var replays = GetReplaysQueriables(request);
        var count = await replays.CountAsync(token);
        logger.LogInformation("Got count: {count}", count);
        return count;
    }

    public async Task<List<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token)
    {
        var replays = GetReplaysQueriables(request);
        replays = OrderReplays(replays, request.Orders);
        return await replays
            .ProjectTo<ReplayListDto>(mapper.ConfigurationProvider)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);
    }

    private IQueryable<Replay> GetReplaysQueriables(ReplaysRequest request)
    {
        var replays = context.Replays
            .AsNoTracking();

        return replays;
    }

    private IQueryable<Replay> OrderReplays(IQueryable<Replay> replays, List<TableOrder> orders)
    {
        return replays.OrderByDescending(o => o.GameTime);
    }
}