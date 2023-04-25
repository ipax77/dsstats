using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace pax.dsstats.dbng.Services;

public partial class ArcadeService
{
    public async void CreateTestData()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.ArcadeReplays
            .Include(i => i.ArcadeReplayPlayers)
                .ThenInclude(i => i.ArcadePlayer)
            .OrderBy(i => i.ArcadeReplayId);

        var testData = await replays.Take(50).ToListAsync();
        var addData = await replays.Skip(50).Take(50).ToListAsync();


    }
}
