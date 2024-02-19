using dsstats.shared;
using System.Collections.Concurrent;

namespace dsstats.api.Services;

public class PickBanService
{
    private ConcurrentDictionary<Guid, PickBanState> PickBans = new();

    public PickBanState CreateOrVisit(PickBanSetting pickBanSetting)
    {
        if (!PickBans.TryGetValue(pickBanSetting.Id, out PickBanState? state))
        {
            state = new PickBanState(pickBanSetting.PickBanMode);
            PickBans.TryAdd(pickBanSetting.Id, state);
        }
        lock (state)
        {
            state.Visitors++;
        }
        return GetPublicPickBanState(state);
    }

    public PickBanState? Connect(Guid guid)
    {
        if (PickBans.TryGetValue(guid, out PickBanState? state))
        {
            lock (state)
            {
                state.Visitors++;
            }
            return GetPublicPickBanState(state);
        }
        return null;
    }

    public int Disconnect(Guid guid)
    {
        if (PickBans.TryGetValue(guid, out PickBanState? state))
        {
            lock (state)
            {
                state.Visitors--;
            }
            return state.Visitors;
        }
        return 0;
    }

    public PickBanState? Ban(Guid guid, PickBanEnt ent)
    {
        if (PickBans.TryGetValue(guid, out PickBanState? state))
        {
            lock (state)
            {
                var ban = state.Bans.FirstOrDefault(f => !f.IsLocked && f.Pos == ent.Pos);
                if (ban != null)
                {
                    ban.IsLocked = true;
                    ban.Commander = ent.Commander;
                    return GetPublicPickBanState(state);
                }
            }
        }
        return null;
    }

    public PickBanState? Lock(Guid guid, PickBanEnt ent)
    {
        if (PickBans.TryGetValue(guid, out PickBanState? state))
        {
            lock (state)
            {
                var pick = state.Picks.FirstOrDefault(f => !f.IsLocked && f.Pos == ent.Pos);
                if (pick != null)
                {
                    if (ent.Commander == Commander.None
                        && state.PickBanMode == PickBanMode.StdRandom)
                    {
                        pick.Commander = (Commander)Random.Shared.Next(1, 4);
                        pick.Random = true;
                    }
                    else
                    {
                        pick.Commander = ent.Commander;
                    }

                    pick.PlayerName = ent.PlayerName;
                    pick.IsLocked = true;
                    pick.Order = state.Picks
                        .Where(x => x.Team == pick.Team && x.IsLocked)
                        .Count();
                    return GetPublicPickBanState(state);
                }
            }
        }
        return null;
    }

    private PickBanState GetPublicPickBanState(PickBanState state)
    {
        PickBanState publicState = state with { Bans = [], Picks = [] };

        if (!state.IsBansReady)
        {
            publicState.Bans = state.Bans.Select(s => s with { Commander = Commander.None }).ToList();
        }
        else
        {
            publicState.Bans = new List<PickBanEnt>(state.Bans);
        }

        if (!state.IsPicksReady)
        {
            publicState.Picks = state.Picks.Select(s => s with 
                { Commander = Commander.None, Random = false, Order = 0, PlayerName = null })
                .ToList();
        }
        else
        {
            publicState.Picks = new List<PickBanEnt>(state.Picks);
        }

        return publicState;
    }
}

