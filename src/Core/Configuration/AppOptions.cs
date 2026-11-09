namespace Core.Configuration;

/// <summary>
/// Налаштування застосунку у вигляді звичайних класів (лабораторна 9). Core не знає, звідки
/// прийшли значення — з appsettings.json, змінної середовища чи командного рядка: розкладає
/// IConfiguration у ці класи точка входу (<c>config.Get&lt;AppOptions&gt;()</c> у Cli).
/// Значення за замовчуванням тут — ті, що діють, коли ключа немає в жодному джерелі.
/// </summary>
public sealed class AppOptions
{
    public string EnvironmentName { get; init; } = "Development";
    public StorageOptions Storage { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public OrdersOptions Orders { get; init; } = new();
}

/// <summary>Від чого рахується відносний <see cref="StorageOptions.DataPath"/>.</summary>
public enum StorageLocation
{
    /// <summary>Каталог застосунку (AppContext.BaseDirectory) — профіль Development.</summary>
    App,

    /// <summary>Системний каталог локальних даних користувача
    /// (SpecialFolder.LocalApplicationData) — профіль Production: каталог bin/ можна
    /// перезаписати новою збіркою, а дані користувача мають пережити оновлення.</summary>
    LocalAppData
}

/// <summary>Розділ Storage: де лежить файл сховища замовлень.</summary>
public sealed class StorageOptions
{
    public StorageLocation Location { get; init; } = StorageLocation.App;
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "orders.json";

    /// <summary>
    /// Базовий каталог для відносного DataPath. Каталог застосунку передає викликач:
    /// Core не вирішує за точку входу, звідки її запущено.
    /// </summary>
    public string ResolveBaseDirectory(string appBaseDirectory) => Location switch
    {
        StorageLocation.LocalAppData => Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
        _ => appBaseDirectory
    };

    /// <summary>
    /// Повний шлях до файлу даних. Лише Path.Combine — жодної конкатенації з "\" чи "/":
    /// роздільник підставляє платформа. Абсолютний DataPath (наприклад, із CROSSAPP_DATA_PATH)
    /// використовується як є; відносний рахується від baseDir, а не від поточного каталогу процесу.
    /// Каталог створюється одразу, тому видалений data/ не призводить до винятку.
    /// </summary>
    public string ResolveFullPath(string baseDir)
    {
        if (string.IsNullOrWhiteSpace(FileName))
            throw new InvalidOperationException("Storage:FileName не задано в конфігурації.");

        string root = Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);
        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}

/// <summary>Рівні журналу від найдетальнішого до найстислішого (імена — як у Microsoft.Extensions.Logging).</summary>
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error
}

/// <summary>Розділ Logging: мінімальний рівень повідомлень, які виводить застосунок.</summary>
public sealed class LoggingOptions
{
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;
}

/// <summary>Розділ варіанта «Замовлення»: валюта, у якій друкуються суми.</summary>
public sealed class OrdersOptions
{
    public string Currency { get; init; } = "UAH";
}
