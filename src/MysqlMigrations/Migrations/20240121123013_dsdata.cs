using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class dsdata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DsUnits",
                columns: table => new
                {
                    DsUnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    Cost = table.Column<int>(type: "int", nullable: false),
                    Life = table.Column<int>(type: "int", nullable: false),
                    Shields = table.Column<int>(type: "int", nullable: false),
                    Speed = table.Column<float>(type: "float", nullable: false),
                    Armor = table.Column<int>(type: "int", nullable: false),
                    ShieldArmor = table.Column<int>(type: "int", nullable: false),
                    StartingEnergy = table.Column<int>(type: "int", nullable: false),
                    MaxEnergy = table.Column<int>(type: "int", nullable: false),
                    HealthRegen = table.Column<float>(type: "float", nullable: false),
                    EnergyRegen = table.Column<float>(type: "float", nullable: false),
                    UnitType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsUnits", x => x.DsUnitId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StreakInfos",
                columns: table => new
                {
                    PlayerResult = table.Column<int>(type: "int", nullable: false),
                    LongestStreak = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsWeapons",
                columns: table => new
                {
                    DsWeaponId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Range = table.Column<float>(type: "float", nullable: false),
                    AttackSpeed = table.Column<float>(type: "float", nullable: false),
                    Attacks = table.Column<int>(type: "int", nullable: false),
                    CanTarget = table.Column<int>(type: "int", nullable: false),
                    Damage = table.Column<int>(type: "int", nullable: false),
                    DamagePerUpgrade = table.Column<int>(type: "int", nullable: false),
                    DsUnitId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsWeapons", x => x.DsWeaponId);
                    table.ForeignKey(
                        name: "FK_DsWeapons_DsUnits_DsUnitId",
                        column: x => x.DsUnitId,
                        principalTable: "DsUnits",
                        principalColumn: "DsUnitId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BonusDamages",
                columns: table => new
                {
                    BonusDamageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UnitType = table.Column<int>(type: "int", nullable: false),
                    Damage = table.Column<int>(type: "int", nullable: false),
                    PerUpgrade = table.Column<int>(type: "int", nullable: false),
                    DsWeaponId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusDamages", x => x.BonusDamageId);
                    table.ForeignKey(
                        name: "FK_BonusDamages_DsWeapons_DsWeaponId",
                        column: x => x.DsWeaponId,
                        principalTable: "DsWeapons",
                        principalColumn: "DsWeaponId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BonusDamages_DsWeaponId",
                table: "BonusDamages",
                column: "DsWeaponId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusDamages_UnitType",
                table: "BonusDamages",
                column: "UnitType");

            migrationBuilder.CreateIndex(
                name: "IX_DsUnits_Commander",
                table: "DsUnits",
                column: "Commander");

            migrationBuilder.CreateIndex(
                name: "IX_DsUnits_Name",
                table: "DsUnits",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DsUnits_Name_Commander",
                table: "DsUnits",
                columns: new[] { "Name", "Commander" });

            migrationBuilder.CreateIndex(
                name: "IX_DsWeapons_DsUnitId",
                table: "DsWeapons",
                column: "DsUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonusDamages");

            migrationBuilder.DropTable(
                name: "StreakInfos");

            migrationBuilder.DropTable(
                name: "DsWeapons");

            migrationBuilder.DropTable(
                name: "DsUnits");
        }
    }
}
