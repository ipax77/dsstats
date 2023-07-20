
using System.Text.Json;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace dsstats.cli;

public static class BlobCheck
{
    public static void CheckBlob(string jsonBlob)
    {
        var replayDtos = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(jsonBlob));

        if (replayDtos == null || replayDtos.Count == 0)
        {
            return;
        }

        PlayerId interest = new(466786, 2, 2);

        int wins = 0;
        int loss = 0;

        replayDtos = replayDtos.Where(x => x.GameTime > new DateTime(2023, 6, 13)).ToList();

        foreach (ReplayDto replay in replayDtos)
        {
            var hash = Data.GenHash(replay);

            if (replay.ReplayHash != hash)
            {
                Console.WriteLine($"hash manipulated!");
            }

            var intrp = replay.ReplayPlayers.FirstOrDefault(f => f.Player.ToonId == interest.ToonId
                && f.Player.RealmId == interest.RealmId
                && f.Player.RegionId == interest.RegionId);

            if (intrp == null)
            {
                Console.WriteLine($"no interest player");
            }
            else
            {
                if (intrp.PlayerResult == PlayerResult.Win)
                {
                    wins++;
                }
                else
                {
                    loss++;
                }
            }
        }

        var repsOrdered = replayDtos.OrderByDescending(o => o.GameTime);
        var first = repsOrdered.First().GameTime;
        var last = repsOrdered.Last().GameTime;

        Console.WriteLine($"check done. First: {first.ToShortDateString()}, Last: {last.ToShortDateString()} Wins: {wins}, Loss: {loss}");
    }
}