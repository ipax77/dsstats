using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InHouse : Migration
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
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseUsers", x => x.InHouseUserId);
                });

            migrationBuilder.CreateTable(
                name: "ReplayObservers",
                columns: table => new
                {
                    ReplayObserversId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerIds = table.Column<string>(type: "TEXT", nullable: true),
                    ReplayId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayObservers", x => x.ReplayObserversId);
                    table.ForeignKey(
                        name: "FK_ReplayObservers_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
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
                name: "InHouseGameSessions",
                columns: table => new
                {
                    InHouseGameSessionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedByInHouseUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    ReplayIds = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessions", x => x.InHouseGameSessionId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessions_InHouseUsers_CreatedByInHouseUserId",
                        column: x => x.CreatedByInHouseUserId,
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

            migrationBuilder.CreateTable(
                name: "InHouseGameSessionStateSnapshots",
                columns: table => new
                {
                    InHouseGameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessionStateSnapshots", x => x.InHouseGameSessionId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionStateSnapshots_InHouseGameSessions_InHouseGameSessionId",
                        column: x => x.InHouseGameSessionId,
                        principalTable: "InHouseGameSessions",
                        principalColumn: "InHouseGameSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_InHouseGameSessions_ClosedAt",
                table: "InHouseGameSessions",
                column: "ClosedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_CreatedAt",
                table: "InHouseGameSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_CreatedByInHouseUserId",
                table: "InHouseGameSessions",
                column: "CreatedByInHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_PublicId",
                table: "InHouseGameSessions",
                column: "PublicId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_ReplayObservers_ReplayId",
                table: "ReplayObservers",
                column: "ReplayId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InHouseDeviceLinkCodes");

            migrationBuilder.DropTable(
                name: "InHouseGameSessionStateSnapshots");

            migrationBuilder.DropTable(
                name: "InHousePasskeyCredentials");

            migrationBuilder.DropTable(
                name: "InHouseProfiles");

            migrationBuilder.DropTable(
                name: "InHouseSessions");

            migrationBuilder.DropTable(
                name: "ReplayObservers");

            migrationBuilder.DropTable(
                name: "InHouseGameSessions");

            migrationBuilder.DropTable(
                name: "InHouseUsers");
        }
    }
}
