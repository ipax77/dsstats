using dsstats.db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices;

public partial class ImportService
{
    public async Task FixPlayerNames()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var sql = @"
                UPDATE Players p
            JOIN (
                SELECT
                    rp.PlayerId,
                    rp.Name,
                    ROW_NUMBER() OVER (
                        PARTITION BY rp.PlayerId
                        ORDER BY r.Gametime DESC
                    ) AS rn
                FROM ReplayPlayers rp
                JOIN Replays r ON r.ReplayId = rp.ReplayId
            ) latest
                ON latest.PlayerId = p.PlayerId
               AND latest.rn = 1
            SET p.Name = latest.Name
            WHERE p.Name IS NULL
               OR p.Name COLLATE utf8mb4_0900_ai_ci
                  <> latest.Name COLLATE utf8mb4_0900_ai_ci;
        ";
        var rows = await context.Database.ExecuteSqlRawAsync(sql);
        logger.LogWarning("Player names fixed: {rows}", rows);
    }
}
