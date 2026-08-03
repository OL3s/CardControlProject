using System;
using CardGeneration.App;

namespace CardGeneration.Cli;

public sealed class CliProgressReporter : IDisposable
{
    private const int BarWidth = 24;
    private readonly bool _enabled;
    private bool _hasRendered;

    private CliProgressReporter(bool enabled)
    {
        _enabled = enabled;
    }

    public static CliProgressReporter Create(CliProgressMode mode)
    {
        var enabled = mode switch
        {
            CliProgressMode.Always => true,
            CliProgressMode.Never => false,
            _ => !Console.IsOutputRedirected
        };

        return new CliProgressReporter(enabled);
    }

    public void Report(ExportProgress progress)
    {
        if (!_enabled)
        {
            return;
        }

        var total = Math.Max(0, progress.Total);
        var current = Math.Clamp(progress.Current, 0, total);
        var ratio = total == 0 ? 0d : (double)current / total;
        var filled = (int)Math.Round(ratio * BarWidth, MidpointRounding.AwayFromZero);
        var line = $"\r{progress.Message} [{new string('=', filled)}{new string(' ', BarWidth - filled)}] {current}/{total}";
        Console.Error.Write(line.PadRight(120));
        _hasRendered = true;

        if (total > 0 && current >= total)
        {
            Console.Error.WriteLine();
            _hasRendered = false;
        }
    }

    public void Dispose()
    {
        if (_enabled && _hasRendered)
        {
            Console.Error.WriteLine();
        }
    }
}
