
using System.Globalization;
using System.Text;
using MySqlConnector;
using pax.dsstats.shared.Calc;

namespace dsstats.ratings.db;

public partial class CalcRepository
{
    private readonly string csvBasePath = "/data/mysqlfiles";

    public async Task CreateDsstatsPlayerRatingCsv(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings)
    {
        var playerIdDic = await GetPlayerIdDic();

        StringBuilder sb = new();
        int i = 0;
        foreach (var ent in mmrIdRatings)
        {
            foreach (var entCalc in ent.Value.Values)
            {
                if (!playerIdDic.TryGetValue(entCalc.PlayerId, out var playerId))
                {
                    continue;
                }

                i++;
                sb.Append($"{i},");
                sb.Append($"{ent.Key},");
                sb.Append($"{entCalc.Games},");
                sb.Append($"{entCalc.Wins},");
                sb.Append($"{entCalc.Mmr.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{entCalc.Consistency.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{entCalc.Confidence.ToString(CultureInfo.InvariantCulture)},");
                sb.Append("0,");
                sb.Append($"{playerId}");
                sb.Append(Environment.NewLine);
            }
        }
        File.WriteAllText($"{csvBasePath}/ComboPlayerRatings.csv", sb.ToString());
    }

    public (int, int) DsstatsCreateOrAppendReplayAndReplayPlayerRatingsCsv(List<ReplayRatingDto> replayRatingDtos,
                                                                       int replayAppendId,
                                                                       int replayPlayerAppendId)
    {
        StringBuilder sbReplay = new();
        StringBuilder sbPlayer = new();

        bool append = replayAppendId > 0;

        foreach (var replayRatingDto in replayRatingDtos)
        {
            if (!replayRatingDto.RepPlayerRatings.Any())
            {
                continue;
            }

            replayAppendId++;

            sbReplay.Append($"{replayAppendId},");
            sbReplay.Append($"{replayRatingDto.RatingType},");
            sbReplay.Append($"{replayRatingDto.LeaverType},");
            sbReplay.Append($"{replayRatingDto.ExpectationToWin.ToString(CultureInfo.InvariantCulture)},");
            sbReplay.Append($"{replayRatingDto.ReplayId},");
            sbReplay.Append("0");
            sbReplay.Append(Environment.NewLine);

            foreach (var rpr in replayRatingDto.RepPlayerRatings)
            {
                replayPlayerAppendId++;
                sbPlayer.Append($"{replayPlayerAppendId},");
                sbPlayer.Append($"{rpr.GamePos},");
                sbPlayer.Append($"{rpr.Rating.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.RatingChange.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.Games},");
                sbPlayer.Append($"{rpr.Consistency.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.Confidence.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.ReplayPlayerId}");
                sbPlayer.Append(Environment.NewLine);
            }
        }

        if (!append)
        {
            File.WriteAllText($"{csvBasePath}/ComboReplayRatings.csv", sbReplay.ToString());
            File.WriteAllText($"{csvBasePath}/ComboReplayPlayerRatings.csv", sbPlayer.ToString());
        }
        else
        {
            File.AppendAllText($"{csvBasePath}/ComboReplayRatings.csv", sbReplay.ToString());
            File.AppendAllText($"{csvBasePath}/ComboReplayPlayerRatings.csv", sbPlayer.ToString());
        }
        return (replayAppendId, replayPlayerAppendId);
    }

    private async Task<Dictionary<PlayerId, int>> GetPlayerIdDic()
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var commandTxt =
$@"SELECT PlayerId, ToonId, RegionId, RealmId FROM Players;";

        using var command = new MySqlCommand(commandTxt, connection);
        using var reader = await command.ExecuteReaderAsync();

        Dictionary<PlayerId, int> playerIdDic = new();

        while (await reader.ReadAsync())
        {
            int id = reader.GetInt32(0);
            int profileId = reader.GetInt32(1);
            int regionId = reader.GetInt32(2);
            int realmId = reader.GetInt32(3);

            PlayerId playerId = new() { ProfileId = profileId, RegionId = regionId, RealmId = realmId };
            playerIdDic[playerId] = id;
        }

        return playerIdDic;
    }
}