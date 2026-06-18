using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Bit.Core.Utilities;

/// <summary>
/// Middleware that reads or generates an <c>X-Request-ID</c> header for distributed tracing.
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
            var requestId = headerValue.ToString();
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                return requestId;
            }
        }

        return Guid.NewGuid().ToString();
    }
}
