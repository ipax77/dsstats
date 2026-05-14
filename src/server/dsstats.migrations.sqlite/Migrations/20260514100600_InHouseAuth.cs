using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
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
                    InHouseUserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseUsers", x => x.InHouseUserId);
                });

            migrationBuilder.CreateTable(
                name: "InHouseDeviceLinkCodes",
                columns: table => new
                {
                    InHouseDeviceLinkCodeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InHouseUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CodeHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayCode = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "InHousePasskeyCredentials",
                columns: table => new
                {
                    InHousePasskeyCredentialId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InHouseUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CredentialId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    UserHandle = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PublicKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SignatureCounter = table.Column<uint>(type: "INTEGER", nullable: false),
                    IsBackedUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "InHouseProfiles",
                columns: table => new
                {
                    InHouseProfileId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InHouseUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ToonId_Region = table.Column<int>(type: "INTEGER", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "INTEGER", nullable: false),
                    ToonId_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "InHouseSessions",
                columns: table => new
                {
                    InHouseSessionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InHouseUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    AccessTokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RefreshExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                });

            migrationBuilder.CreateIndex("IX_InHouseDeviceLinkCodes_CodeHash", "InHouseDeviceLinkCodes", "CodeHash", unique: true);
            migrationBuilder.CreateIndex("IX_InHouseDeviceLinkCodes_ExpiresAt", "InHouseDeviceLinkCodes", "ExpiresAt");
            migrationBuilder.CreateIndex("IX_InHouseDeviceLinkCodes_InHouseUserId", "InHouseDeviceLinkCodes", "InHouseUserId");
            migrationBuilder.CreateIndex("IX_InHousePasskeyCredentials_CredentialId", "InHousePasskeyCredentials", "CredentialId", unique: true);
            migrationBuilder.CreateIndex("IX_InHousePasskeyCredentials_InHouseUserId", "InHousePasskeyCredentials", "InHouseUserId");
            migrationBuilder.CreateIndex("IX_InHousePasskeyCredentials_UserHandle", "InHousePasskeyCredentials", "UserHandle");
            migrationBuilder.CreateIndex("IX_InHouseProfiles_InHouseUserId", "InHouseProfiles", "InHouseUserId");
            migrationBuilder.CreateIndex("IX_InHouseProfiles_ToonId_Region_ToonId_Realm_ToonId_Id", "InHouseProfiles", new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" }, unique: true);
            migrationBuilder.CreateIndex("IX_InHouseSessions_AccessTokenHash", "InHouseSessions", "AccessTokenHash", unique: true);
            migrationBuilder.CreateIndex("IX_InHouseSessions_ExpiresAt", "InHouseSessions", "ExpiresAt");
            migrationBuilder.CreateIndex("IX_InHouseSessions_InHouseUserId", "InHouseSessions", "InHouseUserId");
            migrationBuilder.CreateIndex("IX_InHouseSessions_RefreshTokenHash", "InHouseSessions", "RefreshTokenHash", unique: true);
            migrationBuilder.CreateIndex("IX_InHouseUsers_DisplayName", "InHouseUsers", "DisplayName");
            migrationBuilder.CreateIndex("IX_InHouseUsers_PublicId", "InHouseUsers", "PublicId", unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("InHouseDeviceLinkCodes");
            migrationBuilder.DropTable("InHousePasskeyCredentials");
            migrationBuilder.DropTable("InHouseProfiles");
            migrationBuilder.DropTable("InHouseSessions");
            migrationBuilder.DropTable("InHouseUsers");
        }
    }
}
