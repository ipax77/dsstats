using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class FindDuplicateReplaysInRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReplayIdResult",
                columns: table => new
                {
                    SeqKey = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReplayIdsCsv = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            var sql = @"CREATE PROCEDURE FindDuplicateReplayClustersInRange(
    IN FromGametime DATETIME,
    IN ToGametime   DATETIME
)
BEGIN
  DROP TEMPORARY TABLE IF EXISTS tmp_replay_sequences;
  DROP TEMPORARY TABLE IF EXISTS tmp_duplicate_seq_keys;

  CREATE TEMPORARY TABLE tmp_replay_sequences AS
  SELECT
    r.ReplayId,
    GROUP_CONCAT(CONCAT(p.ToonId_Id, ':', p.ToonId_Region) ORDER BY rp.GamePos SEPARATOR ',') AS seq,
    GROUP_CONCAT(p.ToonId_Realm ORDER BY rp.GamePos SEPARATOR ',') AS seq2
  FROM Replays r
  JOIN ReplayPlayers rp ON rp.ReplayId = r.ReplayId
  JOIN Players p ON p.PlayerID = rp.PlayerId
  WHERE r.Gametime >= FromGametime
    AND r.Gametime <= ToGametime
    AND r.PlayerCount = 6
  GROUP BY r.ReplayId;

  CREATE TEMPORARY TABLE tmp_duplicate_seq_keys AS
  SELECT seq
  FROM tmp_replay_sequences
  GROUP BY seq
  HAVING COUNT(*) > 1
     AND COUNT(DISTINCT seq2) > 1;

  -- return clusters
  SELECT
    r.seq AS SeqKey,
    GROUP_CONCAT(r.ReplayId ORDER BY r.ReplayId SEPARATOR ',') AS ReplayIdsCsv
  FROM tmp_replay_sequences r
  JOIN tmp_duplicate_seq_keys k USING (seq)
  GROUP BY r.seq
  ORDER BY r.seq;

  DROP TEMPORARY TABLE IF EXISTS tmp_duplicate_seq_keys;
  DROP TEMPORARY TABLE IF EXISTS tmp_replay_sequences;
END
";

            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplayIdResult");
        }
    }
}
