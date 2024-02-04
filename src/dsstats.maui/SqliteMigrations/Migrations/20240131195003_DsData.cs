using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class DsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DsAbilities",
                columns: table => new
                {
                    DsAbilityId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    Requirements = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Cooldown = table.Column<int>(type: "INTEGER", nullable: false),
                    GlobalTimer = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnergyCost = table.Column<float>(type: "REAL", nullable: false),
                    CastRange = table.Column<int>(type: "INTEGER", nullable: false),
                    AoeRadius = table.Column<float>(type: "REAL", nullable: false),
                    AbilityTarget = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 310, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsAbilities", x => x.DsAbilityId);
                });

            migrationBuilder.CreateTable(
                name: "DsUnits",
                columns: table => new
                {
                    DsUnitId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    Tier = table.Column<int>(type: "INTEGER", nullable: false),
                    Cost = table.Column<int>(type: "INTEGER", nullable: false),
                    Life = table.Column<int>(type: "INTEGER", nullable: false),
                    Shields = table.Column<int>(type: "INTEGER", nullable: false),
                    Speed = table.Column<float>(type: "REAL", nullable: false),
                    Armor = table.Column<int>(type: "INTEGER", nullable: false),
                    ShieldArmor = table.Column<int>(type: "INTEGER", nullable: false),
                    StartingEnergy = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxEnergy = table.Column<int>(type: "INTEGER", nullable: false),
                    HealthRegen = table.Column<float>(type: "REAL", nullable: false),
                    EnergyRegen = table.Column<float>(type: "REAL", nullable: false),
                    UnitType = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementType = table.Column<int>(type: "INTEGER", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsUnits", x => x.DsUnitId);
                });

            migrationBuilder.CreateTable(
                name: "StreakInfos",
                columns: table => new
                {
                    PlayerResult = table.Column<int>(type: "INTEGER", nullable: false),
                    LongestStreak = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "DsAbilityDsUnit",
                columns: table => new
                {
                    AbilitiesDsAbilityId = table.Column<int>(type: "INTEGER", nullable: false),
                    DsUnitsDsUnitId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsAbilityDsUnit", x => new { x.AbilitiesDsAbilityId, x.DsUnitsDsUnitId });
                    table.ForeignKey(
                        name: "FK_DsAbilityDsUnit_DsAbilities_AbilitiesDsAbilityId",
                        column: x => x.AbilitiesDsAbilityId,
                        principalTable: "DsAbilities",
                        principalColumn: "DsAbilityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DsAbilityDsUnit_DsUnits_DsUnitsDsUnitId",
                        column: x => x.DsUnitsDsUnitId,
                        principalTable: "DsUnits",
                        principalColumn: "DsUnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DsUpgrades",
                columns: table => new
                {
                    DsUpgradeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Upgrade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    Cost = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredTier = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    DsUnitId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsUpgrades", x => x.DsUpgradeId);
                    table.ForeignKey(
                        name: "FK_DsUpgrades_DsUnits_DsUnitId",
                        column: x => x.DsUnitId,
                        principalTable: "DsUnits",
                        principalColumn: "DsUnitId");
                });

            migrationBuilder.CreateTable(
                name: "DsWeapons",
                columns: table => new
                {
                    DsWeaponId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Range = table.Column<float>(type: "REAL", nullable: false),
                    AttackSpeed = table.Column<float>(type: "REAL", nullable: false),
                    Attacks = table.Column<int>(type: "INTEGER", nullable: false),
                    CanTarget = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage = table.Column<int>(type: "INTEGER", nullable: false),
                    DamagePerUpgrade = table.Column<int>(type: "INTEGER", nullable: false),
                    DsUnitId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsWeapons", x => x.DsWeaponId);
                    table.ForeignKey(
                        name: "FK_DsWeapons_DsUnits_DsUnitId",
                        column: x => x.DsUnitId,
                        principalTable: "DsUnits",
                        principalColumn: "DsUnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BonusDamages",
                columns: table => new
                {
                    BonusDamageId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnitType = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage = table.Column<int>(type: "INTEGER", nullable: false),
                    PerUpgrade = table.Column<int>(type: "INTEGER", nullable: false),
                    DsWeaponId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusDamages", x => x.BonusDamageId);
                    table.ForeignKey(
                        name: "FK_BonusDamages_DsWeapons_DsWeaponId",
                        column: x => x.DsWeaponId,
                        principalTable: "DsWeapons",
                        principalColumn: "DsWeaponId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonusDamages_DsWeaponId",
                table: "BonusDamages",
                column: "DsWeaponId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusDamages_UnitType",
                table: "BonusDamages",
                column: "UnitType");

            migrationBuilder.CreateIndex(
                name: "IX_DsAbilities_Name",
                table: "DsAbilities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DsAbilityDsUnit_DsUnitsDsUnitId",
                table: "DsAbilityDsUnit",
                column: "DsUnitsDsUnitId");

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
                name: "IX_DsUpgrades_DsUnitId",
                table: "DsUpgrades",
                column: "DsUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_DsUpgrades_Upgrade",
                table: "DsUpgrades",
                column: "Upgrade");

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
                name: "DsAbilityDsUnit");

            migrationBuilder.DropTable(
                name: "DsUpgrades");

            migrationBuilder.DropTable(
                name: "StreakInfos");

            migrationBuilder.DropTable(
                name: "DsWeapons");

            migrationBuilder.DropTable(
                name: "DsAbilities");

            migrationBuilder.DropTable(
                name: "DsUnits");
        }
    }
}
