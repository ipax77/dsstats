using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class ReplayUploadJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReplayUploadJobs",
                columns: table => new
                {
                    ReplayUploadJobId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BlobFilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    Error = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayUploadJobs", x => x.ReplayUploadJobId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UploadJobs_CreatedAt",
                table: "UploadJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadJobs_FinishedAt",
                table: "UploadJobs",
                column: "FinishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayUploadJobs_CreatedAt",
                table: "ReplayUploadJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayUploadJobs_FinishedAt",
                table: "ReplayUploadJobs",
                column: "FinishedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplayUploadJobs");

            migrationBuilder.DropIndex(
                name: "IX_UploadJobs_CreatedAt",
                table: "UploadJobs");

            migrationBuilder.DropIndex(
                name: "IX_UploadJobs_FinishedAt",
                table: "UploadJobs");
        }
    }
}
