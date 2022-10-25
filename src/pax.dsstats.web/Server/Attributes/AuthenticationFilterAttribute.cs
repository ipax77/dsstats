using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace pax.dsstats.web.Server.Attributes;

public class AuthenticationFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var authKey = context.HttpContext.Request
                .Headers["Authorization"].SingleOrDefault();

        if (authKey != "DSupload77")
        {
            throw new HttpException(HttpStatusCode.Unauthorized);
        }
    }
}

public class HttpException : Exception
{
    public int StatusCode { get; }

    public HttpException(HttpStatusCode httpStatusCode)
        : base(httpStatusCode.ToString())
    {
        this.StatusCode = (int)httpStatusCode;
    }
}
