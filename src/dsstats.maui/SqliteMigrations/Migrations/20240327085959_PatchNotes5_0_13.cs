using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class PatchNotes5_0_13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Faqs",
                columns: table => new
                {
                    FaqId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Question = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Answer = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Upvotes = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faqs", x => x.FaqId);
                });

            migrationBuilder.CreateTable(
                name: "FaqVotes",
                columns: table => new
                {
                    FaqVoteId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FaqId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqVotes", x => x.FaqVoteId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Faqs_Question",
                table: "Faqs",
                column: "Question");

            var sql = @"UPDATE DsUnits SET Life = 130 WHERE Name = 'Cyclone' AND Commander = 2;
UPDATE DsUnits SET Shields = 30 WHERE Name = 'Observer' AND Commander = 1;
UPDATE DsUnits SET UnitType = 768 WHERE Name = 'Sentry' AND Commander = 1;
";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Faqs");

            migrationBuilder.DropTable(
                name: "FaqVotes");
        }
    }
}
