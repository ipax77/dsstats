using dsstats.shared;
using s2protocol.NET;
using s2protocol.NET.Models;
using System.Text;

namespace dsstats.parser;

public static partial class DsstatsParser
{
    internal static readonly int min5 = 6_720;
    internal static readonly int min10 = 13_440;
    internal static readonly int min15 = 20_160;

    public static async Task<Sc2Replay?> GetSc2Replay(string replayPath)
    {
        if (!File.Exists(replayPath))
        {
            throw new FileNotFoundException("replay not found: {replay}", replayPath);
        }
        var decoder = new ReplayDecoder();
        return await decoder.DecodeAsync(replayPath);
    }

    public static async Task<Sc2Replay?> GetSc2Replay(Stream stream)
    {
        var decoder = new ReplayDecoder();
        return await decoder.DecodeAsync(stream);
    }

    /// <summary>
    /// ParseReplay
    /// </summary>
    /// <param name="replay"></param>
    /// <param name="compat">if true compute raw v2 hash in CompatHash</param>
    /// <returns></returns>
    public static ReplayDto ParseReplay(Sc2Replay replay, bool compat = true)
    {
        var dsReplay = new DsstatsReplay();
        ParseHeader(replay.Header, dsReplay);
        ParseDetails(replay.Details, dsReplay);
        ParseMetadata(replay.Metadata, dsReplay);
        ParseMessageEvents(replay.ChatMessages, replay.PingMessages, dsReplay);
        ParseTrackerEvents(replay.TrackerEvents, dsReplay);
        SetMiddleIncome(dsReplay);
        if (compat)
        {
            SetCompatStats(dsReplay);
        }

        foreach (var player in dsReplay.Players)
        {
            var victoryUpgrade = player.Upgrades.FirstOrDefault(f => f.Key == "PlayerStateVictory").Value;
            if (victoryUpgrade > 0)
            {
                dsReplay.Duration = Math.Max(dsReplay.Duration, victoryUpgrade);
                dsReplay.WinnerTeam = player.TeamId;
                player.Result = PlayerResult.Win;
            }
            else if (dsReplay.WinnerTeam == player.TeamId)
            {
                player.Result = PlayerResult.Win;
            }
            else
            {
                player.Result = PlayerResult.Los;
            }
        }

        if (dsReplay.WinnerTeam == 0)
        {
            dsReplay.Players.ForEach(f => f.Result = PlayerResult.None);
        }

        if (compat)
        {
            var replayDto = DsstatsReplayMapper.ToDto(dsReplay);
            replayDto.CompatHash = GenCompatHash(dsReplay, replayDto);
            return replayDto;
        }
        else
        {
            return DsstatsReplayMapper.ToDto(dsReplay);
        }
    }

    private static void ParseHeader(Header? header, DsstatsReplay replay)
    {
        if (header is null)
        {
            return;
        }
        replay.BaseBuild = header.BaseBuild;
    }

    private static void ParseDetails(Details? details, DsstatsReplay replay)
    {
        if (details is null)
        {
            return;
        }
        replay.Gametime = details.DateTimeUTC;


        replay.Players = details.Players
            .Where(x => x.Observe == 0)
            .Select((s, index) => new DsPlayer()
            {
                PlayerId = index + 1,
                Name = s.Name,
                Clan = s.ClanName,
                Race = ParseRace(s.Race),
                ToonId = new()
                {
                    Region = s.Toon.Region,
                    Realm = s.Toon.Realm,
                    Id = s.Toon.Id,
                },
                Control = s.Control,
                TeamId = s.TeamId,
                WorkingSetSlotId = s.WorkingSetSlotId,
                Observe = s.Observe,
                Result = (PlayerResult)s.Result,
            }).ToList();
    }

    private static Commander ParseRace(string race)
    {
        if (Enum.TryParse(typeof(Commander), race, out var cmdrObj)
            && cmdrObj is Commander cmdr)
        {
            return cmdr;
        }
        return Commander.None;
    }

    private static void ParseMetadata(ReplayMetadata? metadata, DsstatsReplay replay)
    {
        if (metadata is null)
        {
            return;
        }
        replay.Title = metadata.Title;
        replay.Version = metadata.GameVersion;
        // replay.Duration = metadata.Duration;

        if (metadata.Players.Count != replay.Players.Count)
        {
            return;
        }

        for (int i = 0; i < metadata.Players.Count; i++)
        {
            var player = metadata.Players.ElementAt(i);
            var dsPlayer = replay.Players[i];
            dsPlayer.MetadataPlayerId = player.PlayerID;
            dsPlayer.Apm = (int)player.APM;
            dsPlayer.SelectedRace = player.SelectedRace switch
            {
                "Rand" => Commander.Random,
                "Prot" => Commander.Protoss,
                "Terr" => Commander.Terran,
                "Zerg" => Commander.Zerg,
                _ => Commander.None,
            };
        }
    }

    private static void ParseMessageEvents(ICollection<ChatMessageEvent>? chatMessages,
                                          ICollection<PingMessageEvent>? pingMessages,
                                          DsstatsReplay replay)
    {
        if (chatMessages is not null)
        {
            foreach (var msg in chatMessages)
            {
                var player = replay.Players.FirstOrDefault(f => f.PlayerId == msg.UserId);
                if (player is null)
                {
                    continue;
                }
                player.Messages++;
            }
        }
        if (pingMessages is not null)
        {
            foreach (var msg in pingMessages)
            {
                var player = replay.Players.FirstOrDefault(f => f.PlayerId == msg.UserId);
                if (player is null)
                {
                    continue;
                }
                player.Pings++;
            }
        }
    }

    private static void SetMiddleIncome(DsstatsReplay replay)
    {
        (int team1, int team2) = GetMiddleIncome(replay, replay.Duration);
        replay.MiddleIncome[Breakpoint.All] = new() { Team1 = team1, Team2 = team2 };

        if (replay.Duration > min5)
        {
            (int team15, int team25) = GetMiddleIncome(replay, min5);
            replay.MiddleIncome[Breakpoint.Min5] = new() { Team1 = team15, Team2 = team25 };
        }
        if (replay.Duration > min10)
        {
            (int team110, int team210) = GetMiddleIncome(replay, min10);
            replay.MiddleIncome[Breakpoint.Min10] = new() { Team1 = team110, Team2 = team210 };
        }
        if (replay.Duration > min15)
        {
            (int team115, int team215) = GetMiddleIncome(replay, min15);
            replay.MiddleIncome[Breakpoint.Min15] = new() { Team1 = team115, Team2 = team215 };
        }
    }

    private static (int, int) GetMiddleIncome(DsstatsReplay replay, int targetGameloop)
    {
        if (replay.MiddleChanges.Count == 0 || replay.Duration <= 0)
        {
            return (0, 0);
        }

        int team1control = 0;
        int team2control = 0;

        int currentGameloop = 0;
        int currentTeam = 0;

        foreach (var middle in replay.MiddleChanges)
        {
            if (middle.Gameloop > targetGameloop)
            {
                var controlledGameloops = targetGameloop - currentGameloop;
                if (controlledGameloops > 0)
                {
                    if (currentTeam == 1)
                    {
                        team1control += controlledGameloops;
                    }
                    else if (currentTeam == 2)
                    {
                        team2control += controlledGameloops;
                    }
                }
                return (
                    (int)(team1control / 22.4),
                    (int)(team2control / 22.4)
                );
            }

            if (currentGameloop == 0)
            {
                // first team taking middle control
                currentTeam = middle.ControlTeam;
                currentGameloop = middle.Gameloop;
            }
            else
            {
                var controlledGameloops = middle.Gameloop - currentGameloop;
                if (currentTeam == 1)
                {
                    team2control += controlledGameloops;
                }
                else
                {
                    team1control += controlledGameloops;
                }

                currentTeam = middle.ControlTeam;
                currentGameloop = middle.Gameloop;
            }
        }

        var finalControlledGameloops = targetGameloop - currentGameloop;
        if (finalControlledGameloops > 0)
        {
            if (currentTeam == 1)
                team1control += finalControlledGameloops;
            else if (currentTeam == 2)
                team2control += finalControlledGameloops;
        }

        return (
            (int)(team1control / 22.4),
            (int)(team2control / 22.4)
        );

    }

    private static string GenCompatHash(DsstatsReplay replay, ReplayDto replayDto)
    {
        if (replay.Players.Count == 0)
        {
            return string.Empty;
        }
        var minArmy = replay.Players.Min(m => m.SpawnStats.ArmyValue);
        var minKills = replay.Players.Min(m => m.SpawnStats.KilledValue);
        var minIncome = replay.Players.Min(m => m.SpawnStats.Income);
        var maxKills = replay.Players.Max(m => m.SpawnStats.KilledValue);

        StringBuilder sb = new();
        foreach (var pl in replay.Players.OrderBy(o => o.GamePos))
        {
            sb.Append(pl.GamePos + pl.Race + pl.ToonId.Id);
        }
        sb.Append(replayDto.GameMode + replayDto.Players.Count);
        sb.Append(minArmy + minKills + minIncome + maxKills);
        return sb.ToString();
    }
}
