using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace dsstats.api;

public class AuthenticationFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var authKey = context.HttpContext.Request
                .Headers["Authorization"].SingleOrDefault();

        if (authKey != "DS8upload77")
        {
            throw new BadHttpRequestException("", (int)HttpStatusCode.Unauthorized);
        }
    }
}
