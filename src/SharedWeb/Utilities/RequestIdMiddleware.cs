using Bit.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Middleware to resolve or generate a request ID and propagate it through the HTTP pipeline and logs.
/// </summary>
/// <param name="next"></param>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string RequestIdHeaderName = "X-Request-ID";
    public const string RequestIdItemKey = "RequestId";

    public async Task Invoke(HttpContext context)
    {
        var requestId = ResolveRequestId(context);
        context.Items[RequestIdItemKey] = requestId;
        context.TraceIdentifier = requestId;
        context.Response.Headers[RequestIdHeaderName] = requestId;
        RequestIdContext.CurrentRequestId = requestId;

        try
        {
            using (LogContext.PushProperty("RequestId", requestId))
            {
                await next(context);
            }
        }
        finally
        {
            RequestIdContext.CurrentRequestId = null;
        }
    }

    public static string? GetRequestId(HttpContext? context)
    {
        if (context?.Items.TryGetValue(RequestIdItemKey, out var value) == true && value is string requestId)
        {
            return requestId;
        }

        return RequestIdContext.CurrentRequestId;
    }

    private static string ResolveRequestId(HttpContext context)
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
