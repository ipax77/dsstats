using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Patch241130 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql1 = @"INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES 
(1, '2024-11-30', 0, '- Stalker cost increased from 110 to 115'),
(1, '2024-11-30', 0, '- Archon/High Templar cost reduced from 275 to 265'),
(1, '2024-11-30', 0, '- Immortal cost reduced from 255 to 245'),
(1, '2024-11-30', 0, '- Phoenix cost reduced from 140 to 135'),
(1, '2024-11-30', 0, '- Void ray cost reduced from 250 to 240'),
(1, '2024-11-30', 0, '- Tempest cost reduced from 335 to 325'),
(2, '2024-11-30', 0, '- Ghost cost reduced from 235 to 230'),
(2, '2024-11-30', 0, '- Siege Tank cost reduced from 265 to 260'),
(3, '2024-11-30', 0, '- Hydralisk cost increased from 95 to 100'),
(3, '2024-11-30', 0, '- Added Lunge.');
";
            migrationBuilder.Sql(sql1);

            var sql2 = @"UPDATE DsUnits SET Cost = 125 WHERE Commander = 1 and Name = 'Stalker';
UPDATE DsUnits SET Cost = 265 WHERE Commander = 1 and Name = 'High Templar';
UPDATE DsUnits SET Cost = 265 WHERE Commander = 1 and Name = 'Archon';
UPDATE DsUnits SET Cost = 245 WHERE Commander = 1 and Name = 'Immortal';
UPDATE DsUnits SET Cost = 135 WHERE Commander = 1 and Name = 'Phoenix';
UPDATE DsUnits SET Cost = 240 WHERE Commander = 1 and Name = 'Void Ray';
UPDATE DsUnits SET Cost = 325 WHERE Commander = 1 and Name = 'Tempest';
UPDATE DsUnits SET Cost = 230 WHERE Commander = 2 and Name = 'Ghost';
UPDATE DsUnits SET Cost = 260 WHERE Commander = 2 and Name = 'Siege Tank';
UPDATE DsUnits SET Cost = 100 WHERE Commander = 3 and Name = 'Hydralisk';

";
            migrationBuilder.Sql(sql2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
