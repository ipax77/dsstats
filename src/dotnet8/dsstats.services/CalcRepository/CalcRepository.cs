using Microsoft.Extensions.Options;
using MySqlConnector;
using dsstats.shared;
using dsstats.shared.Calc;
using dsstats.shared.Interfaces;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using dsstats.db;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.services;

public partial class CalcRepository : ICalcRepository
{
    private readonly string connectionString;
    private readonly bool IsSqlite;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<CalcRepository> logger;

    public CalcRepository(IServiceScopeFactory serviceScopeFactory, IOptions<DbImportOptions> dbOptions, ILogger<CalcRepository> logger)
    {
        connectionString = dbOptions.Value.ImportConnectionString;
        IsSqlite = dbOptions.Value.IsSqlite;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    public async Task<List<CalcDto>> GetDsstatsCalcDtos(DsstatsCalcRequest request)
    {
        if (IsSqlite)
        {
            return await GetSqliteDsstatsCalcDtos(request);
        }

        var commandTxt =
$@"SELECT r.ReplayId, r.GameTime, r.GameMode, r.Duration, r.TournamentEdition, rp.ReplayPlayerId, rp.GamePos, rp.PlayerResult, rp.Race, IF(rp.Duration < r.Duration - 90, 1, 0) AS Isleaver, IF(rp.Kills = r.Maxkillsum, 1, 0) AS IsMvp, rp.Team, IF(p.UploaderId is null, 0, 1) AS IsUploder, p.ToonId, p.RegionId, p.RealmId
FROM (
    SELECT r1.ReplayId
    FROM Replays AS r1
    {(request.Continue ? "LEFT JOIN ReplayRatings as rr on r1.ReplayId = rr.ReplayId" : "")}
    WHERE GameTime > '{request.FromDate.ToString("yyyy-MM-dd")}' 
        AND r1.Playercount = 6
        AND r1.Duration >= 300
        AND r1.WinnerTeam > 0
        AND r1.GameMode in ({string.Join(", ", request.GameModes)})
        {(request.Continue ? "AND rr.ReplayId IS NULL" : "")}
    ORDER BY r1.GameTime ASC
    LIMIT {request.Take} OFFSET {request.Skip}
) AS limited_replays
INNER JOIN Replays AS r ON r.ReplayId = limited_replays.ReplayId
INNER JOIN ReplayPlayers AS rp ON rp.ReplayId = r.ReplayId
INNER JOIN Players AS p ON p.PlayerId = rp.PlayerId
ORDER BY r.GameTime;";

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

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
            int race = reader.GetInt32(8);
            bool isLeaver = reader.GetBoolean(9);
            bool isMvp = reader.GetBoolean(10);
            int team = reader.GetInt32(11);
            bool isUploader = reader.GetBoolean(12);
            int profileId = reader.GetInt32(13);
            int regionId = reader.GetInt32(14);
            int realmId = reader.GetInt32(15);

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
                Race = (Commander)race,
                IsLeaver = isLeaver,
                IsMvp = isMvp,
                Team = team,
                IsUploader = isUploader,
                PlayerId = new(profileId, realmId, regionId)
            });
        }
        return calcDtos.Values.ToList();
    }

    public async Task<List<CalcDto>> GetSqliteDsstatsCalcDtos(DsstatsCalcRequest request)
    {
        var sql = $@"
SELECT r.ReplayId, r.GameTime, r.GameMode, r.Duration, r.TournamentEdition, rp.ReplayPlayerId, rp.GamePos, rp.PlayerResult, rp.Race,
    CASE WHEN rp.Duration < r.Duration - 90 THEN 1 ELSE 0 END AS Isleaver,
    CASE WHEN rp.Kills = r.Maxkillsum THEN 1 ELSE 0 END AS IsMvp,
    rp.Team,
    CASE WHEN p.UploaderId IS NULL THEN 0 ELSE 1 END AS IsUploder,
    p.ToonId, p.RegionId, p.RealmId
FROM (
    SELECT r1.ReplayId
    FROM Replays AS r1
    {(request.Continue ? "LEFT JOIN ReplayRatings as rr on r1.ReplayId = rr.ReplayId" : "")}
    WHERE GameTime > '{request.FromDate.ToString("yyyy-MM-dd")}' 
        AND r1.Playercount = 6
        AND r1.Duration >= 300
        AND r1.WinnerTeam > 0
        AND r1.GameMode in ({string.Join(", ", request.GameModes)})
        {(request.Continue ? "AND rr.ReplayId IS NULL" : "")}
    ORDER BY r1.GameTime ASC
    LIMIT {request.Take} OFFSET {request.Skip}
) AS limited_replays
INNER JOIN Replays AS r ON r.ReplayId = limited_replays.ReplayId
INNER JOIN ReplayPlayers AS rp ON rp.ReplayId = r.ReplayId
INNER JOIN Players AS p ON p.PlayerId = rp.PlayerId
ORDER BY r.GameTime;";

        using var connection = new SQLiteConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SQLiteCommand(sql, connection);
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
            int race = reader.GetInt32(8);
            bool isLeaver = reader.GetBoolean(9);
            bool isMvp = reader.GetBoolean(10);
            int team = reader.GetInt32(11);
            bool isUploader = reader.GetBoolean(12);
            int profileId = reader.GetInt32(13);
            int regionId = reader.GetInt32(14);
            int realmId = reader.GetInt32(15);

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
                Race = (Commander)race,
                IsLeaver = isLeaver,
                IsMvp = isMvp,
                Team = team,
                IsUploader = isUploader,
                PlayerId = new(profileId, realmId, regionId)
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
                PlayerId = new(profileId, realmId, regionId)
            });
        }
        return calcDtos.Values.ToList();
    }
}

