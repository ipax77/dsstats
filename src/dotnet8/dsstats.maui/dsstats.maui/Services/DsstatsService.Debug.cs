
using dsstats.db8;
using Microsoft.EntityFrameworkCore;

namespace dsstats.maui.Services;

public partial class DsstatsService
{
    public async Task DeleteRecentReplays(int count)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.ReplayPlayerRatingInfo)
            .Include(i => i.ReplayRatingInfo)
        .OrderByDescending(o => o.GameTime)
        .Take(count)
        .ToListAsync();

        context.Replays.RemoveRange(replays);
        await context.SaveChangesAsync();
    }
}
