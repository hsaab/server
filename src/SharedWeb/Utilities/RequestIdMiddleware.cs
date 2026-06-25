using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Bit.SharedWeb.Utilities;

public sealed class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);
        context.Items[RequestIdContext.HttpContextItemKey] = requestId;
        context.Response.Headers[RequestIdContext.HeaderName] = requestId;

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            [RequestIdContext.LogPropertyName] = requestId,
        }))
        {
            await next(context);
        }
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(RequestIdContext.HeaderName, out var headerValue))
        {
            var requestId = headerValue.ToString().Trim();
            if (!string.IsNullOrEmpty(requestId))
            {
                return requestId;
            }
        }

        return Guid.NewGuid().ToString();
    }
}
