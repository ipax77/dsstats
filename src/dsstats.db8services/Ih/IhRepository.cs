using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dsstats.db8services;

public class IhRepository(ReplayContext context, ILogger<IhRepository> logger) : IIhRepository
{
    public async Task<GroupState> GetOrCreateGroupState(Guid groupId, RatingType ratingType = RatingType.StdTE)
    {
        var ihSession = await context.IhSessions.FirstOrDefaultAsync(f => f.GroupId == groupId);
        DateTime created = DateTime.UtcNow;

        if (ihSession is null)
        {
            ihSession = new()
            {
                RatingType = ratingType,
                GroupId = groupId,
                Created = created,
                GroupState = new()
                {
                    RatingType = ratingType,
                    GroupId = groupId,
                    Created = created
                }
            };
            context.IhSessions.Add(ihSession);
            await context.SaveChangesAsync();
        }

        if (ihSession.GroupState is null)
        {
            ihSession.GroupState = new()
            {
                RatingType = ratingType,
                GroupId = groupId,
                Created = created
            };
        }
        return ihSession.GroupState;
    }

    public async Task UpdateGroupState(GroupState groupState)
    {
        var ihSession = await context.IhSessions.FirstOrDefaultAsync(f => f.GroupId == groupState.GroupId);

        if (ihSession is null)
        {
            return;
        }

        ihSession.Players = Math.Max(ihSession.Players, groupState.PlayerStats.Count);
        ihSession.Games = groupState.ReplayHashes.Count;
        ihSession.GroupState = groupState;
        await context.SaveChangesAsync();
    }

    public async Task<List<GroupStateDto>> GetOpenGroups()
    {
        return await context.IhSessions
            .Where(x => !x.Closed)
            .OrderByDescending(o => o.Created)
            .Select(s => new GroupStateDto()
            {
                RatingType = s.RatingType,
                GroupId = s.GroupId,
                Visitors = s.Players,
                Created = s.Created
            }).ToListAsync();
    }

    public async Task CloseGroup(Guid groupId)
    {
        try
        {
            await context.IhSessions.ExecuteUpdateAsync(u => u.SetProperty(p => p.Closed, true));
        }
        catch (Exception ex)
        {
            logger.LogError("failed setting group state to closed: {error}", ex.Message);
        }
    }
}
