using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace pax.dsstats.web.Server.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class DownloadsController
{
    [HttpGet, DisableRequestSizeLimit]
    [Route("0.3.0/{fileName}")]
    public async Task<FileStreamResult?> Download1(string fileName)
    {
        if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            return null;
        }

        string filePath = $"/data/downloads/0.3.0/{fileName}";

        if (!File.Exists(filePath))
        {
            return null;
        }

        var memory = new MemoryStream();
        await using (var stream = new FileStream(filePath, FileMode.Open))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;
        return new FileStreamResult(memory, "application/octet-stream") { FileDownloadName = "sc2dsstats.maui_x64.msix" };
    }

    [HttpGet, DisableRequestSizeLimit]
    [Route("1.3/{fileName}")]
    public async Task<FileStreamResult?> Download2(string fileName)
    {
        var filePath = $"/data/downloads/1.3/setup.msix";

        var memory = new MemoryStream();
        await using (var stream = new FileStream(filePath, FileMode.Open))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;
        return new FileStreamResult(memory, "application/octet-stream") { FileDownloadName = fileName };
    }
}
