using Microsoft.EntityFrameworkCore.Migrations;
using System.Reflection;

namespace dsstats.tests;

[TestClass]
public sealed class ReplayPlayerCompatHashMigrationTests
{
    [TestMethod]
    public void MySqlMigrationIsDiscoverable()
    {
        var migration = typeof(dsstats.migrations.mysql.Migrations.ReplayPlayerCompatHash)
            .GetCustomAttribute<MigrationAttribute>();

        Assert.IsNotNull(migration);
        Assert.AreEqual("20260510120000_ReplayPlayerCompatHash", migration.Id);
    }

    [TestMethod]
    public void SqliteMigrationIsDiscoverable()
    {
        var migration = typeof(dsstats.migrations.sqlite.Migrations.ReplayPlayerCompatHash)
            .GetCustomAttribute<MigrationAttribute>();

        Assert.IsNotNull(migration);
        Assert.AreEqual("20260510120001_ReplayPlayerCompatHash", migration.Id);
    }
}
