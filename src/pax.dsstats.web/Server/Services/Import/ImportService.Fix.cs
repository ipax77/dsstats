using pax.dsstats.shared;
using System.IO.Compression;
using System.Text.Json;
using System.Text;
using pax.dsstats.dbng;
using Microsoft.EntityFrameworkCore;

namespace pax.dsstats.web.Server.Services.Import;

public partial class ImportService
{
    public async Task FixPeza()
    {
        var blobDir = "/data/ds/test/PEZA";
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

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var uploader = await context.Uploaders
            .Include(i => i.Players)
            .FirstOrDefaultAsync(f => f.UploaderId == 181);

        if (uploader == null)
        {
            return;
        }

        int i = 0;
        foreach (var replayDto in replays)
        {
            var replay = await context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Player)
                .FirstOrDefaultAsync(f => f.ReplayHash == replayDto.ReplayHash);

            if (replay == null)
            {
                continue;
            }

            foreach (var rp in replay.ReplayPlayers)
            {
                if (uploader.Players.Any(a => a.ToonId == rp.Player.ToonId
                    && a.RegionId == rp.Player.RegionId
                    && a.RealmId == rp.Player.RealmId))
                {
                    rp.IsUploader = true;
                }
            }

            i++;
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("fixing ... {i}", i);
            }
        }
        await context.SaveChangesAsync();

    }
}
