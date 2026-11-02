namespace Cli;

/// <summary>
/// Підписник IProgress&lt;int&gt;, що малює відсоток прочитаного файлу (лабораторна 8,
/// додаткове завдання 3). Звіт друкується одразу в потоці, що повідомив: стандартний
/// Progress&lt;T&gt; пересилає виклик у SynchronizationContext, а в консолі його немає —
/// виклики пішли б у пул потоків і могли б надрукуватися вже після підсумку імпорту.
/// </summary>
internal sealed class ConsoleProgress : IProgress<int>
{
    private const int BarWidth = 30;
    private const int Complete = 100;
    private const int RedirectedStep = 25;   // у файл чи конвеєр — рядок на кожні 25 %, без \r

    private bool _drawn;
    private int _lastStep = -1;

    public void Report(int percent)
    {
        if (Console.IsOutputRedirected)
        {
            int step = percent / RedirectedStep;
            if (step == _lastStep)
                return;
            _lastStep = step;
            Console.WriteLine($"Читання файлу: {percent,3} %");
            return;
        }

        int filled = percent * BarWidth / Complete;
        Console.Write($"\rЧитання файлу: [{new string('#', filled)}{new string('-', BarWidth - filled)}] {percent,3} %");
        _drawn = true;
    }

    /// <summary>Переводить рядок після смуги, щоб наступний вивід не дописався в неї.</summary>
    public void Finish()
    {
        if (_drawn)
            Console.WriteLine();
        _drawn = false;
        _lastStep = -1;
    }
}
