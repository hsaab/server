using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Bit.SharedWeb.Utilities;

public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-ID";
    public const string HttpContextItemKey = "RequestId";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestIdMiddleware> _logger;

    public RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);
        context.Items[HttpContextItemKey] = requestId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = requestId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { [HttpContextItemKey] = requestId }))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var requestId = values.ToString().Trim();
            if (!string.IsNullOrEmpty(requestId))
            {
                return requestId;
            }
        }

        return Guid.NewGuid().ToString();
    }
}
