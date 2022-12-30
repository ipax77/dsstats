using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Controllers;


[ApiController]
[Route("[controller]")]
public class ReplayUploadController : ControllerBase
{
    private readonly ILogger<ReplayUploadController> logger;
    private readonly IMemoryCache memoryCache;
    private readonly TourneyService tourneyService;

    private const int maxAllowedFiles = 9;
    private const long maxFileSize = 1024 * 1024 * 5;

    public ReplayUploadController(ILogger<ReplayUploadController> logger, IMemoryCache memoryCache, TourneyService tourneyService)
    {
        this.logger = logger;
        this.memoryCache = memoryCache;
        this.tourneyService = tourneyService;
    }

    [HttpPost]
    public IActionResult PostInfo([FromBody] UploadInfo uploadInfo)
    {
        string key = $"uploadinfo{uploadInfo.UploadId}";
        memoryCache.Set(key, uploadInfo, TimeSpan.FromMinutes(15));
        return Ok();
    }

    [HttpPost]
    [Route("{guid}")]
    public async Task<ActionResult<IList<UploadResult>>> PostFile(Guid guid, [FromForm] IEnumerable<IFormFile> files)
    {

        var filesProcessed = 0;
        var resourcePath = new Uri($"{Request.Scheme}://{Request.Host}/");
        List<UploadResult> uploadResults = new();



        if (!memoryCache.TryGetValue($"uploadinfo{guid}", out UploadInfo uploadInfo))
        {
            logger.LogError($"failed getting uploadinfo for {guid}");
            return NotFound();
        }

        uploadInfo = uploadInfo with
        {
            Team1 = uploadInfo.Team1.Replace('-', '_'),
            Team2 = uploadInfo.Team2.Replace('-', '_'),
            Round = uploadInfo.Round.Replace('-', '_'),
        };

        foreach (var file in files)
        {
            var uploadResult = new UploadResult();
            var untrustedFileName = file.FileName;
            uploadResult.FileName = untrustedFileName;
            var trustedFileNameForDisplay =
                WebUtility.HtmlEncode(untrustedFileName);

            if (filesProcessed <= maxAllowedFiles)
            {
                if (file.Length == 0)
                {
                    logger.LogInformation("{FileName} length is 0 (Err: 1)",
                        trustedFileNameForDisplay);
                    uploadResult.ErrorCode = 1;
                }
                else if (file.Length > maxFileSize)
                {
                    logger.LogInformation("{FileName} of {Length} bytes is " +
                        "larger than the limit of {Limit} bytes (Err: 2)",
                        trustedFileNameForDisplay, file.Length, maxFileSize);
                    uploadResult.ErrorCode = 2;
                }
                else
                {
                    try
                    {
                        using MemoryStream ms = new MemoryStream();
                        await file.CopyToAsync(ms);
                        await tourneyService.SaveFile(ms, uploadInfo);
                        uploadResult.Uploaded = true;
                    }
                    catch (IOException ex)
                    {
                        logger.LogError("{FileName} error on upload (Err: 3): {Message}",
                            trustedFileNameForDisplay, ex.Message);
                        uploadResult.ErrorCode = 3;
                    }
                }
                filesProcessed++;
            }
            else
            {
                logger.LogInformation("{FileName} not uploaded because the " +
                    "request exceeded the allowed {Count} of files (Err: 4)",
                    trustedFileNameForDisplay, maxAllowedFiles);
                uploadResult.ErrorCode = 4;
            }

            uploadResults.Add(uploadResult);
        }
        return new CreatedResult(resourcePath, uploadResults);
    }
}
