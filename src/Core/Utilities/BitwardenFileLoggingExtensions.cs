using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

namespace Bit.Core.Utilities;

public static class BitwardenFileLoggingExtensions
{
    private const long DefaultFileSizeLimitBytes = 1024 * 1024 * 1024;
    private const int DefaultRetainedFileCountLimit = 31;
    private const string DefaultOutputTemplate =
        "{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}";

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

        var pathFormat = configuration["PathFormat"];
        if (string.IsNullOrWhiteSpace(pathFormat))
        {
            SelfLog.WriteLine("Unable to add the file logger: no PathFormat was present in the configuration");
            return loggingBuilder;
        }

        var minimumLevel = GetMinimumLogLevel(configuration);
        var levelOverrides = GetLevelOverrides(configuration);
        var isJson = bool.TryParse(configuration["Json"], out var json) && json;
        var fileSizeLimitBytes = GetFileSizeLimitBytes(configuration);
        var retainedFileCountLimit = GetRetainedFileCountLimit(configuration);
        var outputTemplate = configuration["OutputTemplate"] ?? DefaultOutputTemplate;

        var logger = CreateLogger(
            pathFormat,
            minimumLevel,
            levelOverrides,
            isJson,
            fileSizeLimitBytes,
            retainedFileCountLimit,
            outputTemplate);

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
        var formatter = isJson
            ? (ITextFormatter)new RenderedCompactJsonFormatter()
            : new MessageTemplateTextFormatter(outputTemplate, null);

        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(ToSerilogLevel(minimumLevel))
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
                ToSerilogLevel(levelOverride.Value));
        }

        return configuration.CreateLogger();
    }

    private static long? GetFileSizeLimitBytes(IConfiguration configuration)
    {
        if (!ConfigurationKeyExists(configuration, "FileSizeLimitBytes"))
        {
            return DefaultFileSizeLimitBytes;
        }

        var value = configuration["FileSizeLimitBytes"];
        return string.IsNullOrWhiteSpace(value)
            ? null
            : long.Parse(value, CultureInfo.InvariantCulture);
    }

    private static int? GetRetainedFileCountLimit(IConfiguration configuration)
    {
        if (!ConfigurationKeyExists(configuration, "RetainedFileCountLimit"))
        {
            return DefaultRetainedFileCountLimit;
        }

        var value = configuration["RetainedFileCountLimit"];
        return string.IsNullOrWhiteSpace(value)
            ? null
            : int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static bool ConfigurationKeyExists(IConfiguration configuration, string key)
    {
        return configuration.GetChildren().Any(child => child.Key == key);
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

    private static LogEventLevel ToSerilogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            LogLevel.None => LogEventLevel.Fatal,
            _ => LogEventLevel.Information,
        };
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
