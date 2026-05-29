using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Bit.Core.Utilities;

/// <summary>
/// Propagates a distributed tracing request ID via the X-Request-ID header and Serilog log context.
/// </summary>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string RequestIdHeaderName = "X-Request-ID";
    public const string RequestIdLogPropertyName = "RequestId";

    public async Task Invoke(HttpContext context)
    {
        var requestId = ResolveRequestId(context);

        context.Response.Headers[RequestIdHeaderName] = requestId;

        using (LogContext.PushProperty(RequestIdLogPropertyName, requestId))
        {
            await next(context);
        }
    }

    private static string ResolveRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(RequestIdHeaderName, out var incomingRequestId))
        {
            var requestId = incomingRequestId.ToString();
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                return requestId;
            }
        }

        return Guid.NewGuid().ToString();
    }
}
