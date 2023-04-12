
using MySqlConnector;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<int> GetCmdrReplayInfosCount(CmdrInfoRequest request, CancellationToken token = default)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        string sqlCommand = 
        $@"
            select count(*)
            from (
                select count(r.ReplayId)
                from Replays as r
                inner join ReplayRatings as rr on rr.ReplayId = r.ReplayId
                inner join RepPlayerRatings as rpr1 on rpr1.ReplayRatingInfoId = rr.ReplayRatingId and rpr1.GamePos <= 3
                inner join RepPlayerRatings as rpr2 on rpr2.ReplayRatingInfoId = rr.ReplayRatingId and rpr2.GamePos > 3
                inner join ReplayPlayers as rp on rp.ReplayId = r.ReplayId and rp.Race = {(int)request.Interest}
                inner join RepPlayerRatings as rprc on rprc.ReplayPlayerId = rp.ReplayPlayerId
                WHERE r.GameTime >= '{fromDate:yyyy-MM-dd}' " + 
                    (toDate == DateTime.Today ? "" : $@"and r.GameTime < '{toDate:yyyy-MM-dd}' ") +
                    (request.WithoutLeavers ? "and rr.LeaverType = 0 " : "") +
                    $@"and rr.RatingType = {(int)request.RatingType}
                group by r.ReplayId " +
                    (request.MaxGap > 0 ? $@"having abs(avg(rpr1.Rating) - avg(rpr2.Rating)) < {request.MaxGap}" : "") +
        "   ) t;";

        using MySqlConnection conn = new(dbImportOptions.Value.ImportConnectionString);
        await conn.OpenAsync(token);

        using MySqlCommand cmd = new(sqlCommand, conn);
        cmd.CommandTimeout = 120;

        return Convert.ToInt32(await cmd.ExecuteScalarAsync(token));
    }    

    public async Task<List<ReplayCmdrInfo>> GetCmdrReplayInfos(CmdrInfoRequest request, CancellationToken token = default)
    {
        var infos = await ProduceCmdrReplayInfos(request, token);
        return infos;
    }

    private async Task<List<ReplayCmdrInfo>> ProduceCmdrReplayInfos(CmdrInfoRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        string sqlCommand = 
        $@"
            select r.ReplayId, r.CommandersTeam1, r.CommandersTeam2, r.WinnerTeam,
            r.ReplayHash, r.Maxleaver, r.GameTime, r.Duration,
            round(avg(rpr1.Rating)) as rating1,
            round(avg(rpr2.Rating)) as rating2,
            group_concat(distinct rprc.Rating) as ratings,
            round(avg(distinct rprc.RatingChange), 2) as avgGain
            from Replays as r
            inner join ReplayRatings as rr on rr.ReplayId = r.ReplayId
            inner join RepPlayerRatings as rpr1 on rpr1.ReplayRatingInfoId = rr.ReplayRatingId and rpr1.GamePos <= 3
            inner join RepPlayerRatings as rpr2 on rpr2.ReplayRatingInfoId = rr.ReplayRatingId and rpr2.GamePos > 3
            inner join ReplayPlayers as rp on rp.ReplayId = r.ReplayId and rp.Race = {(int)request.Interest}
            inner join RepPlayerRatings as rprc on rprc.ReplayPlayerId = rp.ReplayPlayerId
            WHERE r.GameTime >= '{fromDate:yyyy-MM-dd}' " + 
                (toDate == DateTime.Today ? "" : $@"and r.GameTime < '{toDate:yyyy-MM-dd}' ") +
                (request.WithoutLeavers ? "and rr.LeaverType = 0 " : "") +
                $@"and rr.RatingType = {(int)request.RatingType}
            group by r.ReplayId, r.CommandersTeam1, r.CommandersTeam2, r.WinnerTeam,
                r.ReplayHash, r.Maxleaver, r.GameTime " +
                (request.MaxGap > 0 ? $@"having abs(avg(rpr1.Rating) - avg(rpr2.Rating)) < {request.MaxGap} " : "") +
            $@"order by r.GameTime desc
            limit {request.Take}
            offset {request.Skip};
        ";

        using MySqlConnection conn = new(dbImportOptions.Value.ImportConnectionString);
        await conn.OpenAsync(token);

        using MySqlCommand cmd = new(sqlCommand, conn);
        cmd.CommandTimeout = 120;
        using MySqlDataReader reader = await cmd.ExecuteReaderAsync(token);

        List<ReplayCmdrInfo> result = new();

        while (await reader.ReadAsync(token))
        {
            ReplayCmdrInfo replayCmdrInfo = new()
            {
                ReplayId = reader.GetInt32("ReplayId"),
                ReplayHash = reader.GetString("ReplayHash"),
                GameTime = reader.GetDateTime("GameTime"),
                Duration = reader.GetInt32("Duration"),
                Maxleaver = reader.GetInt32("Maxleaver"),
                Team1 = reader.GetString("CommandersTeam1"),
                Team2 = reader.GetString("CommandersTeam2"),
                WinnerTeam = reader.GetInt32("WinnerTeam"),
                Rating1 = reader.IsDBNull(reader.GetOrdinal("rating1")) ? 0 : (float)reader.GetFloat("rating1"),
                Rating2 = reader.IsDBNull(reader.GetOrdinal("rating2")) ? 0 : (float)reader.GetFloat("rating2"),
                Ratings = reader.IsDBNull(reader.GetOrdinal("ratings")) ? "" : reader.GetString("ratings"),
                AvgGain = reader.IsDBNull(reader.GetOrdinal("avgGain")) ? 0 : (float)reader.GetFloat("avgGain")
            };

            result.Add(replayCmdrInfo);
        }
        return result;
    }
}
