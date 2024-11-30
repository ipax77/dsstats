namespace dsstats.shared;

public record PickBanStateDto
{
    public PickBanMode PickBanMode { get; set; }
    public GameMode GameMode { get; set; }
    public int Visitors { get; set; }
    public int TotalBans { get; set; }
    public int TotalPicks { get; set; }
    public List<PickBan> Picks { get; set; } = [];
    public List<PickBan> Bans { get; set; } = [];
}

public record PickBan
{
    public int Slot { get; set; }
    public Commander Commander { get; set; }
    public string? Name { get; set; }
    public bool Locked { get; set; }
}

public class PickBanSetting
{
    public PickBanSetting(Guid guid, PickBanMode pickBanMode, PickBanStateDto serverState)
    {
        ServerState = serverState;
        var stdCommanders = Data.GetCommanders(Data.CmdrGet.Std);
        var cmdrCommanders = Data.GetCommanders(Data.CmdrGet.NoStd);
        Commanders = pickBanMode switch
        {
            PickBanMode.Standard => stdCommanders,
            PickBanMode.StdRandom => stdCommanders,
            PickBanMode.Std1v1 => stdCommanders,
            _ => cmdrCommanders
        };

        if (pickBanMode == PickBanMode.StdRandom || pickBanMode == PickBanMode.Std1v1)
        {
            WithRandom = true;
        }
        Commanders.Insert(0, Commander.None);

        if (Commanders.Contains(Commander.Zeratul))
        {
            Commanders.Remove(Commander.Zeratul);
        }

        if (Commanders.Contains(Commander.Abathur))
        {
            UniqueCommanders = true;
        }
        AvailableCommanders = new(Commanders);

        (GameMode gameMode, int totalBans, int totalPicks) = Data.GetPickBanModeSettings(pickBanMode);
        ClientState = new() { GameMode = gameMode, TotalBans = totalBans, TotalPicks = totalPicks };
        for (int i = 0; i < totalBans; i++)
        {
            ClientState.Bans.Add(new() { Slot = i });
        }
        for (int i = 0; i < totalPicks; i++)
        {
            ClientState.Picks.Add(new() { Slot = i });
        }

        if (totalBans == 0)
        {
            BansReady = true;
        }
        if (totalPicks == 0)
        {
            PicksReady = true;
        }

        ServerBans(serverState.Bans);
        ServerPicks(serverState.Picks);
    }

    public PickBanStateDto ClientState { get; set; }
    public PickBanStateDto ServerState { get; set; }
    public List<Commander> Commanders { get; set; }
    public List<Commander> AvailableCommanders { get; set; }
    public bool UniqueCommanders { get; set; }
    public bool WithRandom {  get; set; }
    public bool BansReady { get; set; }
    public bool PicksReady { get; set; }
    private object lockobject = new();

    public void ClientPick(PickBan pickBan)
    {
        if (!BansReady || PicksReady)
        {
            return;
        }
        lock (lockobject)
        {
            pickBan.Locked = true;
            if (UniqueCommanders && pickBan.Commander != Commander.None && AvailableCommanders.Contains(pickBan.Commander))
            {
                AvailableCommanders.Remove(pickBan.Commander);
            }
        }
    }
    public void ClientBan(PickBan pickBan)
    {
        if (BansReady)
        {
            return;
        }
        lock (lockobject)
        {
            pickBan.Locked = true;
            if (pickBan.Commander != Commander.None && AvailableCommanders.Contains(pickBan.Commander))
            {
                AvailableCommanders.Remove(pickBan.Commander);
            }
        }
    }
    public void ServerBans(List<PickBan> bans)
    {
        lock (lockobject)
        {
            ServerState.Bans = bans;

            if (bans.Count == ServerState.TotalBans)
            {
                BansReady = true;
            }

            foreach (var ban in ServerState.Bans)
            {
                var clientBan = ClientState.Bans.FirstOrDefault(f => f.Slot == ban.Slot);
                if (clientBan == null)
                {
                    continue;
                }
                clientBan.Locked = true;
                if (BansReady)
                {
                    clientBan.Commander = ban.Commander;
                    clientBan.Name = ban.Name;
                }
                if (AvailableCommanders.Contains(clientBan.Commander))
                {
                    AvailableCommanders.Remove(clientBan.Commander);
                }
            }
        }
    }
    public void ServerPicks(List<PickBan> picks)
    {
        lock (lockobject)
        {
            ServerState.Picks = picks;

            if (picks.Count == ServerState.TotalPicks)
            {
                PicksReady = true;
            }

            foreach (var pick in ServerState.Picks)
            {
                var clientPick = ClientState.Picks.FirstOrDefault(f => f.Slot == pick.Slot);
                if (clientPick == null)
                {
                    continue;
                }
                clientPick.Locked = true;
                if (PicksReady)
                {
                    clientPick.Commander = pick.Commander;
                    clientPick.Name = pick.Name;
                }
                if (UniqueCommanders && clientPick.Commander != Commander.None
                    && AvailableCommanders.Contains(clientPick.Commander))
                {
                    AvailableCommanders.Remove(clientPick.Commander);
                }
            }
        }
    }
}