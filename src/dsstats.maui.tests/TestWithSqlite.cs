using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;

namespace dsstats.maui.tests;

public abstract class TestWithSqlite : IDisposable
{
    private const string InMemoryConnectionString = "DataSource=:memory:";
    protected readonly SqliteConnection _connection;

    protected readonly ReplayContext DbContext;

    protected TestWithSqlite()
    {
        _connection = new SqliteConnection(InMemoryConnectionString);
        _connection.Open();
        var options = new DbContextOptionsBuilder<ReplayContext>()
                .UseSqlite(_connection, p =>
                {
                    p.MigrationsAssembly("SqliteMigrations");
                })
                .Options;
        DbContext = new ReplayContext(options);
        //DbContext.Database.EnsureCreated();
        DbContext.Database.Migrate();
    }

    public void Dispose()
    {
        _connection.Close();
    }
}