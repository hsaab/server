using Bit.Core.Utilities;

namespace Bit.Api.Middleware;

public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var requestId = ResolveRequestId(context);

        context.Items[RequestIdConstants.ItemKey] = requestId;
        context.TraceIdentifier = requestId;
        context.Response.Headers[RequestIdConstants.HeaderName] = requestId;

        await next(context);
    }

    private static string ResolveRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(RequestIdConstants.HeaderName, out var headerValue)
            && Guid.TryParse(headerValue.ToString(), out var existingRequestId))
        {
            return existingRequestId.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
