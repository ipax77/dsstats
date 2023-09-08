using MySqlConnector;

namespace dsstats.ratings.db;

public partial class CalcRepository
{
    public async Task SetMainCmdr(int ratingType)
    {
        Dictionary<int, KeyValuePair<int, int>> playerMainDic = new();

        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

            var commandTxt =
    $@"SELECT rp.PlayerId, rp.Race, count(*) AS count
FROM Players as p
INNER JOIN PlayerRatings as pr ON pr.PlayerId = p.PlayerId
INNER JOIN ReplayPlayers as rp ON rp.PlayerId = p.PlayerId
INNER JOIN Replays as r ON r.ReplayId = rp.ReplayId
INNER JOIN ReplayRatings as rr ON rr.ReplayId = r.ReplayId
WHERE pr.RatingType = {ratingType} AND rr.RatingType = {ratingType} AND pr.Games > 9
GROUP BY rp.PlayerId, rp.Race";

            using var command = new MySqlCommand(commandTxt, connection);
            using var reader = await command.ExecuteReaderAsync();

            List<KeyValuePair<int, int>> currentList = new();

            int curPlayerId = 0;

            while (await reader.ReadAsync())
            {
                int playerId = reader.GetInt32(0);
                int race = reader.GetInt32(1);
                int count = reader.GetInt32(2);

                if (playerId == curPlayerId)
                {
                    currentList.Add(new(race, count));
                }
                else
                {
                    var mainInfo = GetMainInfo(currentList);
                    playerMainDic[curPlayerId] = mainInfo;

                    curPlayerId = playerId;
                    currentList = new() { new(race, count) };
                }
            }
        }

        await StoreMainCmdrInfo(playerMainDic, ratingType);
    }

    private async Task StoreMainCmdrInfo(Dictionary<int, KeyValuePair<int, int>> playerMainDic, int ratingType)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var playerEntry in playerMainDic)
            {
                var playerId = playerEntry.Key;
                var mainKey = playerEntry.Value.Key;
                var mainCount = playerEntry.Value.Value;

                var commandTxt = "UPDATE PlayerRatings SET Main = @Main, MainCount = @MainCount WHERE PlayerId = @PlayerId AND RatingType = @RatingType";
                using var command = new MySqlCommand(commandTxt, connection, transaction);
                command.Parameters.AddWithValue("@Main", mainKey);
                command.Parameters.AddWithValue("@MainCount", mainCount);
                command.Parameters.AddWithValue("@PlayerId", playerId);
                command.Parameters.AddWithValue("@RatingType", ratingType);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"failed storing players main info: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    private KeyValuePair<int, int> GetMainInfo(List<KeyValuePair<int, int>> list)
    {
        if (list.Count == 0)
        {
            return new(0, 0);
        }

        int max = list[0].Value;
        int main = list[0].Key;

        for (int i = 0; i < list.Count; i++)
        {
            var ent = list[i];
            if (ent.Value > max)
            {
                max = ent.Value;
                main = ent.Key;
            }
        }
        return new(main, max);
    }
}