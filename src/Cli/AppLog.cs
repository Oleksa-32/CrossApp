using Core.Configuration;

namespace Cli;

/// <summary>
/// Найпростіший журнал у консоль із порогом Logging:MinimumLevel (лабораторна 9): у профілі
/// Development видно діагностичні рядки [debug], у Production — лише попередження й помилки.
/// Звичайний результат команди (Console.WriteLine) від рівня журналу не залежить.
/// </summary>
internal sealed class AppLog(LogLevel minimum)
{
    public bool IsEnabled(LogLevel level) => level >= minimum;

    public void Debug(string message) => Write(LogLevel.Debug, message);
    public void Info(string message) => Write(LogLevel.Information, message);
    public void Warn(string message) => Write(LogLevel.Warning, message);

    private void Write(LogLevel level, string message)
    {
        if (!IsEnabled(level))
            return;

        string tag = level switch
        {
            LogLevel.Trace => "trace",
            LogLevel.Debug => "debug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            _ => "error"
        };
        Console.WriteLine($"[{tag}] {message}");
    }
}
