using pax.dsstats.shared;

namespace pax.dsstats.dbng.Repositories;

public partial class ReplayRepository
{
    private IQueryable<Replay> GetAdvReplays(IQueryable<Replay> replays, ReplaysRequest request)
    {
        if (request.AdvancedRequest == null)
        {
            return replays;
        }

        replays = GetCmdrRequestReplays(replays, request.AdvancedRequest);
        replays = GetNameRequestReplays(replays, request.AdvancedRequest);
        return replays;
    }

    private IQueryable<Replay> GetNameRequestReplays(IQueryable<Replay> replays, ReplaysAdvancedRequest request)
    {
        Dictionary<int, string> exactNames = new();
        HashSet<KeyValuePair<string, string>> lineNames = new();
        HashSet<string> anyNames = new();

        foreach (var nameEnt in request.ReplayNameRequests)
        {
            if (nameEnt.Option == ReplaysAdvEnum.Exact)
            {
                if (!string.IsNullOrWhiteSpace(nameEnt.Name))
                {
                    exactNames[nameEnt.Position] = nameEnt.Name;
                }
                if (!string.IsNullOrWhiteSpace(nameEnt.OppName))
                {
                    exactNames[nameEnt.Position + 3] = nameEnt.OppName;
                }
            }
            else if (nameEnt.Option == ReplaysAdvEnum.ExactLine)
            {
                if (!string.IsNullOrWhiteSpace(nameEnt.Name))
                {
                    if (!string.IsNullOrWhiteSpace(nameEnt.OppName))
                    {
                        lineNames.Add(new(nameEnt.Name, nameEnt.OppName));
                    }
                    else
                    {
                        anyNames.Add(nameEnt.Name);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(nameEnt.Name))
                {
                    anyNames.Add(nameEnt.Name);
                }
                if (!string.IsNullOrWhiteSpace(nameEnt.OppName))
                {
                    anyNames.Add(nameEnt.OppName);
                }
            }
        }

        foreach (var exactName in exactNames)
        {
            replays = replays
                .Where(a => a.ReplayPlayers.Any(a => a.GamePos == exactName.Key && a.Name == exactName.Value));
        }

        foreach (var lineName in lineNames)
        {
            for (int i = 0; i < 3; i++)
            {
                replays = replays.Where(x => x.ReplayPlayers.Any(a => a.GamePos == i + 1 && a.Name == lineName.Key
                    && a.GamePos == i + 1 + 3 && a.Name == lineName.Value));
            }
            for (int i = 0; i < 3; i++)
            {
                replays = replays.Where(x => x.ReplayPlayers.Any(a => a.GamePos == i + 1 + 3 && a.Name == lineName.Key
                    && a.GamePos == i + 1 && a.Name == lineName.Value));
            }
        }

        if (anyNames.Count > 0)
        {
            replays = replays.Where(x => x.ReplayPlayers.Any(a => anyNames.Contains(a.Name)));
        }

        return replays;
    }


    private IQueryable<Replay> GetCmdrRequestReplays(IQueryable<Replay> replays, ReplaysAdvancedRequest request)
    {
        Dictionary<int, Commander> exactCmdrs = new();
        HashSet<KeyValuePair<Commander, Commander>> lineCmdrs = new();
        HashSet<Commander> anyCmdrs = new();

        foreach (var cmdrEnt in request.ReplayCmdrRequests)
        {
            if (cmdrEnt.Option == ReplaysAdvEnum.Exact)
            {
                if (cmdrEnt.Commander != Commander.None)
                {
                    exactCmdrs[cmdrEnt.Position] = cmdrEnt.Commander;
                }
                if (cmdrEnt.OppCommander != Commander.None)
                {
                    exactCmdrs[cmdrEnt.Position + 3] = cmdrEnt.OppCommander;
                }
            }
            else if (cmdrEnt.Option == ReplaysAdvEnum.ExactLine)
            {
                if (cmdrEnt.Commander != Commander.None)
                {
                    if (cmdrEnt.OppCommander != Commander.None)
                    {
                        lineCmdrs.Add(new(cmdrEnt.Commander, cmdrEnt.OppCommander));
                    }
                    else
                    {
                        anyCmdrs.Add(cmdrEnt.Commander);
                    }
                }
            }
            else
            {
                if (cmdrEnt.Commander != Commander.None)
                {
                    anyCmdrs.Add(cmdrEnt.Commander);
                }
                if (cmdrEnt.OppCommander != Commander.None)
                {
                    anyCmdrs.Add(cmdrEnt.OppCommander);
                }
            }
        }


        foreach (var exactCmdr in exactCmdrs)
        {
            replays = replays
                .Where(a => a.ReplayPlayers.Any(a => a.GamePos == exactCmdr.Key && a.Race == exactCmdr.Value));
        }

        foreach (var lineCmdr in lineCmdrs)
        {
            replays = replays
                .Where(x => x.ReplayPlayers.Any(a => a.Race == lineCmdr.Key && a.OppRace == lineCmdr.Value));
        }

        if (anyCmdrs.Count > 0)
        {
            replays = replays.Where(x => x.ReplayPlayers.Any(a => anyCmdrs.Contains(a.Race)));
        }

        return replays;
    }
}
