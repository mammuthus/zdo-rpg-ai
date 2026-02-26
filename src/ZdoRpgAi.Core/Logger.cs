using Serilog;
using Serilog.Events;

namespace ZdoRpgAi.Core;

public enum LogLevel {
    Trace,
    Debug,
    Info,
    Warn,
    Error
}

public interface ILog {
    void Trace(string message);
    void Trace(string template, params object[] args);
    void Debug(string message);
    void Debug(string template, params object[] args);
    void Info(string message);
    void Info(string template, params object[] args);
    void Warn(string message);
    void Warn(string template, params object[] args);
    void Error(string message);
    void Error(string template, params object[] args);
    void Error(Exception ex, string message);
    void Error(Exception ex, string template, params object[] args);
}

public class LogConfig {
    public LogLevel ConsoleLevel { get; set; } = LogLevel.Info;
    public LogLevel FileLevel { get; set; } = LogLevel.Info;
    public string? FilePath { get; set; }
}

public static class Logger {
    public static ILog Get<T>() => new SerilogLog(Log.ForContext<T>());
    public static void Flush() => Log.CloseAndFlush();

    public static void Configure(LogConfig config) {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(
                restrictedToMinimumLevel: ToSerilog(config.ConsoleLevel),
                outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} [{SourceContext}] {Message:lj}{NewLine}{Exception}");

        if (config.FilePath != null) {
            RotateLogs(config.FilePath, 5);
            loggerConfig.WriteTo.File(
                config.FilePath,
                restrictedToMinimumLevel: ToSerilog(config.FileLevel),
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3} [{SourceContext}] {Message:lj}{NewLine}{Exception}");
        }

        Log.Logger = loggerConfig.CreateLogger();
    }

    private static void RotateLogs(string filePath, int maxFiles) {
        if (!File.Exists(filePath)) return;

        var dir = Path.GetDirectoryName(filePath)!;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath);

        var oldest = Path.Combine(dir, $"{name}.{maxFiles - 1}{ext}");
        if (File.Exists(oldest)) File.Delete(oldest);

        for (var i = maxFiles - 2; i >= 0; i--) {
            var src = Path.Combine(dir, $"{name}.{i}{ext}");
            if (File.Exists(src))
                File.Move(src, Path.Combine(dir, $"{name}.{i + 1}{ext}"));
        }

        File.Move(filePath, Path.Combine(dir, $"{name}.0{ext}"));
    }

    private static LogEventLevel ToSerilog(LogLevel level) => level switch {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Info => LogEventLevel.Information,
        LogLevel.Warn => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        _ => LogEventLevel.Information
    };
}

internal class SerilogLog(ILogger logger) : ILog {
    public void Trace(string message) => logger.Verbose(message);
    public void Trace(string template, params object[] args) => logger.Verbose(template, args);
    public void Debug(string message) => logger.Debug(message);
    public void Debug(string template, params object[] args) => logger.Debug(template, args);
    public void Info(string message) => logger.Information(message);
    public void Info(string template, params object[] args) => logger.Information(template, args);
    public void Warn(string message) => logger.Warning(message);
    public void Warn(string template, params object[] args) => logger.Warning(template, args);
    public void Error(string message) => logger.Error(message);
    public void Error(string template, params object[] args) => logger.Error(template, args);
    public void Error(Exception ex, string message) => logger.Error(ex, message);
    public void Error(Exception ex, string template, params object[] args) => logger.Error(ex, template, args);
}
