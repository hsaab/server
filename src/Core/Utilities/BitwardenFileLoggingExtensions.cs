using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Extensions.Logging.File;
using Serilog.Formatting;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

namespace Bit.Core.Utilities;

public static class BitwardenFileLoggingExtensions
{
    public static ILoggingBuilder AddBitwardenFile(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        if (loggingBuilder == null)
        {
            throw new ArgumentNullException(nameof(loggingBuilder));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var config = configuration.Get<FileLoggingConfiguration>();
        if (string.IsNullOrWhiteSpace(config.PathFormat))
        {
            SelfLog.WriteLine("Unable to add the file logger: no PathFormat was present in the configuration");
            return loggingBuilder;
        }

        var minimumLevel = GetMinimumLogLevel(configuration);
        var levelOverrides = GetLevelOverrides(configuration);

        var logger = CreateLogger(
            config.PathFormat,
            minimumLevel,
            levelOverrides,
            config.Json,
            config.FileSizeLimitBytes,
            config.RetainedFileCountLimit,
            config.OutputTemplate);

        return loggingBuilder.AddSerilog(logger, dispose: true);
    }

    private static Serilog.Core.Logger CreateLogger(
        string pathFormat,
        LogLevel minimumLevel,
        IDictionary<string, LogLevel>? levelOverrides,
        bool isJson,
        long? fileSizeLimitBytes,
        int? retainedFileCountLimit,
        string outputTemplate)
    {
        if (pathFormat == null)
        {
            throw new ArgumentNullException(nameof(pathFormat));
        }

        if (outputTemplate == null)
        {
            throw new ArgumentNullException(nameof(outputTemplate));
        }

        var formatter = isJson
            ? (ITextFormatter)new RenderedCompactJsonFormatter()
            : new MessageTemplateTextFormatter(outputTemplate, null);

        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(Conversions.MicrosoftToSerilogLevel(minimumLevel))
            .Enrich.FromLogContext()
            .Enrich.With(new RequestIdEnricher())
            .WriteTo.Async(w => w.RollingFile(
                formatter,
                Environment.ExpandEnvironmentVariables(pathFormat),
                fileSizeLimitBytes: fileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(2)));

        if (!isJson)
        {
            configuration.Enrich.With<FileEventIdEnricher>();
        }

        foreach (var levelOverride in levelOverrides ?? new Dictionary<string, LogLevel>())
        {
            configuration.MinimumLevel.Override(
                levelOverride.Key,
                Conversions.MicrosoftToSerilogLevel(levelOverride.Value));
        }

        return configuration.CreateLogger();
    }

    private static LogLevel GetMinimumLogLevel(IConfiguration configuration)
    {
        var minimumLevel = LogLevel.Information;
        var defaultLevel = configuration["LogLevel:Default"];
        if (!string.IsNullOrWhiteSpace(defaultLevel))
        {
            if (!Enum.TryParse(defaultLevel, out minimumLevel))
            {
                SelfLog.WriteLine("The minimum level setting `{0}` is invalid", defaultLevel);
                minimumLevel = LogLevel.Information;
            }
        }

        return minimumLevel;
    }

    private static Dictionary<string, LogLevel> GetLevelOverrides(IConfiguration configuration)
    {
        var levelOverrides = new Dictionary<string, LogLevel>();
        foreach (var levelOverride in configuration.GetSection("LogLevel").GetChildren().Where(cfg => cfg.Key != "Default"))
        {
            if (!Enum.TryParse(levelOverride.Value, out LogLevel value))
            {
                SelfLog.WriteLine(
                    "The level override setting `{0}` for `{1}` is invalid",
                    levelOverride.Value,
                    levelOverride.Key);
                continue;
            }

            levelOverrides[levelOverride.Key] = value;
        }

        return levelOverrides;
    }

    private sealed class FileEventIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty(
                "EventId",
                new ScalarValue(EventIdHash.Compute(logEvent.MessageTemplate.Text))));
        }
    }
}
