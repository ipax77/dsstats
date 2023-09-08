
using MySqlConnector;

namespace dsstats.ratings.db;

public partial class CalcRepository
{
    public async Task SetPlayerRatingsPos()
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "CALL SetPlayerRatingPos();";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }

    public async Task SetRatingChange()
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "CALL SetRatingChange();";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }
}