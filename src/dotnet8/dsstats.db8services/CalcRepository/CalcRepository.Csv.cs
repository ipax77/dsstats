
using dsstats.shared;
using dsstats.shared.Calc;
using MySqlConnector;
using System.Data.SQLite;
using System.Globalization;
using System.Text;

namespace dsstats.db8services;

public partial class CalcRepository
{
    private readonly string csvBasePath = "/data/mysqlfiles";

    public async Task CreatePlayerRatingCsv(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, RatingCalcType ratingCalcType)
    {
        var playerIdDic = await GetPlayerIdDic(ratingCalcType);

        (var filename, var sb) = ratingCalcType switch
        {
            RatingCalcType.Dsstats => ("PlayerRatings.csv", GetDsstatsPlayerStringBuilder(mmrIdRatings, playerIdDic)),
            RatingCalcType.Arcade => ("ArcadePlayerRatings.csv", GetArcadePlayerStringBuilder(mmrIdRatings, playerIdDic)),
            RatingCalcType.Combo => ("ComboPlayerRatings.csv", GetComboPlayerStringBuilder(mmrIdRatings, playerIdDic)),
            _ => throw new NotImplementedException()
        };

        File.WriteAllText($"{csvBasePath}/{filename}", sb.ToString());
    }

    private static StringBuilder GetArcadePlayerStringBuilder(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                                        Dictionary<PlayerId, int> playerIdDic)
    {
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

                string csvLine = $"{i},{ent.Key},{entCalc.Mmr.ToString(CultureInfo.InvariantCulture)},0,{entCalc.Games},{entCalc.Wins},0,0,0,0,{entCalc.Consistency.ToString(CultureInfo.InvariantCulture)},{entCalc.Confidence.ToString(CultureInfo.InvariantCulture)},0,{playerId}";
                sb.AppendLine(csvLine);
            }
        }
        return sb;
    }

    private static StringBuilder GetDsstatsPlayerStringBuilder(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                                    Dictionary<PlayerId, int> playerIdDic)
    {
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
                var main = entCalc.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();
                var maincount = main.Key == Commander.None ? 0 : main.Value;

                sb.Append($"{i},");    //  `PlayerRatingId` int(11) NOT NULL AUTO_INCREMENT,
                sb.Append($"{ent.Key},");    //  `RatingType` int(11) NOT NULL,
                sb.Append($"{entCalc.Mmr.ToString(CultureInfo.InvariantCulture)},");    //  `Rating` double NOT NULL,
                sb.Append($"{entCalc.Games},");    //  `Games` int(11) NOT NULL,
                sb.Append($"{entCalc.Wins},");    //  `Wins` int(11) NOT NULL,
                sb.Append($"{entCalc.Mvps},");    //  `Mvp` int(11) NOT NULL,
                sb.Append("0,");    //  `TeamGames` int(11) NOT NULL,
                sb.Append($"{maincount},");    //  `MainCount` int(11) NOT NULL,
                sb.Append($"{(int)main.Key},");    //  `Main` int(11) NOT NULL,
                sb.Append($"{entCalc.Consistency.ToString(CultureInfo.InvariantCulture)},");    //  `Consistency` double NOT NULL,
                sb.Append($"{entCalc.Confidence.ToString(CultureInfo.InvariantCulture)},");    //  `Confidence` double NOT NULL,
                sb.Append($"{(entCalc.IsUploader ? 1 : 0)},");    //  `IsUploader` tinyint(1) NOT NULL,
                sb.Append($"{playerId},");    //  `PlayerId` int(11) NOT NULL,
                sb.Append("0,");    //  `Pos` int(11) NOT NULL DEFAULT '0',
                sb.Append("0");    //  `ArcadeDefeatsSinceLastUpload` int(11) NOT NULL DEFAULT '0',
                sb.Append(Environment.NewLine);
            }
        }
        return sb;
    }

    private static StringBuilder GetComboPlayerStringBuilder(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                                Dictionary<PlayerId, int> playerIdDic)
    {
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
                var main = entCalc.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();
                var manicount = main.Key == Commander.None ? 0 : main.Value;
                sb.Append($"{i},");
                sb.Append($"{ent.Key},");
                sb.Append($"{entCalc.Games},");
                sb.Append($"{entCalc.Wins},");
                sb.Append($"{entCalc.Mmr.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{entCalc.Consistency.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{entCalc.Confidence.ToString(CultureInfo.InvariantCulture)},");
                sb.Append("0,"); // pos
                sb.Append($"{playerId}");
                sb.Append(Environment.NewLine);
            }
        }
        return sb;
    }

    public (int, int) CreateOrAppendReplayAndReplayPlayerRatingsCsv(List<shared.Calc.ReplayRatingDto> replayRatingDtos,
                                                                       int replayAppendId,
                                                                       int replayPlayerAppendId,
                                                                       RatingCalcType ratingCalcType)
    {
        StringBuilder sbReplay = new();
        StringBuilder sbPlayer = new();

        bool append = replayAppendId > 0;

        foreach (var replayRatingDto in replayRatingDtos)
        {
            if (replayRatingDto.RepPlayerRatings.Count == 0)
            {
                continue;
            }

            replayAppendId++;

            sbReplay.Append($"{replayAppendId},");
            sbReplay.Append($"{replayRatingDto.RatingType},");
            sbReplay.Append($"{replayRatingDto.LeaverType},");
            sbReplay.Append($"{replayRatingDto.ExpectationToWin.ToString(CultureInfo.InvariantCulture)},");
            if (ratingCalcType == RatingCalcType.Arcade)
            {
                sbReplay.Append($"{replayRatingDto.ReplayId}");
            }
            else
            {
                sbReplay.Append($"{replayRatingDto.ReplayId},");
                sbReplay.Append("0");
            }
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
                if (ratingCalcType == RatingCalcType.Combo)
                {
                    sbPlayer.Append($"{rpr.ReplayPlayerId}");
                }
                else
                {
                    sbPlayer.Append($"{rpr.ReplayPlayerId},");
                    sbPlayer.Append($"{replayAppendId}");
                }
                sbPlayer.Append(Environment.NewLine);
            }
        }

        (var replayFileName, var replayPlayerFileName) = ratingCalcType switch
        {
            RatingCalcType.Dsstats => ("ReplayRatings.csv", "RepPlayerRatings.csv"),
            RatingCalcType.Arcade => ("ArcadeReplayRatings.csv", "ArcadeReplayPlayerRatings.csv"),
            RatingCalcType.Combo => ("ComboReplayRatings.csv", "ComboReplayPlayerRatings.csv"),
            _ => throw new NotImplementedException()

        };

        if (!append)
        {
            File.WriteAllText($"{csvBasePath}/{replayFileName}", sbReplay.ToString());
            File.WriteAllText($"{csvBasePath}/{replayPlayerFileName}", sbPlayer.ToString());
        }
        else
        {
            File.AppendAllText($"{csvBasePath}/{replayFileName}", sbReplay.ToString());
            File.AppendAllText($"{csvBasePath}/{replayPlayerFileName}", sbPlayer.ToString());
        }
        return (replayAppendId, replayPlayerAppendId);
    }

    private async Task<Dictionary<PlayerId, int>> GetPlayerIdDic(RatingCalcType ratingCalcType)
    {
        if (IsSqlite)
        {
            return await GetSqlitePlayerIdDic();
        }

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var commandTxt = ratingCalcType == RatingCalcType.Arcade ?
    "SELECT ArcadePlayerId, ProfileId, RegionId, RealmId FROM ArcadePlayers;" :
    "SELECT PlayerId, ToonId, RegionId, RealmId FROM Players;";

        using var command = new MySqlCommand(commandTxt, connection);
        using var reader = await command.ExecuteReaderAsync();

        Dictionary<PlayerId, int> playerIdDic = new();

        while (await reader.ReadAsync())
        {
            int id = reader.GetInt32(0);
            int profileId = reader.GetInt32(1);
            int regionId = reader.GetInt32(2);
            int realmId = reader.GetInt32(3);

            PlayerId playerId = new() { ToonId = profileId, RegionId = regionId, RealmId = realmId };
            playerIdDic[playerId] = id;
        }

        return playerIdDic;
    }

    private async Task<Dictionary<PlayerId, int>> GetSqlitePlayerIdDic()
    {
        using var connection = new SQLiteConnection(connectionString);
        await connection.OpenAsync();

        var commandTxt = "SELECT PlayerId, ToonId, RegionId, RealmId FROM Players;";

        using var command = new SQLiteCommand(commandTxt, connection);
        using var reader = await command.ExecuteReaderAsync();

        Dictionary<PlayerId, int> playerIdDic = new();

        while (await reader.ReadAsync())
        {
            int id = reader.GetInt32(0);
            int profileId = reader.GetInt32(1);
            int regionId = reader.GetInt32(2);
            int realmId = reader.GetInt32(3);

            PlayerId playerId = new() { ToonId = profileId, RegionId = regionId, RealmId = realmId };
            playerIdDic[playerId] = id;
        }

        return playerIdDic;
    }
}