using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Bit.SharedWeb.Utilities;

public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string RequestIdHeaderName = "X-Request-ID";

    public async Task Invoke(HttpContext context)
    {
        var requestId = context.Request.Headers.TryGetValue(RequestIdHeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[RequestIdHeaderName] = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        {
            await next(context);
        }
    }
}
