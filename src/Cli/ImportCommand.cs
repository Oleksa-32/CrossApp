using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Core;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Cli;

/// <summary>
/// Підкоманда import-orders (лабораторна 8): асинхронний імпорт замовлень із JSON.
/// Тут лише вибір режиму за прапорцями і вивід; скасування, таймаут і доменні помилки
/// перехоплює Program.cs, а правила імпорту — у Core.
/// </summary>
/// <remarks>
/// import-orders [шлях] [--stream] [--timeout=мс] [--big=N] [--repeat] [--file]
/// </remarks>
internal static class ImportCommand
{
    public const string Name = "import-orders";

    private const int FileBufferSize = 4096;
    private static readonly string DefaultPath = Path.Combine("data", "orders-import.json");

    public static async Task<int> RunAsync(OrderService service, string[] args, CancellationToken ct)
    {
        if (!TryParseInt(args, "--timeout=", out int? timeoutMs) || !TryParseInt(args, "--big=", out int? bigCount))
        {
            Console.WriteLine("Значення --timeout=<мс> і --big=<кількість> мають бути цілими додатними числами.");
            return 1;
        }

        bool stream = HasFlag(args, "--stream");
        TimeSpan? timeout = timeoutMs is int ms ? TimeSpan.FromMilliseconds(ms) : null;
        string path = bigCount is int count
            ? await EnsureBigFileAsync(count, ct)
            : args.Skip(1).FirstOrDefault(argument => !argument.StartsWith("--")) ?? DefaultPath;

        Console.WriteLine($"Файл: {Path.GetFullPath(path)}");
        Console.WriteLine($"Режим: {(stream ? "потік IAsyncEnumerable (ImportStreamAsync)" : "список (ImportAsync → LoadAsync)")}, " +
                          $"таймаут: {(timeout is TimeSpan t ? $"{t.TotalSeconds:0.###} с" : "немає")}; Ctrl+C — скасувати");

        int imported = 0;
        service.Changed += (_, e) =>
        {
            if (e.Kind == OrderChangeKind.Imported)
                imported++;
        };

        // Одна функція для обох режимів: WithTimeoutAsync підставить зв'язаний токен.
        Task<int> Import(JsonOrderImporter importer, CancellationToken token) => stream
            ? service.ImportStreamAsync(importer, path, token)
            : service.ImportAsync(importer, path, token);

        var progress = new ConsoleProgress();
        var importer = new JsonOrderImporter(progress);
        var stopwatch = Stopwatch.StartNew();
        int added;
        try
        {
            added = timeout is TimeSpan limit
                ? await service.WithTimeoutAsync(token => Import(importer, token), limit, ct)
                : await Import(importer, ct);
        }
        finally
        {
            progress.Finish();   // і при скасуванні: повідомлення Program.cs — з нового рядка
        }

        Console.WriteLine($"Імпортовано записів: {added}");
        Console.WriteLine($"Подій Changed (Imported): {imported}; замовлень у сховищі: {service.All().Count}; " +
                          $"час: {stopwatch.ElapsedMilliseconds} мс");

        if (HasFlag(args, "--repeat"))
        {
            int again = await Import(new JsonOrderImporter(), ct);
            Console.WriteLine($"Повторний імпорт того самого файлу: {again} (наявні id пропущено)");
        }

        return 0;
    }

    /// <summary>
    /// Великий файл для демонстрації скасування: <paramref name="count"/> згенерованих
    /// замовлень, створюється один раз у тимчасовому каталозі ОС і далі перевикористовується.
    /// </summary>
    private static async Task<string> EnsureBigFileAsync(int count, CancellationToken ct)
    {
        string path = Path.Combine(Path.GetTempPath(), $"crossapp-orders-{count}.json");
        if (File.Exists(path))
            return path;

        Console.WriteLine($"Генерую {count} замовлень…");
        string temporary = path + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None,
                             FileBufferSize, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, SampleData.Generate(count).Select(order => order.ToDto()),
                    cancellationToken: ct);
            }

            File.Move(temporary, path, overwrite: true);
            return path;
        }
        catch (OperationCanceledException)
        {
            File.Delete(temporary);   // недописаний файл не повинен стати «готовим» наступного разу
            throw;
        }
    }

    private static bool HasFlag(string[] args, string flag) =>
        args.Contains(flag, StringComparer.OrdinalIgnoreCase);

    // Прапорець зі значенням через "=": "--timeout 2000" зробило б 2000 позиційним аргументом.
    private static bool TryParseInt(string[] args, string prefix, out int? value)
    {
        value = null;
        string? argument = args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (argument is null)
            return true;

        if (!int.TryParse(argument[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
            || parsed <= 0)
            return false;

        value = parsed;
        return true;
    }
}
