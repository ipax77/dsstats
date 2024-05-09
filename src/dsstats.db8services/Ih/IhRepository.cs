using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public class IhRepository(ReplayContext context) : IIhRepository
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
            .Select(s => new GroupStateDto()
            {
                RatingType = s.RatingType,
                GroupId = s.GroupId,
                Visitors = s.Players,
                Created = s.Created
            }).ToListAsync();
    }
}
