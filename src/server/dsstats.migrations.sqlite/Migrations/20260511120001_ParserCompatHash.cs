using dsstats.db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DsstatsContext))]
    [Migration("20260511120001_ParserCompatHash")]
    public partial class ParserCompatHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParserCompatHash",
                table: "Replays",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Replays_ParserCompatHash",
                table: "Replays",
                column: "ParserCompatHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Replays_ParserCompatHash",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "ParserCompatHash",
                table: "Replays");
        }
    }
}
