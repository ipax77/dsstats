using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class DsUnits0524 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"UPDATE DsAbilities
SET Description = REPLACE(Description, '+50% Lifesteal', '+10% Lifesteal')
WHERE Commander = 10 AND Description LIKE '%+50% Lifesteal%';
UPDATE DsUnits SET Cost = 300 WHERE Commander = 1 and Name = 'Colossus';
UPDATE DsUnits SET Cost = 700 WHERE Commander = 1 and Name = 'Mothership';
UPDATE DsUnits SET Cost = 105 WHERE Commander = 2 and Name = 'Widow Mine';
UPDATE DsUnits SET Cost = 265 WHERE Commander = 2 and Name = 'Siege Tank';
UPDATE DsUnits SET Cost = 425 WHERE Commander = 2 and Name = 'Thor';
UPDATE DsUnits SET Cost = 80 WHERE Commander = 3 and Name = 'Mutalisk';";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
