using dsstats.db8;
using dsstats.shared;

namespace dsstats.db8services;

public partial class ReplaysService
{
    private IQueryable<Replay> FilterReplays(ReplaysRequest request, IQueryable<Replay> replays)
    {
        if (request.Filter is null)
        {
            return replays;
        }

        if (request.Filter.Playercount != 0)
        {
            replays = replays.Where(x => x.Playercount == request.Filter.Playercount);
        }

        if (request.Filter.TournamentEdition)
        {
            replays = replays.Where(x => x.TournamentEdition);
        }

        if (request.Filter.GameModes.Count > 0
            && !request.Filter.GameModes.Contains(GameMode.None))
        {
            replays = replays.Where(x => request.Filter.GameModes.Contains(x.GameMode));
        }

        if (request.Filter.PosFilters.Count > 0)
        {
            foreach (var posFilter in request.Filter.PosFilters)
            {
                string name = posFilter.PlayerNameOrId.Trim();
                var playerId = Data.GetPlayerId(name.Replace("%7C", "|"));

                if (playerId != null)
                {
                    replays = from r in replays
                              from rp in r.ReplayPlayers
                              join p in context.Players on rp.PlayerId equals p.PlayerId
                              where (posFilter.GamePos == 0 || rp.GamePos == posFilter.GamePos)
                              && (posFilter.Commander == Commander.None || rp.Race == posFilter.Commander)
                              && (posFilter.OppCommander == Commander.None || rp.OppRace == posFilter.OppCommander)
                              && p.ToonId == playerId.ToonId && p.RealmId == playerId.RealmId && p.RegionId == playerId.RegionId
                              select r;
                }
                else
                {
                    replays = from r in replays
                              from rp in r.ReplayPlayers
                              where (posFilter.GamePos == 0 || rp.GamePos == posFilter.GamePos)
                              && (posFilter.Commander == Commander.None || rp.Race == posFilter.Commander)
                              && (posFilter.OppCommander == Commander.None || rp.OppRace == posFilter.OppCommander)
                              && (string.IsNullOrEmpty(name) || rp.Name == name)
                              select r;
                }

                foreach (var unitFilter in posFilter.UnitFilters)
                {
                    if (string.IsNullOrEmpty(unitFilter.Name) || unitFilter.Count <= 0)
                    {
                        continue;
                    }
                    replays = from r in replays
                              from rp in r.ReplayPlayers
                              from sp in rp.Spawns
                              from su in sp.Units
                              where (posFilter.GamePos == 0 || rp.GamePos == posFilter.GamePos)
                                && sp.Breakpoint == unitFilter.Breakpoint
                                && su.Unit.Name == unitFilter.Name
                                && (unitFilter.Min ? su.Count >= unitFilter.Count : su.Count < unitFilter.Count)
                              select r;
                }
            }
            replays = replays.Distinct();
        }
        return replays;
    }
}
