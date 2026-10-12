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

// Режим змішаного файлу: рядки різних типів розрізняються префіксом у першій колонці.
bool mixed = args.Contains("--mixed", StringComparer.OrdinalIgnoreCase);

// Режим лабораторної 4: доменна модель, сценарії «успіх» і «порушення інваріантів».
bool domain = args.Contains("--domain", StringComparer.OrdinalIgnoreCase);

// Шлях приходить аргументом; типове значення будується через Path.Combine,
// щоб не зашивати роздільник каталогів конкретної ОС.
string[] paths = args.Where(argument => !argument.StartsWith("--")).ToArray();

// Імпорт лабораторної 3 — за прапорцем --import або коли передано шлях до файлу.
bool import = args.Contains("--import", StringComparer.OrdinalIgnoreCase) || paths.Length > 0;

// Типовий режим (лабораторна 5). Composition root: єдине місце, де названо
// конкретні класи сховищ; далі сервіс і сценарій бачать лише IOrderStore.
if (!mixed && !domain && !import)
{
    bool useFile = args.Contains("--file", StringComparer.OrdinalIgnoreCase);
    string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

    IOrderStore store = useFile
        ? new FileOrderStore(dataPath)
        : new InMemoryOrderStore(SampleData.Orders());
    if (args.Contains("--cache", StringComparer.OrdinalIgnoreCase))
        store = new CachingOrderStore(store);
    var service = new OrderService(store, TimeProvider.System);

    Console.WriteLine($"Сховище: {store}");
    try
    {
        int exitCode = OrderScenario.Run(service, store, SampleData.Products());
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
string path = paths.Length > 0 ? paths[0] : Path.Combine("data", defaultFile);

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

if (domain)
    return DomainDemo.Run(path);

if (mixed)
    return PrintMixed(MixedCsvImporter.Load(path), path);

// Формат обирається за розширенням — знову switch expression з константними патернами.
ImportResult<ProductDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
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

return 0;

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
