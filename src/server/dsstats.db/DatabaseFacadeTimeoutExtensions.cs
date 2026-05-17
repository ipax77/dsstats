using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace dsstats.db;

public static class DatabaseFacadeTimeoutExtensions
{
    public static async Task ExecuteWithCommandTimeoutAsync(
        this DatabaseFacade database,
        TimeSpan timeout,
        Func<Task> operation)
    {
        var previousTimeout = database.GetCommandTimeout();
        database.SetCommandTimeout(timeout);

        try
        {
            await operation();
        }
        finally
        {
            database.SetCommandTimeout(previousTimeout);
        }
    }
}
