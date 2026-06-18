using Bit.Core.Utilities;
using Microsoft.AspNetCore.Http;

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Middleware that reads or generates an X-Request-ID for distributed tracing correlation.
/// </summary>
/// <param name="next"></param>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Request-ID";

    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);
        RequestIdHttpContext.SetRequestId(context, requestId);
        context.TraceIdentifier = requestId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = requestId;
            return Task.CompletedTask;
        });

        await next(context);
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue.ToString()))
        {
            return headerValue.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
