using Microsoft.AspNetCore.Http;

namespace Bit.Core.Utilities;

/// <summary>
/// Propagates the current request identifier to downstream HTTP services.
/// </summary>
public sealed class RequestIdDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestId = RequestIdUtilities.GetRequestId(httpContextAccessor.HttpContext);
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            request.Headers.TryAddWithoutValidation(RequestIdUtilities.HeaderName, requestId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
