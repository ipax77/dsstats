using dsstats.shared;
using dsstats.shared.InHouse;
using s2protocol.NET;
using s2protocol.NET.Models;
using DirectStrikeObserver = Sc2DirectStrike.Parser.DirectStrikeObserver;
using DirectStrikeReplay = Sc2DirectStrike.Parser.DirectStrikeReplay;
using Sc2DirectStrikeParser = Sc2DirectStrike.Parser.Sc2DirectStrikeParser;

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
    /// Parses a Direct Strike replay and maps it to the dsstats DTO contract.
    /// </summary>
    /// <param name="replay">Decoded SC2 replay.</param>
    /// <param name="compat">Kept for source compatibility. The parser always emits compat hashes.</param>
    public static ReplayDto ParseReplay(Sc2Replay replay, bool compat = true)
    {
        ArgumentNullException.ThrowIfNull(replay);

        DirectStrikeReplay directStrikeReplay = Sc2DirectStrikeParser.Parse(replay);
        ReplayDto dto = DirectStrikeReplayDtoMapper.Map(replay, directStrikeReplay);
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

        DirectStrikeReplay directStrikeReplay = Sc2DirectStrikeParser.Parse(replay);
        ReplayDto dto = DirectStrikeReplayDtoMapper.Map(replay, directStrikeReplay);
        SpawnPlaybackEncodedSidecar? encodedSidecar = null;
        try
        {
            var sidecar = SpawnPlaybackSidecarFactory.Create(replay, directStrikeReplay);
            if (sidecar is not null)
            {
                Func<SpawnPlaybackSidecarDto, SpawnPlaybackEncodedSidecar> encoder =
                    spawnPlaybackEncoder ?? (sidecarDto => SpawnPlaybackSidecarCodec.EncodeWithMetadata(sidecarDto));
                encodedSidecar = encoder(sidecar);
                ApplySpawnPlaybackMetadata(dto, encodedSidecar);
            }
        }
        catch (Exception ex) when (tolerateSpawnPlaybackErrors)
        {
            onSpawnPlaybackError?.Invoke(ex);
            encodedSidecar = null;
            dto.SpawnPlayback = null;
        }

        return new(dto, encodedSidecar);
    }

    public static DirectStrikeReplay ParseDirectStrikeReplay(Sc2Replay replay)
    {
        ArgumentNullException.ThrowIfNull(replay);

        return Sc2DirectStrikeParser.Parse(replay);
    }

    public static InHouseParsedReplayDto ParseInHouseReplay(Sc2Replay replay)
    {
        ArgumentNullException.ThrowIfNull(replay);

        var directStrikeReplay = Sc2DirectStrikeParser.Parse(replay);
        var dto = DirectStrikeReplayDtoMapper.Map(replay, directStrikeReplay);
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

    private static InHouseReplayObserverDto ToDsstatsDto(DirectStrikeObserver observer)
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
