using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Middleware to read or generate an X-Request-ID header for distributed tracing.
/// </summary>
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
            var requestId = headerValue.ToString();
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                return requestId;
            }
        }

        return Guid.NewGuid().ToString();
    }
}
