using Microsoft.Extensions.Options;
using MySqlConnector;
using pax.dsstats.shared;
using pax.dsstats.shared.Calc;
using pax.dsstats.shared.Interfaces;

namespace dsstats.ratings.db;

public partial class CalcRepository : ICalcRepository
{
    private readonly string connectionString;

    public CalcRepository(IOptions<DbImportOptions> dbOptions)
    {
        connectionString = dbOptions.Value.ImportConnectionString;
    }

    public async Task<List<CalcDto>> GetDsstatsCalcDtos(DsstatsCalcRequest request)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var commandTxt =
$@"SELECT r.ReplayId, r.GameTime, r.GameMode, r.Duration, r.TournamentEdition, rp.ReplayPlayerId, rp.GamePos, rp.PlayerResult, IF(rp.Duration < r.Duration - 90, 1, 0) AS Isleaver, rp.Team, p.ToonId, p.RegionId, p.RealmId
FROM (
    SELECT ReplayId
    FROM Replays
    WHERE GameTime > '{request.FromDate.ToString("yyyy-MM-dd")}' 
        AND Playercount = 6
        AND Duration >= 300
        AND WinnerTeam > 0
        AND GameMode in ({string.Join(", ", request.GameModes)})
    ORDER BY GameTime ASC
    LIMIT {request.Take} OFFSET {request.Skip} -- Limit the number of replays
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

    public async Task<List<CalcDto>> GetSc2ArcadeCalcDtos(Sc2ArcadeRequest request)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var commandTxt =
$@"SELECT r.ArcadeReplayId, r.CreatedAt, r.GameMode, r.Duration, rp.ArcadeReplayPlayerId, rp.SlotNumber, rp.PlayerResult, rp.Team, p.ProfileId, p.RegionId, p.RealmId
FROM ArcadeReplays as r
INNER JOIN ArcadeReplayPlayers as rp ON rp.ArcadeReplayId = r.ArcadeReplayId
INNER JOIN ArcadePlayers as p ON p.ArcadePlayerId = rp.ArcadePlayerId
WHERE r.CreatedAt > '{request.FromDate.ToString("yyyy-MM-dd")}' AND r.CreatedAt <= '{request.ToDate.ToString("yyyy-MM-dd")}' 
    AND r.PlayerCount = 6
    AND r.Duration >= 240
    AND r.WinnerTeam > 0
    AND !r.TournamentEdition
    AND r.GameMode in ({string.Join(", ", request.GameModes)});";

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

