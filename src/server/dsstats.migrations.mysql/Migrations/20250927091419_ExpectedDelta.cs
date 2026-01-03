using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class ExpectedDelta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RatingAfter",
                table: "ReplayPlayerRatings",
                newName: "RatingDelta");

            //migrationBuilder.AddColumn<double>(
            //    name: "ExpectedDelta",
            //    table: "ReplayPlayerRatings",
            //    type: "double",
            //    precision: 7,
            //    scale: 2,
            //    nullable: false,
            //    defaultValue: 0.0);

            migrationBuilder.Sql(@"
                ALTER TABLE ReplayPlayerRatings 
                ADD COLUMN ExpectedDelta DOUBLE(7,2) NOT NULL DEFAULT 0.0 
                AFTER RatingDelta"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedDelta",
                table: "ReplayPlayerRatings");

            migrationBuilder.RenameColumn(
                name: "RatingDelta",
                table: "ReplayPlayerRatings",
                newName: "RatingAfter");
        }
    }
}
