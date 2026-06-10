using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Middleware for distributed tracing that reads or generates an <c>X-Request-ID</c> header,
/// echoes it on the response, and enriches structured logs for the request lifecycle.
/// </summary>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string RequestIdHeaderName = "X-Request-ID";
    public const string RequestIdLogPropertyName = "RequestId";

    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);
        context.Response.Headers[RequestIdHeaderName] = requestId;

        using (LogContext.PushProperty(RequestIdLogPropertyName, requestId))
        {
            await next(context);
        }
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(RequestIdHeaderName, out var headerValue))
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
