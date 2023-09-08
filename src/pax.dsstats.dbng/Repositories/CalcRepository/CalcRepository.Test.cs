
using System.Text;
using MySqlConnector;
using pax.dsstats.shared.Calc;

namespace dsstats.ratings.db;

public partial class CalcRepository
{
    public async Task<List<CalcDto>> TestGetDsstatsCalcDtos(int playerId)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var commandTxt =
$@"SELECT r.ReplayId, r.GameTime, r.GameMode, r.Duration, r.TournamentEdition, rp.ReplayPlayerId, rp.GamePos, rp.PlayerResult, IF(rp.Duration < r.Duration - 90, 1, 0) AS Isleaver, rp.Team, p.ToonId, p.RegionId, p.RealmId
FROM (
    SELECT ri.ReplayId
    FROM Replays as ri
    INNER JOIN ReplayPlayers as rpi on rpi.ReplayId = ri.ReplayId
    INNER JOIN Players as pi on pi.PlayerId = rpi.PlayerId
    WHERE GameTime > '2021-02-01'
        AND ri.Playercount = 6
        AND ri.Duration >= 300
        AND ri.WinnerTeam > 0
        AND ri.GameMode in (3, 4, 7)
        AND pi.PlayerId = {playerId}
    ORDER BY GameTime ASC
    -- LIMIT 10000 OFFSET 0 -- Limit the number of replays
) AS limited_replays
INNER JOIN Replays AS r ON r.ReplayId = limited_replays.ReplayId
INNER JOIN ReplayPlayers AS rp ON rp.ReplayId = r.ReplayId
INNER JOIN Players AS p ON p.PlayerId = rp.PlayerId
ORDER BY r.GameTime;";

        using var command = new MySqlCommand(commandTxt, connection);
        using var reader = await command.ExecuteReaderAsync();

        Dictionary<int, CalcDto> calcDtos = new();

        while (await reader.ReadAsync())
        {
            int id = reader.GetInt32(0);
            DateTime gameTime = reader.GetDateTime(1);
            int gameMode = reader.GetInt32(2);
            int duration = reader.GetInt32(3);
            bool tournamentEdition = reader.GetBoolean(4);
            int replayPlayerId = reader.GetInt32(5);
            int gamePos = reader.GetInt32(6);
            int playerResult = reader.GetInt32(7);
            bool isLeaver = reader.GetBoolean(8);
            int team = reader.GetInt32(9);
            int profileId = reader.GetInt32(10);
            int regionId = reader.GetInt32(11);
            int realmId = reader.GetInt32(12);

            if (!calcDtos.TryGetValue(id, out var calcDto))
            {
                calcDto = new()
                {
                    DsstatsReplayId = id,
                    GameTime = gameTime,
                    GameMode = gameMode,
                    Duration = duration,
                    TournamentEdition = tournamentEdition
                };
                calcDtos.Add(id, calcDto);
            }

            calcDto.Players.Add(new()
            {
                ReplayPlayerId = replayPlayerId,
                GamePos = gamePos,
                PlayerResult = playerResult,
                IsLeaver = isLeaver,
                Team = team,
                ProfileId = profileId,
                RealmId = realmId,
                RegionId = regionId,
            });
        }
        return calcDtos.Values.ToList();
    }

    public async Task<List<CalcDto>> TestGetSc2ArcadeCalcDtos(int playerId)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var commandTxt =
$@"SELECT r.ArcadeReplayId, r.CreatedAt, r.GameMode, r.Duration, rp.ArcadeReplayPlayerId, rp.SlotNumber, rp.PlayerResult, rp.Team, p.ProfileId, p.RegionId, p.RealmId
FROM (
    SELECT ri.ArcadeReplayId
    FROM ArcadeReplays as ri
    INNER JOIN ArcadeReplayPlayers as rpi on rpi.ArcadeReplayId = ri.ArcadeReplayId
    INNER JOIN ArcadePlayers as pi on pi.ArcadePlayerId = rpi.ArcadePlayerId
    WHERE ri.CreatedAt > '2021-02-01'
        AND ri.PlayerCount = 6
        AND ri.Duration >= 300
        AND ri.WinnerTeam > 0
        AND ri.GameMode in (3, 4, 7)
        AND pi.ArcadePlayerId = {playerId}
    ORDER BY ri.CreatedAt ASC
    -- LIMIT 10000 OFFSET 0 -- Limit the number of replays
) AS limited_replays
INNER JOIN ArcadeReplays as r on r.ArcadeReplayId = limited_replays.ArcadeReplayId
INNER JOIN ArcadeReplayPlayers as rp ON rp.ArcadeReplayId = r.ArcadeReplayId
INNER JOIN ArcadePlayers as p ON p.ArcadePlayerId = rp.ArcadePlayerId
WHERE r.CreatedAt > '2021-02-01'
    AND r.PlayerCount = 6
    AND r.Duration >= 240
    AND r.WinnerTeam > 0
    AND !r.TournamentEdition
    AND r.GameMode in (3, 4, 7);";

        using var command = new MySqlCommand(commandTxt, connection);
        using var reader = await command.ExecuteReaderAsync();

        Dictionary<int, CalcDto> calcDtos = new();

        while (await reader.ReadAsync())
        {
            int id = reader.GetInt32(0);
            DateTime gameTime = reader.GetDateTime(1);
            int gameMode = reader.GetInt32(2);
            int duration = reader.GetInt32(3);
            int replayPlayerId = reader.GetInt32(4);
            int gamePos = reader.GetInt32(5);
            int playerResult = reader.GetInt32(6);
            int team = reader.GetInt32(7);
            int profileId = reader.GetInt32(8);
            int regionId = reader.GetInt32(9);
            int realmId = reader.GetInt32(10);

            if (!calcDtos.TryGetValue(id, out var calcDto))
            {
                calcDto = new()
                {
                    Sc2ArcadeReplayId = id,
                    GameTime = gameTime,
                    GameMode = gameMode,
                    Duration = duration
                };
                calcDtos.Add(id, calcDto);
            }

            calcDto.Players.Add(new()
            {
                ReplayPlayerId = replayPlayerId,
                GamePos = gamePos,
                PlayerResult = playerResult,
                Team = team,
                ProfileId = profileId,
                RealmId = realmId,
                RegionId = regionId,
            });
        }
        return calcDtos.Values.ToList();
    }
}