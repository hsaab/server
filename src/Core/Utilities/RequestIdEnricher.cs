using Serilog.Core;
using Serilog.Events;

namespace Bit.Core.Utilities;

/// <summary>
/// Adds the current request ID to Serilog log events for the active HTTP request.
/// </summary>
public sealed class RequestIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.ContainsKey("RequestId"))
        {
            return;
        }

        var requestId = RequestIdContext.CurrentRequestId;
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestId", requestId));
    }
}
