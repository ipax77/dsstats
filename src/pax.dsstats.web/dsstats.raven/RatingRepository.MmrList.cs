
using pax.dsstats.shared;
using pax.dsstats.shared;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Sparrow.Collections;
using System.Collections.Generic;

namespace dsstats.raven;

public partial class RatingRepository
{
    public async Task<List<PlChange>> GetReplayPlayerMmrChanges(string replayHash, CancellationToken token = default)
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var result = await session.Query<RavenMmrChange>()
            .Where(x => x.Id == $"RavenMmrChange/{replayHash}")
            .FirstOrDefaultAsync(token);

        if (result == null)
        {
            return new();
        }

        return result.Changes;
    }

    public async Task SetReplayListMmrChanges(List<ReplayListDto> replays, CancellationToken token = default)
    {
        var ids = replays.Select(s => $"RavenMmrChange/{s.ReplayHash}").ToList();

        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var results = await session.Query<RavenMmrChange>()
            .Where(x => x.Id.In(ids))
            .ToListAsync(token);

        replays.ForEach(f =>
        {
            var result = results.FirstOrDefault(g => g.Id.EndsWith(f.ReplayHash));
            f.MmrChange = Math.Round(result?.Changes.FirstOrDefault(g => g.Pos == f.PlayerPos)?.Change ?? 0, 2);
        });
    }
}
