using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Bit.Core.Utilities;

public sealed class RequestIdEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        var requestId = RequestIdHttpContext.GetRequestId(httpContext);
        if (string.IsNullOrEmpty(requestId))
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestId", requestId));
    }
}
