using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBroodlings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"UPDATE DsUnits u
INNER JOIN DsWeapons w ON w.DsUnitId = u.DsUnitId
SET u.Life = 20, w.AttackSpeed = 0.57
WHERE u.Name = 'Broodling' AND u.Commander = 80;";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
