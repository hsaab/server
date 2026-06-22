using Bit.Core.Utilities;
using Microsoft.AspNetCore.Http;

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Ensures every HTTP request has a stable request identifier for distributed tracing.
/// Reads <see cref="RequestIdUtilities.HeaderName"/> from the incoming request or generates a new UUID,
/// stores it on the <see cref="HttpContext"/>, and echoes it on the response.
/// </summary>
/// <param name="next"></param>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var requestId = RequestIdUtilities.GetOrCreateRequestId(context);

        context.Response.OnStarting(() =>
        {
            RequestIdUtilities.TrySetResponseRequestId(context.Response, requestId);
            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        finally
        {
            if (!context.Response.HasStarted)
            {
                RequestIdUtilities.TrySetResponseRequestId(context.Response, requestId);
            }

            RequestIdContext.Current = null;
        }
    }
}
