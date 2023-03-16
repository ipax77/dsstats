
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<CmdrInfoResult> GetCmdrInfo(CmdrInfoRequest request)
    {
        var infos = await GetReplayCmdrInfos(request);

        foreach (var info in infos.Take(20))
        {
            Console.WriteLine(info);
        }

        return new();
    }

    private async Task<List<ReplayCmdrInfo>> GetReplayCmdrInfos(CmdrInfoRequest request)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        string sqlCommand = 
        $@"
            select r.ReplayId, r.CommandersTeam1, r.CommandersTeam2, r.WinnerTeam,
            round(avg(rpr1.Rating)) as rating1,
            round(avg(rpr2.Rating)) as rating2,
            group_concat(distinct rprc.Rating) as ratings,
            round(avg(distinct rprc.RatingChange)) as avgGain
            from Replays as r
            inner join ReplayRatings as rr on rr.ReplayId = r.ReplayId
            inner join RepPlayerRatings as rpr1 on rpr1.ReplayRatingInfoId = rr.ReplayRatingId and rpr1.GamePos <= 3
            inner join RepPlayerRatings as rpr2 on rpr2.ReplayRatingInfoId = rr.ReplayRatingId and rpr2.GamePos > 3
            inner join ReplayPlayers as rp on rp.ReplayId = r.ReplayId and rp.Race = {(int)request.Interest}
            inner join RepPlayerRatings as rprc on rprc.ReplayPlayerId = rp.ReplayPlayerId
            WHERE r.GameTime >= '{fromDate:yyyy-MM-dd}' and r.GameTime < '{toDate:yyyy-MM-dd}'
                and rr.RatingType = {(int)request.RatingType}
            group by r.ReplayId, r.CommandersTeam1, r.CommandersTeam2, r.WinnerTeam
            having abs(avg(rpr1.Rating) - avg(rpr2.Rating)) < {request.MaxGap};
        ";

        using MySqlConnection conn = new(Data.MysqlConnectionString);
        await conn.OpenAsync();

        using MySqlCommand cmd = new(sqlCommand, conn);
        cmd.CommandTimeout = 120;
        using MySqlDataReader reader = await cmd.ExecuteReaderAsync();

        List<ReplayCmdrInfo> result = new();

        while (await reader.ReadAsync())
        {
            ReplayCmdrInfo replayCmdrInfo = new()
            {
                ReplayId = reader.GetInt32("ReplayId"),
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

public record ReplayCmdrInfo
{
    public int ReplayId { get; set; }
    public float Rating1 { get; set; }
    public float Rating2 { get; set; }
    public float AvgGain { get; set; }
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public int WinnerTeam { get; set; }
    public string Ratings { get; set; } = string.Empty;
}


public record CmdrRating
{
    public int GamePos { get; set; }
    public float Rating { get; set; }
}

public record CmdrInfoRequest
{
    public RatingType RatingType { get; set; } = RatingType.Cmdr;
    public TimePeriod TimePeriod { get; set; } = TimePeriod.Patch2_71;
    public Commander Interest { get; set; } = Commander.Swann;
    public int MaxGap { get; set; } = 200;
    public int MinRating { get; set; }
    public int MaxRating { get; set; }
}

public record CmdrInfoResult
{

}

