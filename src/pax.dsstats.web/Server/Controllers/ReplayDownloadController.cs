using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;

namespace pax.dsstats.web.Server.Controllers;

[ApiController]
[Route("/api/v1/[controller]")]
public class ReplayDownloadController : Controller
{
    private readonly ReplayContext context;

    public ReplayDownloadController(ReplayContext context)
    {
        this.context = context;
    }


    [HttpGet, DisableRequestSizeLimit]
    [Route("{hash}")]
    public async Task<IActionResult> Download(string hash)
    {
        //var filePath = Path.Combine("/data/dsstats_replays/files", hash);
        //if (!System.IO.File.Exists(filePath))
        //{
        //    return NotFound();
        //}

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var replay = await context.Replays
            .Include(i => i.ReplayEvent)
                .ThenInclude(i => i.Event)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ReplayHash == hash);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        if (replay == null)
        {
            return NotFound();
        }

        context.ReplayDownloadCounts.Add(new()
        {
            ReplayHash = hash
        });
        await context.SaveChangesAsync();

        var memory = new MemoryStream();
        await using (var stream = new FileStream(replay.FileName, FileMode.Open))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;

        string fileName = $"{replay.ReplayEvent?.WinnerTeam?.Replace(" ", "_") ?? "Team1"}_vs_{replay.ReplayEvent?.RunnerTeam?.Replace(" ", "_") ?? "Team2"}.SC2Replay";


        return File(memory, "application/octet-stream", fileName);
    }
}
