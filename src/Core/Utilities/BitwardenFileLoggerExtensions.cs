using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Extensions.Logging;
using Serilog.Extensions.Logging.File;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

namespace Bit.Core.Utilities;

internal static class BitwardenFileLoggerExtensions
{
    public static ILoggingBuilder AddBitwardenFile(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(loggingBuilder);
        ArgumentNullException.ThrowIfNull(configuration);

        var config = configuration.Get<FileLoggingConfiguration>();
        if (string.IsNullOrWhiteSpace(config?.PathFormat))
        {
            SelfLog.WriteLine("Unable to add the file logger: no PathFormat was present in the configuration");
            return loggingBuilder;
        }

        var minimumLevel = GetMinimumLogLevel(configuration);
        var levelOverrides = GetLevelOverrides(configuration);

        var logger = CreateLogger(
            config.PathFormat!,
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
            .MinimumLevel.Is(LevelConvert.ToSerilogLevel(minimumLevel))
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

        if (!isJson)
        {
            configuration.Enrich.With<EventIdEnricher>();
        }

        foreach (var levelOverride in levelOverrides ?? new Dictionary<string, LogLevel>())
        {
            configuration.MinimumLevel.Override(
                levelOverride.Key,
                LevelConvert.ToSerilogLevel(levelOverride.Value));
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
}
