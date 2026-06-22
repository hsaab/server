using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Extensions.Logging;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;
using Serilog.Sinks.File;

namespace Bit.Core.Utilities;

internal static class SerilogFileLoggingConfiguration
{
    private const long DefaultFileSizeLimitBytes = 1024 * 1024 * 1024;
    private const int DefaultRetainedFileCountLimit = 31;
    private const string DefaultOutputTemplate =
        "{Timestamp:o} {requestId} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}";

    public static void AddFileWithRequestIdEnricher(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        var config = configuration.Get<FileLoggingOptions>();
        if (string.IsNullOrWhiteSpace(config?.PathFormat))
        {
            SelfLog.WriteLine("Unable to add the file logger: no PathFormat was present in the configuration");
            return;
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
            config.OutputTemplate ?? DefaultOutputTemplate);

        loggingBuilder.AddSerilog(logger, dispose: true);
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
            : new MessageTemplateTextFormatter(outputTemplate);

        var path = Environment.ExpandEnvironmentVariables(pathFormat);
        if (Path.GetFileNameWithoutExtension(path).EndsWith("{Date}", StringComparison.Ordinal))
        {
            path = path.Replace("{Date}", string.Empty, StringComparison.Ordinal);
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
                fileSizeLimitBytes: fileSizeLimitBytes ?? DefaultFileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit ?? DefaultRetainedFileCountLimit,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(2)));

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
        foreach (var overrideSection in configuration.GetSection("LogLevel").GetChildren().Where(cfg => cfg.Key != "Default"))
        {
            if (!Enum.TryParse(overrideSection.Value, out LogLevel value))
            {
                SelfLog.WriteLine(
                    "The level override setting `{0}` for `{1}` is invalid",
                    overrideSection.Value,
                    overrideSection.Key);
                continue;
            }

            levelOverrides[overrideSection.Key] = value;
        }

        return levelOverrides;
    }

    private sealed class FileLoggingOptions
    {
        public string? PathFormat { get; set; }
        public bool Json { get; set; }
        public long? FileSizeLimitBytes { get; set; } = DefaultFileSizeLimitBytes;
        public int? RetainedFileCountLimit { get; set; } = DefaultRetainedFileCountLimit;
        public string? OutputTemplate { get; set; }
    }
}
