using Serilog.Core;
using Serilog.Events;

namespace Bit.Core.Utilities;

public sealed class RequestIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var requestId = RequestIdContext.Current;
        if (string.IsNullOrEmpty(requestId))
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("requestId", requestId));
    }
}
