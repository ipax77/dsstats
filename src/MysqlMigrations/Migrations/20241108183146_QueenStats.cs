using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class QueenStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var updateAttacksSql = @"
                UPDATE DsWeapons
                SET Attacks = 1
                WHERE `Name` = 'Acid Spines' AND `Range` = 7;
            ";
            migrationBuilder.Sql(updateAttacksSql);

            // 2. Delete related BonusDamages for those records
            var deleteBonusDamagesSql = @"
                DELETE bd
                FROM BonusDamages AS bd
                INNER JOIN DsWeapons AS dw ON bd.DsWeaponId = dw.DsWeaponId
                WHERE dw.`Name` = 'Acid Spines' AND dw.`Range` = 7;
            ";
            migrationBuilder.Sql(deleteBonusDamagesSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
