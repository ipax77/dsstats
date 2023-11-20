using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace dsstats.ratings;

public partial class RatingsSaveService(IServiceScopeFactory scopeFactory, ILogger<RatingsSaveService> logger) : IRatingsSaveService
{
    private static readonly string mysqlDir = "/data/mysqlfiles";


    private static string GetFileName(RatingCalcType calcType, string job)
    {
        var path = Path.Combine(mysqlDir, $"{calcType}_{job}.csv");
        return path.Replace("\\", "/");
    }

    private async Task SetPlayerRatingsPos(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "CALL SetPlayerRatingPos();";
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed setting player rating pos: {error}", ex.Message);
        }
    }

    private async Task SetRatingChange(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "CALL SetRatingChange();";
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed setting player rating change: {error}", ex.Message);
        }
    }

    private async Task SetComboPlayerRatingsPos(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "CALL SetComboPlayerRatingPos();";
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed setting combo player rating change: {error}", ex.Message);
        }
    }

    private async Task SetArcadePlayerRatingsPos(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "CALL SetArcadePlayerRatingPos();";
            command.CommandTimeout = 500;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed setting arcade player rating pos: {error}", ex.Message);
        }
    }

    //private async Task SetArcadeRatingChange(string connectionString)
    //{
    //    try
    //    {
    //        using var connection = new MySqlConnection(connectionString);
    //        await connection.OpenAsync();
    //        var command = connection.CreateCommand();
    //        command.CommandText = "CALL SetArcadeRatingChange();";
    //        command.CommandTimeout = 500;
    //        await command.ExecuteNonQueryAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError("failed setting arcade player rating changes: {error}", ex.Message);
    //    }
    //}
}
