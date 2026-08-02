using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace CardGeneration.App;

public static class AppLogger
{
    private static readonly object FileLock = new();
    private static readonly string SessionId = $"{DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture)}_{System.Environment.ProcessId}";
    private static readonly string UserLogPath = $"user://logs/card_generation_{SessionId}.log";
    private static string? _globalLogPath;
    private static int _globalHandlersRegistered;

    public static string CurrentUserLogPath => UserLogPath;

    public static string CurrentGlobalLogPath => _globalLogPath ??= ProjectSettings.GlobalizePath(UserLogPath);

    public static void RegisterGlobalHandlers()
    {
        if (Interlocked.Exchange(ref _globalHandlersRegistered, 1) != 0)
        {
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                Error("Unhandled application exception.", "APP", exception);
            }
            else
            {
                Error($"Unhandled application error: {args.ExceptionObject}", "APP");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Error("Unobserved task exception.", "TASK", args.Exception);
            args.SetObserved();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Info("Application process exiting.", "APP");
    }

    public static void Debug(string message, string category = "APP") => Write("DEBUG", category, message);

    public static void Info(string message, string category = "APP") => Write("INFO", category, message);

    public static void Warning(string message, string category = "APP") => Write("WARN", category, message);

    public static void Error(string message, string category = "APP", Exception? exception = null)
    {
        Write("ERROR", category, message, exception);
    }

    public static void GuiInfo(string message) => Info(message, "GUI");

    public static void GuiWarning(string message) => Warning(message, "GUI");

    public static void GuiError(string message, Exception? exception = null) => Error(message, "GUI", exception);

    public static void CliInfo(string message) => Write("INFO", "CLI", message, writeToGodot: false);

    public static void CliError(string message, Exception? exception = null) => Write("ERROR", "CLI", message, exception, writeToGodot: false);

    public static Action WrapGuiAction(string action, Action callback, string? details = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return () => RunGuiAction(action, callback, details);
    }

    public static void RunGuiAction(string action, Action callback, string? details = null)
    {
        var context = string.IsNullOrWhiteSpace(details) ? action : $"{action}; {details}";
        var stopwatch = Stopwatch.StartNew();
        Debug($"START {context}", "GUI");

        try
        {
            callback();
            Debug($"DONE {action}; elapsed_ms={stopwatch.ElapsedMilliseconds}", "GUI");
        }
        catch (Exception exception)
        {
            GuiError($"FAILED {action}; elapsed_ms={stopwatch.ElapsedMilliseconds}", exception);
            throw;
        }
    }

    private static void Write(string level, string category, string message, Exception? exception = null, bool writeToGodot = true)
    {
        var builder = new StringBuilder()
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
            .Append(" [").Append(level).Append("]")
            .Append(" [").Append(category).Append("]")
            .Append(" [thread:").Append(System.Environment.CurrentManagedThreadId).Append("] ")
            .Append(message);

        if (exception is not null)
        {
            builder.AppendLine().Append(exception);
        }

        var entry = builder.ToString();
        if (!writeToGodot)
        {
            WriteToFile(entry);
            return;
        }

        if (level == "ERROR")
        {
            GD.PushError(entry);
        }
        else if (level == "WARN")
        {
            GD.PushWarning(entry);
        }
        else
        {
            GD.Print(entry);
        }

        WriteToFile(entry);
    }

    private static void WriteToFile(string entry)
    {
        try
        {
            lock (FileLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CurrentGlobalLogPath)!);
                File.AppendAllText(CurrentGlobalLogPath, entry + System.Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception fileException)
        {
            GD.PushError($"Could not write application log '{CurrentGlobalLogPath}': {fileException.Message}");
        }
    }
}
