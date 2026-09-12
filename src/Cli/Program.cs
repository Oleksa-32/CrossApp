using System.Text;
using System.Text.Json;
using Core;

Console.OutputEncoding = Encoding.UTF8;

const int SeparatorWidth = 52;
const int LabelWidth = 22;

EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json", StringComparer.OrdinalIgnoreCase))
{
    PrintJson(report);
}
else
{
    PrintTable(report);
}

static void PrintJson(EnvironmentReport report)
{
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    Console.WriteLine(JsonSerializer.Serialize(report, options));
}

static void PrintTable(EnvironmentReport report)
{
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
}

static void PrintRow(string label, string value) =>
    Console.WriteLine($"{label.PadRight(LabelWidth)} : {value}");
