using AutoMapper.QueryableExtensions;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class BuildService
{
    public async Task<BuildMapResponse> GetBuildMap(BuildRequest request, int skip, CancellationToken token = default)
    {
        var replays = GetQueriableReplays(request);

        var minDuration = request.Breakpoint switch
        {
            Breakpoint.Min5 => 300,
            Breakpoint.Min10 => 600,
            Breakpoint.Min15 => 900,
            _ => 0
        };

        bool skipMinDuration = minDuration == 0;
        bool skipVersus = request.Versus == Commander.None;

        replays = replays
            .Where(x => skipMinDuration || x.Duration >= minDuration)
            .OrderByDescending(o => o.GameTime)
            .Skip(skip)
            .Take(1);

        var query = skipVersus ?
                    from r in replays
                    from rp in r.ReplayPlayers
                    from sp in rp.Spawns
                    where sp.Breakpoint == request.Breakpoint
                        && rp.Race == request.Interest
                    select sp
                    : from r in replays
                    from rp in r.ReplayPlayers
                    from sp in rp.Spawns
                    where sp.Breakpoint == request.Breakpoint
                        && ((rp.Race == request.Interest && rp.OppRace == request.Versus)
                        || (rp.Race == request.Versus && rp.OppRace == request.Interest))
                    select sp;

        var spawns = await query
            .ProjectTo<SpawnDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        return new()
        {
            Spawn = spawns.FirstOrDefault(),
            OppSpawn = skipVersus ? null : spawns.LastOrDefault()
        };
    }
}
