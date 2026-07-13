using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
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
                    PatchNoteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SourceKey = table.Column<string>(type: "varchar(96)", maxLength: 96, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    SourceMessageId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceSequence = table.Column<int>(type: "int", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    Commander = table.Column<short>(type: "smallint", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchNotes", x => x.PatchNoteId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PatchNoteSyncStates",
                columns: table => new
                {
                    PatchNoteSyncStateId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cursor = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchNoteSyncStates", x => x.PatchNoteSyncStateId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.Sql(
                "CREATE FULLTEXT INDEX `IX_PatchNotes_Content_FullText` ON `PatchNotes` (`Content`);");
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
