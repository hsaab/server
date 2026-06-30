using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Extensions.Logging;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;
using Serilog.Sinks.RollingFile;

namespace Bit.Core.Utilities;

public static class BitwardenFileLoggingExtensions
{
    private const long DefaultFileSizeLimitBytes = 1_073_741_824;
    private const int DefaultRetainedFileCountLimit = 31;
    private const string DefaultOutputTemplate =
        "{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}";

    public static ILoggingBuilder AddBitwardenFileLogging(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(loggingBuilder);
        ArgumentNullException.ThrowIfNull(configuration);

        var pathFormat = configuration["PathFormat"];
        if (string.IsNullOrWhiteSpace(pathFormat))
        {
            SelfLog.WriteLine("Unable to add the file logger: no PathFormat was present in the configuration");
            return loggingBuilder;
        }

        var minimumLevel = GetMinimumLogLevel(configuration);
        var levelOverrides = GetLevelOverrides(configuration);
        var isJson = configuration.GetValue("Json", false);
        var fileSizeLimitBytes = configuration.GetValue<long?>("FileSizeLimitBytes") ?? DefaultFileSizeLimitBytes;
        var retainedFileCountLimit = configuration.GetValue("RetainedFileCountLimit", DefaultRetainedFileCountLimit);
        var outputTemplate = configuration["OutputTemplate"] ?? DefaultOutputTemplate;

        return loggingBuilder.AddBitwardenFileLogging(
            pathFormat,
            minimumLevel,
            levelOverrides,
            isJson,
            fileSizeLimitBytes,
            retainedFileCountLimit,
            outputTemplate);
    }

    public static ILoggingBuilder AddBitwardenFileLogging(
        this ILoggingBuilder loggingBuilder,
        string pathFormat,
        LogLevel minimumLevel = LogLevel.Information,
        IDictionary<string, LogLevel>? levelOverrides = null,
        bool isJson = false,
        long? fileSizeLimitBytes = DefaultFileSizeLimitBytes,
        int? retainedFileCountLimit = DefaultRetainedFileCountLimit,
        string outputTemplate = DefaultOutputTemplate)
    {
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
        ArgumentNullException.ThrowIfNull(pathFormat);
        ArgumentNullException.ThrowIfNull(outputTemplate);

        ITextFormatter formatter = isJson
            ? new RenderedCompactJsonFormatter()
            : new MessageTemplateTextFormatter(outputTemplate);

        var path = Environment.ExpandEnvironmentVariables(pathFormat);

        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(LevelConvert.ToSerilogLevel(minimumLevel))
            .Enrich.FromLogContext()
            .Enrich.With<RequestIdEnricher>()
            .WriteTo.Async(w => w.RollingFile(
                formatter,
                path,
                fileSizeLimitBytes: fileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(2)));

        foreach (var levelOverride in levelOverrides ?? new Dictionary<string, LogLevel>())
        {
            configuration.MinimumLevel.Override(levelOverride.Key, LevelConvert.ToSerilogLevel(levelOverride.Value));
        }

        return configuration.CreateLogger();
    }

    private static LogLevel GetMinimumLogLevel(IConfiguration configuration)
    {
        var minimumLevel = LogLevel.Information;
        var defaultLevel = configuration["LogLevel:Default"];
        if (!string.IsNullOrWhiteSpace(defaultLevel) &&
            !Enum.TryParse(defaultLevel, out minimumLevel))
        {
            SelfLog.WriteLine("The minimum level setting `{0}` is invalid", defaultLevel);
            minimumLevel = LogLevel.Information;
        }

        return minimumLevel;
    }

    private static Dictionary<string, LogLevel> GetLevelOverrides(IConfiguration configuration)
    {
        var levelOverrides = new Dictionary<string, LogLevel>();
        foreach (var levelOverride in configuration.GetSection("LogLevel").GetChildren()
                     .Where(section => section.Key != "Default"))
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
}
