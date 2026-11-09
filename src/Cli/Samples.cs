namespace Cli;

/// <summary>
/// Вхідні приклади з каталогу data/ кореня solution. Збірка копіює їх у bin/.../samples
/// (див. Cli.csproj), тому типовий шлях будується від AppContext.BaseDirectory і не залежить
/// від того, з якого каталогу запущено процес. Шлях, переданий аргументом, лишається відносним
/// до поточного каталогу — так поводиться будь-яка консольна утиліта.
/// </summary>
internal static class Samples
{
    public static string PathOf(string fileName) => Path.Combine(AppContext.BaseDirectory, "samples", fileName);
}
