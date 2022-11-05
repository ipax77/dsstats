﻿using Microsoft.AspNetCore.Mvc;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Attributes;
using pax.dsstats.web.Server.Services;

namespace pax.dsstats.web.Server.Controllers;

[ServiceFilter(typeof(AuthenticationFilterAttribute))]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly UploadService uploadService;

    public UploadController(UploadService uploadService)
    {
        this.uploadService = uploadService;
    }

    [HttpPost]
    [Route("GetLatestReplayDate")]
    public async Task<ActionResult<DateTime>> GetLatestReplayDate(UploaderDto uploaderDto)
    {
        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);
        if (latestReplay == null)
        {
            return Unauthorized();
        }
        else
        {
            return latestReplay.Value;
        }
    }

    [HttpPost]
    [RequestSizeLimit(1024000000)]
    [Route("ImportReplays/{appGuid}")]
    public async Task<ActionResult> ImportReplays([FromBody] string base64string, Guid appGuid)
    {
        var result = await uploadService.ImportReplays(base64string, appGuid);
        if (result)
        {
            return Ok();
        }
        else
        {
            return BadRequest();
        }
    }

    [HttpGet]
    [Route("DisableUploader/{appGuid}")]
    public async Task<ActionResult<DateTime>> DisableUploader(Guid appGuid)
    {
        return await uploadService.DisableUploader(appGuid);
    }

    [HttpGet]
    [Route("DeleteUploader/{appGuid}")]
    public async Task<ActionResult<bool>> DeleteUploader(Guid appGuid)
    {
        return await uploadService.DeleteUploader(appGuid);
    }
}
