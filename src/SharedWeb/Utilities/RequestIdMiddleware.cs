using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Bit.SharedWeb.Utilities;

public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Request-ID";

    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);

        context.Response.Headers[HeaderName] = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        {
            await next(context);
        }
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
        {
            var value = headerValue.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return Guid.NewGuid().ToString();
    }
}
