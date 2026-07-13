using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    public partial class PatchNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatchNotes",
                columns: table => new
                {
                    PatchNoteId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceKey = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    Source = table.Column<byte>(type: "INTEGER", nullable: false),
                    SourceMessageId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    SourceSequence = table.Column<int>(type: "INTEGER", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: false),
                    Commander = table.Column<short>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchNotes", x => x.PatchNoteId);
                });

            migrationBuilder.CreateTable(
                name: "PatchNoteSyncStates",
                columns: table => new
                {
                    PatchNoteSyncStateId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Cursor = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchNoteSyncStates", x => x.PatchNoteSyncStateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatchNotes_Commander_PublishedAtUtc_PatchNoteId",
                table: "PatchNotes",
                columns: new[] { "Commander", "PublishedAtUtc", "PatchNoteId" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_PatchNotes_PublishedAtUtc_PatchNoteId",
                table: "PatchNotes",
                columns: new[] { "PublishedAtUtc", "PatchNoteId" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_PatchNotes_SourceKey",
                table: "PatchNotes",
                column: "SourceKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatchNotes");

            migrationBuilder.DropTable(
                name: "PatchNoteSyncStates");
        }
    }
}
