using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[EnableCors("dsstatsOrigin")]
[ApiController]
[Route("api8/v1/[controller]")]
public class FaqController(IFaqService faqService, IHttpContextAccessor httpContextAccessor)
{
    [HttpPost]
    public async Task<ActionResult<List<FaqDto>>> GetList(FaqRequest request, CancellationToken token = default)
    {
        return await faqService.GetList(request, token);
    }

    [HttpPost]
    [Route("count")]
    public async Task<ActionResult<int>> GetCount(FaqRequest request)
    {
        return await faqService.GetCount(request);
    }

    [HttpPost]
    [Route("create")]
    public async Task<ActionResult<int>> CreateFaq(FaqDto faqDto, string? name)
    {
        return await faqService.CreateFaq(faqDto, name);
    }

    [HttpPut]
    [Route("{faqId:int}")]
    public async Task<ActionResult> UpdateFaq(int faqId, FaqDto faqDto, string? name)
    {
        if (await faqService.UpdateFaq(faqDto, name))
        {
            return new NoContentResult();
        }
        return new NotFoundResult();
    }

    [Authorize]
    [HttpDelete]
    [Route("{faqId:int}")]
    public async Task<ActionResult> DeleteFaq(int faqId)
    {
        if (await faqService.DeleteFaq(faqId))
        {
            return new NoContentResult();
        }
        return new NotFoundResult();
    }

    [HttpGet]
    [Route("upvote/{faqId:int}")]
    public async Task<ActionResult> Upvote(int faqId)
    {
        var clientIpAddress = GetClientIpAddress(httpContextAccessor.HttpContext);

        if (string.IsNullOrEmpty(clientIpAddress))
        {
            return new BadRequestResult();
        }

        if (await faqService.Upvote(faqId, clientIpAddress))
        {
            return new CreatedResult();
        }
        else
        {
            return new UnauthorizedResult();
        }
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedForValues))
        {
            return forwardedForValues.FirstOrDefault();
        }
        else
        {
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
