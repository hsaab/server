using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Middleware to read or generate an X-Request-ID header for distributed tracing.
/// </summary>
/// <param name="next"></param>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string RequestIdHeaderName = "X-Request-ID";
    public const string RequestIdPropertyName = "RequestId";

    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);

        context.Response.Headers[RequestIdHeaderName] = requestId;

        using (LogContext.PushProperty(RequestIdPropertyName, requestId))
        {
            await next(context);
        }
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(RequestIdHeaderName, out var headerValue))
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
