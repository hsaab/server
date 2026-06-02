using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Middleware to read or generate an <c>X-Request-ID</c> header for distributed tracing.
/// </summary>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Request-ID";
    public const string LogPropertyName = "RequestId";

    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = requestId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(LogPropertyName, requestId))
        {
            await next(context);
        }
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var requestIdHeader))
        {
            var requestId = requestIdHeader.ToString();
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                return requestId;
            }
        }

        return Guid.NewGuid().ToString();
    }
}
