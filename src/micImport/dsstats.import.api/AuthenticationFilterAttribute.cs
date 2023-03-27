using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace dsstats.import.api;

public class AuthenticationFilterAttribute : ActionFilterAttribute
{
    private string _auth;

    public AuthenticationFilterAttribute(IConfiguration configuration)
    {
        _auth = configuration["ServerConfig:ImportAuthSecret"] ?? Guid.NewGuid().ToString();
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var authKey = context.HttpContext.Request
                .Headers["Authorization"].SingleOrDefault();

        if (authKey != _auth)
        {
            throw new HttpException(HttpStatusCode.Unauthorized);
        }
        base.OnActionExecuting(context);
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