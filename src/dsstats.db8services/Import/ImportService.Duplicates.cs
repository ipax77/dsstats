using dsstats.db8;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services.Import;

public partial class ImportService
{
    public async Task<int> HandleDuplicates(List<Replay> replays, ReplayContext context)
    {
        var replayHashes = replays.Select(s => s.ReplayHash).ToList();
        var dupReplays = await context.Replays
            .Include(i => i.ReplayPlayers)
            .Where(x => replayHashes.Contains(x.ReplayHash))
            .ToListAsync();

        if (dupReplays.Count == 0)
        {
            return 0;
        }

        int dupsHandled = 0;

        foreach (var dbReplay in dupReplays)
        {
            var dupReplay = replays
                .FirstOrDefault(f => f.ReplayHash == dbReplay.ReplayHash);

            if (dupReplay is null)
            {
                throw new ArgumentNullException(nameof(dbReplay));
            }

            if (HandleDuplicate(dbReplay, dupReplay, context))
            {
                replays.Remove(dupReplay);
                dupsHandled++;
            }
        }
        await context.SaveChangesAsync();
        return dupsHandled;
    }

    private bool HandleDuplicate(Replay dbReplay, Replay dupReplay, ReplayContext context)
    {
        if (!DuplicateIsPlausible(dbReplay, dupReplay))
        {
            return true;
        }

        foreach (var rp in dupReplay.ReplayPlayers)
        {
            if (rp.IsUploader)
            {
                var dbRp = dbReplay.ReplayPlayers
                    .FirstOrDefault(f => f.PlayerId == rp.PlayerId);
                if (dbRp is not null)
                {
                    dbRp.IsUploader = true;
                }
            }
        }
        return true;
    }

    private bool DuplicateIsPlausible(Replay dbReplay, Replay dupReplay)
    {
        if ((dupReplay.GameTime - dbReplay.GameTime).Duration() > TimeSpan.FromDays(3))
        {
            return false;
        }
        return true;
    }
}
