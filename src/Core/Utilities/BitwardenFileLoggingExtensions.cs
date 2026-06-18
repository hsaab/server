using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Extensions.Logging;
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

    public static ILoggingBuilder AddFileWithRequestIdEnrichment(
        this ILoggingBuilder loggingBuilder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(loggingBuilder);
        ArgumentNullException.ThrowIfNull(configuration);

        loggingBuilder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        var config = configuration.Get<BitwardenFileLoggingOptions>();
        if (string.IsNullOrWhiteSpace(config?.PathFormat))
        {
            SelfLog.WriteLine("Unable to add the file logger: no PathFormat was present in the configuration");
            return loggingBuilder;
        }

        var minimumLevel = GetMinimumLogLevel(configuration);
        var levelOverrides = GetLevelOverrides(configuration);

        loggingBuilder.Services.AddSingleton<ILoggerProvider>(sp =>
        {
            var enricher = new RequestIdEnricher(sp.GetRequiredService<IHttpContextAccessor>());
            var logger = CreateLogger(
                config.PathFormat!,
                minimumLevel,
                levelOverrides,
                config.Json,
                config.FileSizeLimitBytes,
                config.RetainedFileCountLimit,
                config.OutputTemplate,
                enricher);

            return new SerilogLoggerProvider(logger, dispose: true);
        });

        return loggingBuilder;
    }

    private static Serilog.Core.Logger CreateLogger(
        string pathFormat,
        LogLevel minimumLevel,
        IDictionary<string, LogLevel>? levelOverrides,
        bool isJson,
        long? fileSizeLimitBytes,
        int? retainedFileCountLimit,
        string? outputTemplate,
        RequestIdEnricher requestIdEnricher)
    {
        outputTemplate ??= DefaultOutputTemplate;

        ITextFormatter formatter = isJson
            ? new RenderedCompactJsonFormatter()
            : new MessageTemplateTextFormatter(outputTemplate, null);

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(ToSerilogLevel(minimumLevel))
            .Enrich.FromLogContext()
            .Enrich.With(requestIdEnricher)
            .WriteTo.Async(w => w.RollingFile(
                formatter,
                Environment.ExpandEnvironmentVariables(pathFormat),
                fileSizeLimitBytes: fileSizeLimitBytes ?? DefaultFileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit ?? DefaultRetainedFileCountLimit,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(2)));

        foreach (var levelOverride in levelOverrides ?? new Dictionary<string, LogLevel>())
        {
            loggerConfiguration.MinimumLevel.Override(levelOverride.Key, ToSerilogLevel(levelOverride.Value));
        }

        return loggerConfiguration.CreateLogger();
    }

    private static LogLevel GetMinimumLogLevel(IConfiguration configuration)
    {
        var minimumLevel = LogLevel.Information;
        var defaultLevel = configuration["LogLevel:Default"];
        if (!string.IsNullOrWhiteSpace(defaultLevel)
            && !Enum.TryParse(defaultLevel, out minimumLevel))
        {
            SelfLog.WriteLine("The minimum level setting `{0}` is invalid", defaultLevel);
            minimumLevel = LogLevel.Information;
        }

        return minimumLevel;
    }

    private static Dictionary<string, LogLevel> GetLevelOverrides(IConfiguration configuration)
    {
        var levelOverrides = new Dictionary<string, LogLevel>();
        foreach (var overr in configuration.GetSection("LogLevel").GetChildren().Where(cfg => cfg.Key != "Default"))
        {
            if (!Enum.TryParse(overr.Value, out LogLevel value))
            {
                SelfLog.WriteLine("The level override setting `{0}` for `{1}` is invalid", overr.Value, overr.Key);
                continue;
            }

            levelOverrides[overr.Key] = value;
        }

        return levelOverrides;
    }

    private static LogEventLevel ToSerilogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.None or LogLevel.Critical => LogEventLevel.Fatal,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Trace => LogEventLevel.Verbose,
            _ => LogEventLevel.Verbose,
        };
    }

    private sealed class BitwardenFileLoggingOptions
    {
        public string? PathFormat { get; set; }
        public bool Json { get; set; }
        public long? FileSizeLimitBytes { get; set; } = DefaultFileSizeLimitBytes;
        public int? RetainedFileCountLimit { get; set; } = DefaultRetainedFileCountLimit;
        public string OutputTemplate { get; set; } = DefaultOutputTemplate;
    }
}
