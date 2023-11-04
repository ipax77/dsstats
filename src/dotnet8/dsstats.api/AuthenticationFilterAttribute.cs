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
