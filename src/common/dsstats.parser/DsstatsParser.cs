using dsstats.shared;
using dsstats.shared.InHouse;
using s2protocol.NET;
using s2protocol.NET.Models;
using ExternalDirectStrikeObserver = Sc2DirectStrike.Parser.DirectStrikeObserver;
using ExternalDirectStrikeReplay = Sc2DirectStrike.Parser.DirectStrikeReplay;
using ExternalBreakpoint = Sc2DirectStrike.Parser.Breakpoint;
using ExternalCommander = Sc2DirectStrike.Parser.Commander;
using ExternalGameMode = Sc2DirectStrike.Parser.GameMode;
using ExternalPlayerResult = Sc2DirectStrike.Parser.PlayerResult;
using ExternalPlayerDto = Sc2DirectStrike.Parser.PlayerDto;
using ExternalReplayDto = Sc2DirectStrike.Parser.ReplayDto;
using ExternalReplayPlayerDto = Sc2DirectStrike.Parser.ReplayPlayerDto;
using ExternalSpawnDto = Sc2DirectStrike.Parser.SpawnDto;
using ExternalToonIdDto = Sc2DirectStrike.Parser.ToonIdDto;
using ExternalUnitDto = Sc2DirectStrike.Parser.UnitDto;
using ExternalUpgradeDto = Sc2DirectStrike.Parser.UpgradeDto;

namespace dsstats.parser;

public static class DsstatsParser
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
    /// Parses a Direct Strike replay through the external Sc2DirectStrike parser and maps it to the dsstats DTO contract.
    /// </summary>
    /// <param name="replay">Decoded SC2 replay.</param>
    /// <param name="compat">Kept for source compatibility. The external parser always emits compat hashes.</param>
    public static ReplayDto ParseReplay(Sc2Replay replay, bool compat = true)
    {
        ArgumentNullException.ThrowIfNull(replay);

        ExternalReplayDto externalReplay = Sc2DirectStrike.Parser.Sc2DirectStrikeParser.ParseDto(replay);
        ReplayDto dto = externalReplay.ToDsstatsDto();
        SetMvp(dto);
        return dto;
    }

    public static ReplayImportDto ParseReplayImport(
        Sc2Replay replay,
        bool compat = true,
        bool tolerateSpawnPlaybackErrors = true,
        Action<Exception>? onSpawnPlaybackError = null,
        Func<SpawnPlaybackSidecarDto, SpawnPlaybackEncodedSidecar>? spawnPlaybackEncoder = null)
    {
        ArgumentNullException.ThrowIfNull(replay);

        ExternalDirectStrikeReplay directStrikeReplay = Sc2DirectStrike.Parser.Sc2DirectStrikeParser.Parse(replay);
        ExternalReplayDto externalReplay = Sc2DirectStrike.Parser.Sc2DirectStrikeParser.ParseDto(replay, directStrikeReplay);
        ReplayDto dto = externalReplay.ToDsstatsDto();
        SetMvp(dto);

        SpawnPlaybackEncodedSidecar? encodedSidecar = null;
        try
        {
            var sidecar = SpawnPlaybackSidecarFactory.Create(replay, directStrikeReplay);
            Func<SpawnPlaybackSidecarDto, SpawnPlaybackEncodedSidecar> encoder =
                spawnPlaybackEncoder ?? (sidecarDto => SpawnPlaybackSidecarCodec.EncodeWithMetadata(sidecarDto));
            encodedSidecar = encoder(sidecar);
            ApplySpawnPlaybackMetadata(dto, encodedSidecar);
        }
        catch (Exception ex) when (tolerateSpawnPlaybackErrors)
        {
            onSpawnPlaybackError?.Invoke(ex);
            encodedSidecar = null;
            dto.SpawnPlayback = null;
        }

        return new(dto, encodedSidecar);
    }

    public static ExternalDirectStrikeReplay ParseDirectStrikeReplay(Sc2Replay replay)
    {
        ArgumentNullException.ThrowIfNull(replay);

        return Sc2DirectStrike.Parser.Sc2DirectStrikeParser.Parse(replay);
    }

    public static InHouseParsedReplayDto ParseInHouseReplay(Sc2Replay replay)
    {
        ArgumentNullException.ThrowIfNull(replay);

        var directStrikeReplay = Sc2DirectStrike.Parser.Sc2DirectStrikeParser.Parse(replay);
        var dto = Sc2DirectStrike.Parser.Sc2DirectStrikeParser.ParseDto(replay, directStrikeReplay).ToDsstatsDto();
        SetMvp(dto);

        return new()
        {
            Replay = dto,
            Observers = directStrikeReplay.Observers.Select(ToDsstatsDto).ToList(),
        };
    }

    public static ReplayTourneyInfoDto? GetMetaData(Sc2Replay replay)
    {
        List<TourneyPlayerDto> players = [];

        if (replay.Initdata is null || replay.Details is null || replay.Metadata is null)
        {
            return null;
        }

        for (int i = 0; i < replay.Initdata.UserInitialData.Count; i++)
        {
            var initData = replay.Initdata.UserInitialData.ElementAt(i);
            if (string.IsNullOrEmpty(initData.Name))
            {
                continue;
            }

            TourneyPlayerDto player = new()
            {
                Player = new()
                {
                    Name = initData.Name,
                },
            };
            players.Add(player);
        }

        for (int i = 0; i < replay.Initdata.LobbyState.Slots.Count; i++)
        {
            var slot = replay.Initdata.LobbyState.Slots.ElementAt(i);
            var player = players.ElementAtOrDefault(i);
            if (player is null)
            {
                continue;
            }

            player.Observer = slot.Observe == 1;
            player.WorkingSetSlotId = slot.WorkingSetSlotId;
        }

        for (int i = 0; i < replay.Metadata.Players.Count; i++)
        {
            var metaPlayer = replay.Metadata.Players.ElementAt(i);
            var player = players.ElementAtOrDefault(i);
            if (player is null)
            {
                continue;
            }

            player.AssignedRace = GetRace(metaPlayer.AssignedRace);
            player.SelectedRace = GetSelectedRace(metaPlayer.SelectedRace);
        }

        for (int i = 0; i < replay.Details.Players.Count; i++)
        {
            var detailPlayer = replay.Details.Players.ElementAt(i);
            var player = players.ElementAtOrDefault(i);
            if (player is null)
            {
                continue;
            }

            player.Player.Name = detailPlayer.Name;
            player.Player.ToonId = new()
            {
                Region = detailPlayer.Toon.Region,
                Realm = detailPlayer.Toon.Realm,
                Id = detailPlayer.Toon.Id,
            };
            player.PlayerColor = new()
            {
                A = detailPlayer.Color.A,
                R = detailPlayer.Color.R,
                G = detailPlayer.Color.G,
                B = detailPlayer.Color.B,
            };
        }

        return new()
        {
            Players = players
        };
    }

    private static void ApplySpawnPlaybackMetadata(ReplayDto replay, SpawnPlaybackEncodedSidecar sidecar)
    {
        replay.SpawnPlayback = new()
        {
            Available = true,
            FormatVersion = sidecar.FormatVersion,
            Compression = sidecar.Compression,
            CompressedLength = sidecar.CompressedLength,
            UncompressedLength = sidecar.UncompressedLength,
            UnitCount = sidecar.UnitCount,
        };
    }

    private static ReplayDto ToDsstatsDto(this ExternalReplayDto replay)
    {
        return new()
        {
            FileName = replay.FileName,
            CompatHash = replay.CompatHash,
            Title = replay.Title,
            Version = replay.Version,
            GameMode = ToGameMode(replay.GameMode),
            RegionId = replay.RegionId,
            Gametime = replay.Gametime,
            BaseBuild = replay.BaseBuild,
            Duration = ToSeconds(replay.Duration),
            Cannon = ToSeconds(replay.Cannon),
            Bunker = ToSeconds(replay.Bunker),
            WinnerTeam = replay.WinnerTeam,
            MiddleChanges = ToMiddleChanges(replay),
            Players = replay.Players.Select(ToDsstatsDto).ToList()
        };
    }

    private static ReplayPlayerDto ToDsstatsDto(ExternalReplayPlayerDto player)
    {
        return new()
        {
            CompatHash = player.CompatHash,
            Name = player.Name,
            Clan = player.Clan,
            Race = ToCommander(player.Race),
            SelectedRace = ToCommander(player.SelectedRace),
            TeamId = player.TeamId,
            GamePos = player.GamePos,
            Result = ToPlayerResult(player.Result),
            Duration = ToSeconds(player.Duration),
            Apm = player.Apm,
            Messages = player.Messages,
            Pings = player.Pings,
            IsMvp = player.IsMvp,
            Spawns = player.Spawns.Select(ToDsstatsDto).ToList(),
            Upgrades = player.Upgrades.Select(ToDsstatsDto).ToList(),
            TierUpgrades = player.TierUpgrades.Select(ToSeconds).ToList(),
            Refineries = player.Refineries.Select(ToSeconds).ToList(),
            Player = ToDsstatsDto(player.Player)
        };
    }

    private static InHouseReplayObserverDto ToDsstatsDto(ExternalDirectStrikeObserver observer)
    {
        return new()
        {
            Name = observer.Name,
            Clan = observer.Clan,
            SlotId = observer.SlotId,
            ToonId = new ToonIdDto
            {
                Region = observer.Region,
                Realm = observer.Realm,
                Id = observer.Id,
            },
        };
    }

    private static PlayerDto ToDsstatsDto(ExternalPlayerDto player)
    {
        return new()
        {
            PlayerId = player.PlayerId,
            Name = player.Name,
            ToonId = ToDsstatsDto(player.ToonId)
        };
    }

    private static ToonIdDto ToDsstatsDto(ExternalToonIdDto toonId)
    {
        return new()
        {
            Region = toonId.Region,
            Realm = toonId.Realm,
            Id = toonId.Id
        };
    }

    private static SpawnDto ToDsstatsDto(ExternalSpawnDto spawn)
    {
        return new()
        {
            Breakpoint = ToBreakpoint(spawn.Breakpoint),
            Income = spawn.Income,
            GasCount = spawn.GasCount,
            ArmyValue = spawn.ArmyValue,
            KilledValue = spawn.KilledValue,
            LostValue = spawn.LostValue,
            UpgradeSpent = spawn.UpgradeSpent,
            Units = spawn.Units.Select(ToDsstatsDto).ToList()
        };
    }

    private static UnitDto ToDsstatsDto(ExternalUnitDto unit)
    {
        return new()
        {
            Name = unit.Name,
            Count = unit.Count,
            Positions = unit.Positions.ToList()
        };
    }

    private static UpgradeDto ToDsstatsDto(ExternalUpgradeDto upgrade)
    {
        return new()
        {
            Name = upgrade.Name,
            Gameloop = ToSeconds(upgrade.Time)
        };
    }

    private static List<int> ToMiddleChanges(ExternalReplayDto replay)
    {
        if (replay.FirstTeamCrossedMiddle is not (1 or 2) || replay.MiddleChanges.Count == 0)
        {
            return [];
        }

        List<int> middleChanges = new(replay.MiddleChanges.Count + 1)
        {
            replay.FirstTeamCrossedMiddle
        };
        middleChanges.AddRange(replay.MiddleChanges.Select(ToSeconds));
        return middleChanges;
    }

    private static int ToSeconds(TimeSpan value)
    {
        return value <= TimeSpan.Zero ? 0 : (int)value.TotalSeconds;
    }

    private static GameMode ToGameMode(ExternalGameMode value) => (GameMode)(int)value;

    private static Commander ToCommander(ExternalCommander value) => (Commander)(int)value;

    private static Breakpoint ToBreakpoint(ExternalBreakpoint value) => (Breakpoint)(int)value;

    private static PlayerResult ToPlayerResult(ExternalPlayerResult value)
    {
        return value switch
        {
            ExternalPlayerResult.Win => PlayerResult.Win,
            ExternalPlayerResult.Loss => PlayerResult.Los,
            _ => PlayerResult.None
        };
    }

    private static Commander GetRace(string race)
    {
        if (Enum.TryParse(typeof(Commander), race, out var cmdrObj)
            && cmdrObj is Commander cmdr)
        {
            return cmdr;
        }

        return Commander.None;
    }

    private static Commander GetSelectedRace(string selectedRace)
    {
        var race = selectedRace switch
        {
            "Terr" => "Terran",
            "Prot" => "Protoss",
            "Rand" => "None",
            _ => selectedRace
        };
        return GetRace(race);
    }

    private static void SetMvp(ReplayDto replay)
    {
        List<SpawnDto> allSpawns = replay.Players
            .SelectMany(player => player.Spawns)
            .Where(spawn => spawn.Breakpoint == Breakpoint.All)
            .ToList();

        if (allSpawns.Count == 0)
        {
            return;
        }

        int maxKills = allSpawns.Max(spawn => spawn.KilledValue);
        foreach (ReplayPlayerDto player in replay.Players.Where(player => player.Spawns.Any(spawn => spawn.KilledValue == maxKills)))
        {
            player.IsMvp = true;
        }
    }

    internal static (int, int) GetMiddleIncome(DsstatsReplay replay, int targetGameloop)
    {
        if (replay.MiddleChanges.Count == 0 || replay.Duration <= 0)
        {
            return (0, 0);
        }

        int team1Control = 0;
        int team2Control = 0;
        int currentGameloop = 0;
        int currentTeam = 0;

        foreach (DsMiddle middle in replay.MiddleChanges)
        {
            if (middle.Gameloop > targetGameloop)
            {
                int controlledGameloops = targetGameloop - currentGameloop;
                if (controlledGameloops > 0)
                {
                    if (currentTeam == 1)
                    {
                        team1Control += controlledGameloops;
                    }
                    else if (currentTeam == 2)
                    {
                        team2Control += controlledGameloops;
                    }
                }

                return ((int)(team1Control / 22.4), (int)(team2Control / 22.4));
            }

            if (currentGameloop == 0)
            {
                currentTeam = middle.ControlTeam;
                currentGameloop = middle.Gameloop;
            }
            else
            {
                int controlledGameloops = middle.Gameloop - currentGameloop;
                if (currentTeam == 1)
                {
                    team1Control += controlledGameloops;
                }
                else
                {
                    team2Control += controlledGameloops;
                }

                currentTeam = middle.ControlTeam;
                currentGameloop = middle.Gameloop;
            }
        }

        int finalControlledGameloops = targetGameloop - currentGameloop;
        if (finalControlledGameloops > 0)
        {
            if (currentTeam == 1)
            {
                team1Control += finalControlledGameloops;
            }
            else if (currentTeam == 2)
            {
                team2Control += finalControlledGameloops;
            }
        }

        return ((int)(team1Control / 22.4), (int)(team2Control / 22.4));
    }
}
