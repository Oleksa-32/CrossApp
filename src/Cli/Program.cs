using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;

const int SeparatorWidth = 52;
const int LabelWidth = 22;

var info = EnvironmentInfo.Collect();

if (args.Contains("--json", StringComparer.OrdinalIgnoreCase))
{
    PrintJson(info);
}
else
{
    PrintTable(info);
}

static void PrintJson(EnvironmentInfo info)
{
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    Console.WriteLine(JsonSerializer.Serialize(info, options));
}

static void PrintTable(EnvironmentInfo info)
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Грабань Олекса Андрійович, група ФЕІ-32");
    Console.WriteLine(new string('-', SeparatorWidth));

    PrintRow("ОС (OSDescription)", info.OsDescription);
    PrintRow("ОС (Environment)", info.OsVersion);
    PrintRow("Архітектура процесу", info.ProcessArchitecture);
    PrintRow("RID (runtime)", info.RuntimeIdentifier);
    PrintRow("Версія .NET (CLR)", info.ClrVersion);
    PrintRow("Runtime", info.Framework);
    PrintRow("Каталог застосунку", info.BaseDirectory);
    PrintRow("Поточний каталог", info.CurrentDirectory);

    Console.WriteLine(new string('-', SeparatorWidth));
    Console.WriteLine("Предметна область: Замовлення (клієнти, товари, замовлення, рядки замовлення)");
}

static void PrintRow(string label, string value) =>
    Console.WriteLine($"{label.PadRight(LabelWidth)} : {value}");

internal sealed record EnvironmentInfo(
    string OsDescription,
    string OsVersion,
    string ProcessArchitecture,
    string RuntimeIdentifier,
    string ClrVersion,
    string Framework,
    string BaseDirectory,
    string CurrentDirectory)
{
    public static EnvironmentInfo Collect() => new(
        RuntimeInformation.OSDescription,
        Environment.OSVersion.ToString(),
        RuntimeInformation.ProcessArchitecture.ToString(),
        RuntimeInformation.RuntimeIdentifier,
        Environment.Version.ToString(),
        RuntimeInformation.FrameworkDescription,
        AppContext.BaseDirectory,
        Environment.CurrentDirectory);
}
