using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class InHouseAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InHouseUsers",
                columns: table => new
                {
                    InHouseUserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PublicId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DisplayName = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseUsers", x => x.InHouseUserId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseDeviceLinkCodes",
                columns: table => new
                {
                    InHouseDeviceLinkCodeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseUserId = table.Column<int>(type: "int", nullable: false),
                    CodeHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayCode = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseDeviceLinkCodes", x => x.InHouseDeviceLinkCodeId);
                    table.ForeignKey(
                        name: "FK_InHouseDeviceLinkCodes_InHouseUsers_InHouseUserId",
                        column: x => x.InHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHousePasskeyCredentials",
                columns: table => new
                {
                    InHousePasskeyCredentialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseUserId = table.Column<int>(type: "int", nullable: false),
                    CredentialId = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserHandle = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PublicKey = table.Column<byte[]>(type: "longblob", nullable: false),
                    SignatureCounter = table.Column<uint>(type: "int unsigned", nullable: false),
                    IsBackedUp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeviceName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHousePasskeyCredentials", x => x.InHousePasskeyCredentialId);
                    table.ForeignKey(
                        name: "FK_InHousePasskeyCredentials_InHouseUsers_InHouseUserId",
                        column: x => x.InHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseProfiles",
                columns: table => new
                {
                    InHouseProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToonId_Region = table.Column<int>(type: "int", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "int", nullable: false),
                    ToonId_Id = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseProfiles", x => x.InHouseProfileId);
                    table.ForeignKey(
                        name: "FK_InHouseProfiles_InHouseUsers_InHouseUserId",
                        column: x => x.InHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseSessions",
                columns: table => new
                {
                    InHouseSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseUserId = table.Column<int>(type: "int", nullable: false),
                    AccessTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefreshTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RefreshExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseSessions", x => x.InHouseSessionId);
                    table.ForeignKey(
                        name: "FK_InHouseSessions_InHouseUsers_InHouseUserId",
                        column: x => x.InHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseDeviceLinkCodes_CodeHash",
                table: "InHouseDeviceLinkCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHouseDeviceLinkCodes_ExpiresAt",
                table: "InHouseDeviceLinkCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseDeviceLinkCodes_InHouseUserId",
                table: "InHouseDeviceLinkCodes",
                column: "InHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHousePasskeyCredentials_CredentialId",
                table: "InHousePasskeyCredentials",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHousePasskeyCredentials_InHouseUserId",
                table: "InHousePasskeyCredentials",
                column: "InHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHousePasskeyCredentials_UserHandle",
                table: "InHousePasskeyCredentials",
                column: "UserHandle");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseProfiles_InHouseUserId",
                table: "InHouseProfiles",
                column: "InHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseProfiles_ToonId_Region_ToonId_Realm_ToonId_Id",
                table: "InHouseProfiles",
                columns: new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHouseSessions_AccessTokenHash",
                table: "InHouseSessions",
                column: "AccessTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHouseSessions_ExpiresAt",
                table: "InHouseSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseSessions_InHouseUserId",
                table: "InHouseSessions",
                column: "InHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseSessions_RefreshTokenHash",
                table: "InHouseSessions",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHouseUsers_DisplayName",
                table: "InHouseUsers",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseUsers_PublicId",
                table: "InHouseUsers",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InHouseDeviceLinkCodes");

            migrationBuilder.DropTable(
                name: "InHousePasskeyCredentials");

            migrationBuilder.DropTable(
                name: "InHouseProfiles");

            migrationBuilder.DropTable(
                name: "InHouseSessions");

            migrationBuilder.DropTable(
                name: "InHouseUsers");
        }
    }
}
