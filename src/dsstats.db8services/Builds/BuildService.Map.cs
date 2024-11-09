using AutoMapper.QueryableExtensions;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class BuildService
{
    public async Task<BuildMapResponse> GetReplayBuildMap(BuildRequest request, CancellationToken token = default)
    {
        var replays = GetQueriableReplays(request);

        var minDuration = request.Breakpoint switch
        {
            Breakpoint.Min5 => 300,
            Breakpoint.Min10 => 600,
            Breakpoint.Min15 => 900,
            _ => 300
        };

        bool skipMinDuration = minDuration == 0;

        var replay = await replays
            .OrderByDescending(o => o.GameTime)
            .Where(x => skipMinDuration || x.Duration >= minDuration)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (replay is null)
        {
            return new();
        }

        var player = replay.ReplayPlayers.FirstOrDefault(f => f.Race == request.Interest
            && (request.Versus == Commander.None || f.OppRace == request.Versus));

        if (player is null)
        {
            return new();
        }


        var oppPlayer = player.GamePos switch
        {
            1 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 4),
            2 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 5),
            3 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 6),
            4 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 1),
            5 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 2),
            6 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 3),
            _ => null
        };

        return new()
        {
            ReplayPlayer = player,
            OppReplayPlayer = oppPlayer
        };
    }
}
