using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
#pragma warning disable IDE0005
using Serilog.Extensions.Logging;
#pragma warning restore IDE0005
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

namespace Bit.Core.Utilities;

internal static class BitwardenFileLoggerExtensions
{
    private const long DefaultFileSizeLimitBytes = 1024L * 1024 * 1024;
    private const int DefaultRetainedFileCountLimit = 31;
    private const string DefaultOutputTemplate =
        "{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}";

    public static ILoggingBuilder AddBitwardenFile(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(loggingBuilder);
        ArgumentNullException.ThrowIfNull(configuration);

        var config = new FileLoggingOptions();
        configuration.Bind(config);

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
            config.FileSizeLimitBytes ?? DefaultFileSizeLimitBytes,
            config.RetainedFileCountLimit ?? DefaultRetainedFileCountLimit,
            config.OutputTemplate ?? DefaultOutputTemplate);

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
        ArgumentNullException.ThrowIfNull(pathFormat);
        ArgumentNullException.ThrowIfNull(outputTemplate);

        var formatter = isJson
            ? (ITextFormatter)new RenderedCompactJsonFormatter()
            : new MessageTemplateTextFormatter(outputTemplate);

        var path = Environment.ExpandEnvironmentVariables(pathFormat);
        if (Path.GetFileNameWithoutExtension(path).EndsWith("{Date}", StringComparison.Ordinal))
        {
            path = path.Replace("{Date}", "", StringComparison.Ordinal);
        }
        else
        {
            SelfLog.WriteLine(
                "The log file name `{0}` should end with `{Date}`; other formats or rolling strategies are not supported",
                path);
        }

        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(ToSerilogLevel(minimumLevel))
            .Enrich.FromLogContext()
            .Enrich.With<RequestIdEnricher>()
            .WriteTo.Async(w => w.File(
                formatter,
                path,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: fileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(2)));

        foreach (var levelOverride in levelOverrides ?? new Dictionary<string, LogLevel>())
        {
            configuration.MinimumLevel.Override(
                levelOverride.Key,
                ToSerilogLevel(levelOverride.Value));
        }

        return configuration.CreateLogger();
    }

    private static LogEventLevel ToSerilogLevel(LogLevel level) => level switch
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

    private sealed class FileLoggingOptions
    {
        public string? PathFormat { get; set; }
        public bool Json { get; set; }
        public long? FileSizeLimitBytes { get; set; }
        public int? RetainedFileCountLimit { get; set; }
        public string? OutputTemplate { get; set; }
    }
}
