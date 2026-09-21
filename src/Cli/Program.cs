using System.Text;
using System.Text.Json;
using Core;
using Core.Dto;
using Core.Import;

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

// Шлях приходить аргументом; типове значення будується через Path.Combine,
// щоб не зашивати роздільник каталогів конкретної ОС.
string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

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
