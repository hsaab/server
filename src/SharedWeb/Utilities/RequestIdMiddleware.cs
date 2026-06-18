using Microsoft.AspNetCore.Http;

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Middleware for distributed tracing that reads or generates an <c>X-Request-ID</c> header value.
/// </summary>
/// <param name="next"></param>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Request-ID";
    public const string HttpContextItemKey = "RequestId";

    public async Task Invoke(HttpContext context)
    {
        var requestId = ResolveRequestId(context);
        context.Items[HttpContextItemKey] = requestId;
        context.Response.Headers[HeaderName] = requestId;

        await next(context);
    }

    private static string ResolveRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var incoming) &&
            !string.IsNullOrWhiteSpace(incoming))
        {
            return incoming.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
