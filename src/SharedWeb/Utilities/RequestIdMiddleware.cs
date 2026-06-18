using Microsoft.AspNetCore.Http;

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Middleware to read or generate an <c>X-Request-ID</c> header for distributed tracing.
/// </summary>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);
        context.Items[RequestIdConstants.HttpContextItemKey] = requestId;
        context.Response.Headers[RequestIdConstants.HeaderName] = requestId;

        await next(context);
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(RequestIdConstants.HeaderName, out var headerValue))
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
