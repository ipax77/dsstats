using dsstats.shared;

namespace dsstats.parser;

internal static partial class DsstatsReplayMapper
{
    public static ReplayDto ToDto(this DsstatsReplay replay)
    {
        var gameMode = GetGameMode(replay.Modes, replay.Players.Where(x => x.Observe == 0).Count());

        if (replay.Duration == 0)
        {
            replay.Duration = replay.Players.Max(p => p.Duration);
        }

        var replayDto = new ReplayDto
        {
            Title = replay.Title,
            Version = replay.Version.ToString(),
            Gametime = Data.CanonicalizeGametime(replay.Gametime),
            GameMode = gameMode,
            BaseBuild = replay.BaseBuild,
            Duration = (int)(replay.Duration / 22.4),
            Cannon = (int)(replay.Cannon / 22.4),
            Bunker = (int)(replay.Bunker / 22.4),
            WinnerTeam = replay.WinnerTeam,
            MiddleChanges = GetMiddle(replay.MiddleChanges),
            Players = replay.Players.Select(p => p.ToDto(replay, gameMode)).ToList()
        };
        var allSpawns = replayDto.Players.SelectMany(s => s.Spawns).Where(x => x.Breakpoint == Breakpoint.All).ToList();
        if (allSpawns.Count > 0)
        {
            var maxKills = allSpawns.Max(m => m.KilledValue);
            foreach (var player in replayDto.Players.Where(s => s.Spawns.Any(a => a.KilledValue == maxKills)))
            {
                player.IsMvp = true;
            }
            ;
        }
        var mostFrequentRegion = replayDto.Players
            .Select(p => p.Player.ToonId.Region)
            .GroupBy(region => region)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();
        replayDto.RegionId = mostFrequentRegion;

        return replayDto;
    }

    private static GameMode GetGameMode(HashSet<string> modes, int playerCount)
    {
        if (playerCount == 1)
        {
            return GameMode.Tutorial;
        }

        bool isBrawl = false;
        bool isCommanders = false;
        bool isStandard = false;

        foreach (var mode in modes)
        {
            if (mode == "GameModeBrawl")
            {
                isBrawl = true;
            }
            else if (mode == "GameModeBrawlCommanders")
            {
                return GameMode.BrawlCommanders;
            }
            else if (mode == "GameModeBrawlStandard")
            {
                return GameMode.BrawlStandard;
            }
            else if (mode == "GameModeHeroicCommanders" || mode == "GameModeCommandersHeroic")
            {
                return GameMode.CommandersHeroic;
            }
            else if (mode == "GameModeCommanders")
            {
                isCommanders = true;
            }
            else if (mode == "GameModeStandard")
            {
                isStandard = true;
            }
            else if (mode == "GameModeGear")
            {
                return GameMode.Gear;
            }
            else if (mode == "GameModeSwitch")
            {
                return GameMode.Switch;
            }
            else if (mode == "GameModeSabotage")
            {
                return GameMode.Sabotage;
            }
        }

        if (isBrawl && isCommanders)
        {
            return GameMode.BrawlCommanders;
        }
        else if (isBrawl)
        {
            return GameMode.BrawlStandard;
        }
        else if (isCommanders)
        {
            return GameMode.Commanders;
        }
        else if (isStandard)
        {
            return GameMode.Standard;
        }

        return GameMode.Tutorial;
    }

    private static List<int> GetMiddle(List<DsMiddle> middleChanges)
    {
        if (middleChanges.Count == 0)
        {
            return [];
        }
        int firstTeam = middleChanges[0].ControlTeam;
        return [firstTeam, .. middleChanges.Select(s => (int)(s.Gameloop / 22.4))];
    }

    public static ReplayPlayerDto ToDto(this DsPlayer player, DsstatsReplay replay, GameMode gameMode)
    {
        Commander race = player.Race;
        if (Data.IsCommanderGameMode(gameMode) && (int)player.Race <= 3)
        {
            race = Commander.None;
        }

        return new ReplayPlayerDto
        {
            Name = player.Name,
            Clan = player.Clan,
            Race = race,
            SelectedRace = player.SelectedRace,
            GamePos = player.GamePos,
            TeamId = player.TeamId,
            Duration = (int)(player.Duration / 22.4),
            Result = player.Result,
            Apm = player.Apm,
            Messages = player.Messages,
            Pings = player.Pings,
            Spawns = CreateSpawns(player, replay),
            TierUpgrades = player.TierUpgrades.Select(s => (int)(s / 22.4)).ToList(),
            Refineries = player.Refineries.Where(x => x.Taken).Select(s => (int)(s.Gameloop / 22.4)).OrderBy(o => o).ToList(),
            Upgrades = player.Upgrades.Select(s => new UpgradeDto() { Name = s.Key, Gameloop = (int)(s.Value / 22.4) }).ToList(),
            Player = new PlayerDto
            {
                Name = player.Name,
                ToonId = new ToonIdDto
                {
                    Region = player.ToonId.Region,
                    Realm = player.ToonId.Realm,
                    Id = player.ToonId.Id
                }
            }
        };
    }
}

