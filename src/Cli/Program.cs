using System.Text;
using System.Text.Json;
using Cli;
using Core;
using Core.Abstractions;
using Core.Dto;
using Core.Import;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = Encoding.UTF8;

const int SeparatorWidth = 52;
const int LabelWidth = 22;
const int PreviewRows = 5;

// Режим лабораторної 2 залишено доступним за прапорцем: наскрізний проєкт
// не втрачає попередніх можливостей, коли додається нова.
if (args.Contains("--env", StringComparer.OrdinalIgnoreCase))
{
    PrintEnvironment(args);
    return 0;
}

// Лабораторна 9: конфігурація збирається один раз і до вибору режиму — шлях до файлу сховища
// тепер приходить з appsettings*.json / user-secrets / змінних CROSSAPP_*, а не з коду.
AppConfiguration config;
try
{
    config = AppConfiguration.Build(args);
}
catch (Exception ex) when (ex is InvalidOperationException or FormatException or InvalidDataException
                               or FileNotFoundException or IOException or UnauthorizedAccessException)
{
    // Неіснуючий рівень логу, зіпсований appsettings.json, недоступний каталог даних —
    // повідомлення замість стека виклику.
    Console.WriteLine($"Помилка конфігурації: {ex.Message}");
    return 1;
}
var log = new AppLog(config.Options.Logging.MinimumLevel);

// Режим змішаного файлу: рядки різних типів розрізняються префіксом у першій колонці.
bool mixed = args.Contains("--mixed", StringComparer.OrdinalIgnoreCase);

// Режим лабораторної 4: доменна модель, сценарії «успіх» і «порушення інваріантів».
bool domain = args.Contains("--domain", StringComparer.OrdinalIgnoreCase);

// Перший аргумент без "--" — це підкоманда (report-* лабораторної 7, import-orders 8-ї) або шлях до
// файлу імпорту. Прапорці на кшталт --file назвою звіту не стануть.
string? firstArgument = args.FirstOrDefault(argument => !argument.StartsWith("--"));

// Лабораторна 8: асинхронний імпорт замовлень з Ctrl+C і таймаутом — окрема гілка з await.
if (string.Equals(firstArgument, ImportCommand.Name, StringComparison.OrdinalIgnoreCase))
    return await RunImportAsync(args, config.DataFile);

// Лабораторна 9: config, export, import — експорт та імпорт усієї колекції у JSON.
if (ExchangeCommands.IsCommand(firstArgument))
    return ExchangeCommands.Run(firstArgument!, args, config, log);

string? reportCommand = firstArgument is not null && firstArgument.StartsWith("report", StringComparison.OrdinalIgnoreCase)
    ? firstArgument.ToLowerInvariant()
    : null;

// Шлях приходить аргументом; типове значення будується через Path.Combine,
// щоб не зашивати роздільник каталогів конкретної ОС.
string? filePath = reportCommand is null ? firstArgument : null;

// Імпорт лабораторної 3 — за прапорцем --import або коли передано шлях до файлу.
bool import = args.Contains("--import", StringComparer.OrdinalIgnoreCase) || filePath is not null;

// Режим лабораторної 6: події сервісу, підписка, відписка, журнал logs/app.log.
bool events = args.Contains("--events", StringComparer.OrdinalIgnoreCase);

// Додаткове завдання 1 лабораторної 6: одержувач сповіщень через конструктор сервісу.
bool sinkToConsole = args.Contains("--sink-console", StringComparer.OrdinalIgnoreCase);
bool sinkToFile = args.Contains("--sink-file", StringComparer.OrdinalIgnoreCase);

// Самоперевірка лабораторної 7: звіти на порожньому сховищі не повинні падати.
bool empty = args.Contains("--empty", StringComparer.OrdinalIgnoreCase);

// Типовий режим (лабораторна 5), --events і звіти. Composition root: єдине місце, де названо
// конкретні класи сховищ; далі сервіс і сценарій бачать лише IOrderStore.
if (!mixed && !domain && !import)
{
    bool useFile = args.Contains("--file", StringComparer.OrdinalIgnoreCase);
    string dataPath = config.DataFile;

    IOrderStore store = useFile
        ? new FileOrderStore(dataPath)
        : new InMemoryOrderStore(empty ? [] : SampleData.Orders());
    if (args.Contains("--cache", StringComparer.OrdinalIgnoreCase))
        store = new CachingOrderStore(store);
    INotificationSink? sink = (sinkToFile, sinkToConsole) switch
    {
        (true, _) => new FileSink(PrepareLogPath("sink.log")),
        (_, true) => new ConsoleSink(),
        _ => null
    };
    var service = new OrderService(store, TimeProvider.System, sink);

    Console.WriteLine($"Сховище: {store}");
    try
    {
        int exitCode = (sink, events) switch
        {
            _ when reportCommand is not null => ReportCommands.Run(reportCommand, service),
            (not null, _) => SinkScenario.Run(service, sink, SampleData.Products()),
            (_, true) => EventScenario.Run(service, SampleData.Products(), PrepareLogPath("app.log")),
            _ => OrderScenario.Run(service, store, SampleData.Products())
        };
        if (store is CachingOrderStore caching)
            Console.WriteLine($"Кеш: влучань {caching.Hits}, промахів {caching.Misses}");
        return exitCode;
    }
    catch (InvalidDataException ex)
    {
        // Старий формат, обірваний JSON або запис, що порушує інваріанти.
        Console.WriteLine(ex.Message);
        Console.WriteLine($"Видаліть {dataPath} і запустіть знову.");
        return 1;
    }
}

string defaultFile = (mixed, domain) switch
{
    (true, _) => "mixed.csv",
    (_, true) => "catalog.csv",
    _ => "sample.csv"
};
string path = filePath ?? Samples.PathOf(defaultFile);

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

if (domain)
    return DomainDemo.Run(path);

if (mixed)
    return PrintMixed(MixedCsvImporter.Load(path), path);

// Додаткове завдання 2 лабораторної 6: імпортер повідомляє про кожен пропущений
// рядок через Action<string>, а Cli вирішує, що з цим робити, — пише файл звіту.
string skippedReportPath = PrepareLogPath("import-skipped.log");
ChangeLog.TryAppend(skippedReportPath, $"--- імпорт {Path.GetFullPath(path)} {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} ---");
Action<string> reportSkipped = reason => ChangeLog.TryAppend(skippedReportPath, reason);

// Формат обирається за розширенням — знову switch expression з константними патернами.
ImportResult<ProductDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => ProductCsvImporter.Load(path, reportSkipped),
    ".json" => ProductJsonImporter.Load(path, reportSkipped),
    string other => new ImportResult<ProductDto>([], [$"невідоме розширення '{other}': очікую .csv або .json"])
};

Console.WriteLine($"Файл: {Path.GetFullPath(path)}");
Console.WriteLine($"Завантажено записів: {result.Items.Count}");

foreach (ProductDto product in result.Items.Take(PreviewRows))
    Console.WriteLine($"  {product.Id,-6} {product.Name,-32} {product.Price,10:F2} грн");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string error in result.Errors)
        Console.WriteLine($"  ! {error}");
}

// Статистика імпорту одним рядком — заготовка під звіти сьомого тижня.
Console.WriteLine($"Підсумок: усього {result.TotalRows}, прийнято {result.Items.Count}, " +
                  $"пропущено {result.Errors.Count} ({result.ErrorRate:F1} % помилок)");
Console.WriteLine($"Звіт про пропущені рядки: {skippedReportPath}");

return 0;

// Composition root імпорту і розмежування результатів: скасування, таймаут, відхилені
// дані, відсутній файл — кожен зі своїм повідомленням замість стека виклику.
static async Task<int> RunImportAsync(string[] args, string dataFile)
{
    IOrderStore store = args.Contains("--file", StringComparer.OrdinalIgnoreCase)
        ? new FileOrderStore(dataFile)
        : new InMemoryOrderStore(SampleData.Orders());
    var service = new OrderService(store, TimeProvider.System);
    Console.WriteLine($"Сховище: {store}");

    using var cts = new CancellationTokenSource();
    // e.Cancel = true: процес не обривається, а імпорт зупиняється кооперативно через токен.
    ConsoleCancelEventHandler onCancel = (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };
    Console.CancelKeyPress += onCancel;
    // Для демонстрації таймауту без прапорця: cts.CancelAfter(TimeSpan.FromSeconds(2));

    try
    {
        return await ImportCommand.RunAsync(service, args, cts.Token);
    }
    // Порядок важливий: скасування — очікуваний результат, а не збій, тому раніше за загальні обробники.
    catch (OperationCanceledException)
    {
        Console.WriteLine("Імпорт скасовано.");
        return 2;
    }
    catch (TimeoutException ex)
    {
        Console.WriteLine($"Імпорт скасовано: спрацював таймаут. {ex.Message}");
        return 2;
    }
    catch (DomainException ex)
    {
        Console.WriteLine($"Дані відхилено: {ex.Message}");
        return 1;
    }
    catch (FileNotFoundException ex)
    {
        Console.WriteLine($"Файл не знайдено: {ex.FileName}");
        return 1;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Помилка введення-виведення: {ex.Message}");
        return 1;
    }
    finally
    {
        Console.CancelKeyPress -= onCancel;   // після Dispose токена Ctrl+C більше не звертається до cts
    }
}

// Журнали — справа Cli: Core не знає ні про файл, ні про каталог logs.
static string PrepareLogPath(string fileName)
{
    string logPath = Path.Combine(AppContext.BaseDirectory, "logs", fileName);
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    return logPath;
}

static int PrintMixed(MixedImportResult result, string path)
{
    Console.WriteLine($"Файл: {Path.GetFullPath(path)}");
    Console.WriteLine($"Завантажено товарів: {result.Products.Count}, клієнтів: {result.Customers.Count}");

    foreach (ProductDto product in result.Products.Take(PreviewRows))
        Console.WriteLine($"  товар   {product.Id,-6} {product.Name,-32} {product.Price,10:F2} грн");

    foreach (CustomerDto customer in result.Customers.Take(PreviewRows))
        Console.WriteLine($"  клієнт  {customer.Id,-6} {customer.Name,-32} {customer.Email}");

    if (result.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
        foreach (string error in result.Errors)
            Console.WriteLine($"  ! {error}");
    }

    Console.WriteLine($"Підсумок: усього {result.TotalRows}, прийнято {result.Accepted}, " +
                      $"пропущено {result.Errors.Count} ({result.ErrorRate:F1} % помилок)");

    return 0;
}

static void PrintEnvironment(string[] args)
{
    EnvironmentReport report = EnvironmentInfo.Collect();

    if (args.Contains("--json", StringComparer.OrdinalIgnoreCase))
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        Console.WriteLine(JsonSerializer.Serialize(report, options));
        return;
    }

    Console.WriteLine("CrossApp – інформація про середовище");
    Console.WriteLine("Студент: Грабань Олекса Андрійович, група ФЕІ-32");
    Console.WriteLine(new string('-', SeparatorWidth));

    PrintRow("ОС", report.OsDescription);
    PrintRow("ОС (Environment)", report.OsVersion);
    PrintRow("Runtime", report.FrameworkDescription);
    PrintRow("Версія CLR", report.ClrVersion);
    PrintRow("Архітектура", report.ProcessArchitecture);
    PrintRow("RID (визначено)", report.DetectedRid);
    PrintRow("RID (від .NET)", report.ReportedRid);
    PrintRow("Каталог", report.BaseDirectory);
    PrintRow("Поточний каталог", report.CurrentDirectory);
    PrintRow("TFM бібліотеки Core", report.BuildNote);

    Console.WriteLine(new string('-', SeparatorWidth));
    Console.WriteLine("Предметна область: Замовлення (клієнти, товари, замовлення, рядки замовлення)");

    static void PrintRow(string label, string value) =>
        Console.WriteLine($"{label.PadRight(LabelWidth)} : {value}");
}
