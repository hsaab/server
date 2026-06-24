using Microsoft.AspNetCore.Http;

#nullable enable

namespace Bit.SharedWeb.Utilities;

public static class RequestIdHttpContextExtensions
{
    public static string? GetRequestId(this HttpContext context)
    {
        return context.Items.TryGetValue(RequestIdMiddleware.HttpContextItemKey, out var value)
            ? value as string
            : null;
    }
}
