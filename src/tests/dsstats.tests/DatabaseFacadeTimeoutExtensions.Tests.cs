using dsstats.db;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace dsstats.tests;

[TestClass]
public sealed class DatabaseFacadeTimeoutExtensionsTests
{
    [TestMethod]
    public async Task ExecuteWithCommandTimeoutAsync_RestoresPreviousTimeout_WhenOperationFails()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DsstatsContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new DsstatsContext(options);
        context.Database.SetCommandTimeout(7);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            context.Database.ExecuteWithCommandTimeoutAsync(TimeSpan.FromSeconds(42), async () =>
            {
                Assert.AreEqual(42, context.Database.GetCommandTimeout());
                await Task.Yield();
                throw new InvalidOperationException("test failure");
            }));

        Assert.AreEqual(7, context.Database.GetCommandTimeout());
    }
}
