using System.Runtime.InteropServices;

namespace Core;

/// <summary>
/// Дані про середовище виконання. Це record, бо тут лише результат вимірювання
/// (незмінний набір значень), а не сутність із життєвим циклом і поведінкою.
/// </summary>
public sealed record EnvironmentReport(
    string OsDescription,
    string OsVersion,
    string FrameworkDescription,
    string ClrVersion,
    string ProcessArchitecture,
    string DetectedRid,
    string ReportedRid,
    string BaseDirectory,
    string CurrentDirectory,
    string BuildNote);

/// <summary>
/// Збирає інформацію про середовище виконання. Клас, бо тут алгоритм, а не набір полів.
/// Нічого не друкує: вивід — відповідальність Cli.
/// </summary>
public static class EnvironmentInfo
{
    // Умовна компіляція: рядок обчислюється під час збірки й відрізняється для кожного TFM.
#if NET10_0_OR_GREATER
    private const string TargetFrameworkNote = "збірка під net10.0";
#else
    private const string TargetFrameworkNote = "збірка під net8.0";
#endif

    public static EnvironmentReport Collect() => new(
        RuntimeInformation.OSDescription,
        Environment.OSVersion.ToString(),
        RuntimeInformation.FrameworkDescription,
        Environment.Version.ToString(),
        RuntimeInformation.ProcessArchitecture.ToString(),
        DetectRid(),
        RuntimeInformation.RuntimeIdentifier,
        AppContext.BaseDirectory,
        Environment.CurrentDirectory,
        TargetFrameworkNote);

    // Ручне визначення RID: показує, з чого складається рядок osx-arm64.
    private static string DetectRid()
    {
        string os =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "unknown";

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "unknown"
        };

        return $"{os}-{arch}";
    }
}
