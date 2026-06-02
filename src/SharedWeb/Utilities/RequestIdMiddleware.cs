using Microsoft.AspNetCore.Http;
using Serilog.Context;

#nullable enable

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Propagates a shared <c>X-Request-ID</c> for distributed tracing across HTTP hops.
/// </summary>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string RequestIdHeaderName = "X-Request-ID";

    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);
        context.Response.Headers[RequestIdHeaderName] = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        {
            await next(context);
        }
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(RequestIdHeaderName, out var value))
        {
            var requestId = value.ToString();
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                return requestId;
            }
        }

        return Guid.NewGuid().ToString();
    }
}
