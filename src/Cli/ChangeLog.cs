using Core.Events;

namespace Cli;

/// <summary>
/// Спільне для всіх одержувачів сповіщень у Cli: формат рядка журналу і
/// безпечний дозапис у файл.
/// </summary>
internal static class ChangeLog
{
    public static string Format(OrderChangedEventArgs e) =>
        $"{e.At:HH:mm:ss} {e.Kind,-9} {e.OrderId} {e.Delta,10:+0.00;-0.00} " +
        $"→ сума {e.Total:F2} грн, рядків {e.LinesCount}";

    /// <summary>
    /// Дописує рядок у файл. Помилку диска перехоплює тут же: обробник
    /// сповіщення викликається синхронно всередині операції сервісу, і
    /// необроблений виняток зламав би вже успішну бізнес-операцію.
    /// </summary>
    public static bool TryAppend(string path, string line)
    {
        try
        {
            File.AppendAllText(path, line + Environment.NewLine);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"  ! не вдалося записати в {path}: {ex.Message}");
            return false;
        }
    }
}
