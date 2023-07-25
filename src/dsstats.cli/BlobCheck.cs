
using System.Drawing.Printing;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace dsstats.cli;

public static class BlobCheck
{
    public static async Task CheckBlobs(string blobDir)
    {
        var files = Directory.GetFiles(blobDir);
        List<ReplayDto> replays = new();

        foreach (var file in files)
        {
            var bytes = Convert.FromBase64String(await File.ReadAllTextAsync(file, Encoding.UTF8));
            using var msi = new MemoryStream(bytes);
            var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                await gs.CopyToAsync(mso);
            }
            mso.Position = 0;

            var replayDtos = await JsonSerializer
                .DeserializeAsync<List<ReplayDto>>(mso);

            if (replayDtos != null)
                replays.AddRange(replayDtos);
        }

        List<PlayerId> PlayerIds = new()
        {
            new(2474605, 0, 2),
            new(2474605, 1, 2),
            new(2474605, 2, 2),
            new(2474605, 0, 1),
            new(2474605, 1, 1),
            new(2474605, 2, 1),
        };
        int wins = 0;
        int loss = 0;
        int uploader = 0;
        int skip = 0;
        int computer = 0;

        List<GameMode> gameModes = new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic };    

        foreach (ReplayDto replay in replays)
        {
            if (replay.WinnerTeam == 0
                || replay.Duration < 300
                || !gameModes.Contains(replay.GameMode))
            {
                skip++;
                continue;
            }

            if (replay.ReplayPlayers.Any(a => a.Name.StartsWith("Компьютер ")))
            {
                computer++;
                continue;
            }

            var hash = Data.GenHash(replay);

            if (replay.ReplayHash != hash)
            {
                Console.WriteLine($"hash manipulated!");
                Console.WriteLine(replay);
            }

            var intrp = replay.ReplayPlayers.FirstOrDefault(f => PlayerIds.Any(a => f.Player.ToonId == a.ToonId
                && f.Player.RealmId == a.RealmId
                && f.Player.RegionId == a.RegionId));

            if (intrp == null)
            {
            }
            else
            {
                if (intrp.IsUploader)
                {
                    uploader++;
                }

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

        var repsOrdered = replays.OrderByDescending(o => o.GameTime);
        var first = repsOrdered.First().GameTime;
        var last = repsOrdered.Last().GameTime;

        Console.WriteLine($"check done. Count: {replays.Count} Wins: {wins}, Loss: {loss}, uploader: {uploader}, skip: {skip}, computer: {computer}");
    }


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