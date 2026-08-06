using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    public partial class DecoderSourceVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UploadJobs_CreatedAt",
                table: "UploadJobs");

            migrationBuilder.DropIndex(
                name: "IX_ReplayUploadJobs_CreatedAt",
                table: "ReplayUploadJobs");

            migrationBuilder.AddColumn<byte>(
                name: "DecoderSource",
                table: "UploadJobs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecoderVersion",
                table: "UploadJobs",
                type: "TEXT",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DecoderSource",
                table: "ReplayUploadJobs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecoderVersion",
                table: "ReplayUploadJobs",
                type: "TEXT",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "ReplayUploadJobs",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE UploadJobs
                SET DecoderSource = CASE
                        WHEN LOWER(TRIM(COALESCE(Version, ''))) LIKE 'myds%' THEN 2
                        WHEN LOWER(TRIM(COALESCE(Version, ''))) LIKE 'ser%' THEN 3
                        WHEN LOWER(TRIM(COALESCE(Version, ''))) LIKE 'api%' THEN 4
                        ELSE 1
                    END,
                    DecoderVersion = SUBSTR(CASE
                        WHEN LOWER(TRIM(COALESCE(Version, ''))) LIKE 'myds%'
                            THEN COALESCE(NULLIF(TRIM(SUBSTR(Version, 5)), ''), 'unknown')
                        WHEN LOWER(TRIM(COALESCE(Version, ''))) LIKE 'ser%'
                            THEN COALESCE(NULLIF(TRIM(SUBSTR(Version, 4)), ''), 'unknown')
                        WHEN LOWER(TRIM(COALESCE(Version, ''))) LIKE 'api%'
                            THEN COALESCE(NULLIF(TRIM(SUBSTR(Version, 4)), ''), 'unknown')
                        WHEN LOWER(TRIM(COALESCE(Version, ''))) LIKE 'ma%'
                            THEN COALESCE(NULLIF(TRIM(SUBSTR(Version, 3)), ''), 'unknown')
                        ELSE COALESCE(NULLIF(TRIM(Version), ''), 'unknown')
                    END, 1, 24)
                WHERE CreatedAt >= datetime('now', '-90 days')
                  AND DecoderSource IS NULL;

                UPDATE ReplayUploadJobs
                SET Version = COALESCE(Version, 'apiunknown'),
                    DecoderSource = 4,
                    DecoderVersion = 'unknown'
                WHERE CreatedAt >= datetime('now', '-90 days')
                  AND DecoderSource IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UploadJobs_CreatedAt_DecoderSource_DecoderVersion",
                table: "UploadJobs",
                columns: new[] { "CreatedAt", "DecoderSource", "DecoderVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayUploadJobs_CreatedAt_DecoderSource_DecoderVersion",
                table: "ReplayUploadJobs",
                columns: new[] { "CreatedAt", "DecoderSource", "DecoderVersion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UploadJobs_CreatedAt_DecoderSource_DecoderVersion",
                table: "UploadJobs");

            migrationBuilder.DropIndex(
                name: "IX_ReplayUploadJobs_CreatedAt_DecoderSource_DecoderVersion",
                table: "ReplayUploadJobs");

            migrationBuilder.DropColumn(
                name: "DecoderSource",
                table: "UploadJobs");

            migrationBuilder.DropColumn(
                name: "DecoderVersion",
                table: "UploadJobs");

            migrationBuilder.DropColumn(
                name: "DecoderSource",
                table: "ReplayUploadJobs");

            migrationBuilder.DropColumn(
                name: "DecoderVersion",
                table: "ReplayUploadJobs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ReplayUploadJobs");

            migrationBuilder.CreateIndex(
                name: "IX_UploadJobs_CreatedAt",
                table: "UploadJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayUploadJobs_CreatedAt",
                table: "ReplayUploadJobs",
                column: "CreatedAt");
        }
    }
}
