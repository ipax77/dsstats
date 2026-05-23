using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    public partial class SpawnPlaybackSidecar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReplaySpawnPlaybacks",
                columns: table => new
                {
                    ReplayId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormatVersion = table.Column<ushort>(type: "INTEGER", nullable: false),
                    Compression = table.Column<byte>(type: "INTEGER", nullable: false),
                    CompressedLength = table.Column<int>(type: "INTEGER", nullable: false),
                    UncompressedLength = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Payload = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplaySpawnPlaybacks", x => x.ReplayId);
                    table.ForeignKey(
                        name: "FK_ReplaySpawnPlaybacks_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplaySpawnPlaybacks");
        }
    }
}
