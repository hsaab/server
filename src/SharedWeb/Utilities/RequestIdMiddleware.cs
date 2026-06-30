using Bit.Core.Utilities;
using Microsoft.AspNetCore.Http;

namespace Bit.SharedWeb.Utilities;

public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Request-ID";
    public const string HttpContextItemKey = "RequestId";

    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);

        context.Items[HttpContextItemKey] = requestId;
        RequestIdContext.Current = requestId;
        context.Response.Headers[HeaderName] = requestId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = requestId;
            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        finally
        {
            RequestIdContext.Current = null;
        }
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
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
