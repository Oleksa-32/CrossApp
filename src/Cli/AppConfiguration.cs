using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Cli;

/// <summary>
/// Зібрана конфігурація застосунку (лабораторна 9): сирий IConfiguration для поодиноких ключів
/// на кшталт секрету, розкладені в класи AppOptions і вже обчислений повний шлях до файлу даних.
/// </summary>
internal sealed record AppConfiguration(
    string Environment,
    IConfiguration Raw,
    AppOptions Options,
    StorageOptions Storage,
    string DataFile,
    string? DataPathOverride)
{
    public const string EnvPrefix = "CROSSAPP_";
    public const string EnvironmentVariable = EnvPrefix + "ENVIRONMENT";
    public const string DataPathVariable = EnvPrefix + "DATA_PATH";
    public const string ApiKeyKey = "Integration:ApiKey";
    private const string DefaultEnvironment = "Development";

    /// <summary>
    /// Джерела в порядку зростання пріоритету — кожне наступне перекриває попереднє:
    /// appsettings.json → appsettings.{Environment}.json → user-secrets (лише Development) → змінні CROSSAPP_* →
    /// аргументи --Розділ:Ключ=значення → проста змінна CROSSAPP_DATA_PATH (лише для шляху).
    /// </summary>
    public static AppConfiguration Build(string[] args)
    {
        string environment = System.Environment.GetEnvironmentVariable(EnvironmentVariable) is { Length: > 0 } fromEnv
            ? fromEnv
            : DefaultEnvironment;

        // Базовий каталог задано явно: appsettings*.json лежать поруч із Cli.dll, і їх буде
        // знайдено незалежно від того, з якого каталогу запущено процес.
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);

        // user-secrets — інструмент локальної розробки: файл лежить відкритим текстом у профілі
        // розробника. У Production секрет приходить змінною середовища або зі сховища секретів.
        if (IsDevelopment(environment))
            builder.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

        IConfiguration config = builder
            .AddEnvironmentVariables(prefix: EnvPrefix)
            .AddCommandLine(ConfigArguments(args))
            .Build();

        AppOptions options = config.Get<AppOptions>() ?? new AppOptions();

        // Проста змінна CROSSAPP_DATA_PATH перекриває Storage:DataPath з будь-якого джерела:
        // її зручно задати в одному рядку перед запуском, без знання структури розділів.
        string? legacy = System.Environment.GetEnvironmentVariable(DataPathVariable);
        bool overridden = !string.IsNullOrWhiteSpace(legacy);
        var storage = new StorageOptions
        {
            Location = options.Storage.Location,
            DataPath = overridden ? legacy! : options.Storage.DataPath,
            FileName = options.Storage.FileName
        };

        string baseDir = storage.ResolveBaseDirectory(AppContext.BaseDirectory);
        return new AppConfiguration(environment, config, options, storage, storage.ResolveFullPath(baseDir),
            overridden ? legacy : null);
    }

    /// <summary>Секрет читається як звичайний ключ; назовні показується лише факт наявності.</summary>
    public string? ApiKey => Raw[ApiKeyKey];

    public string DescribeApiKey() => string.IsNullOrEmpty(ApiKey)
        ? "не заданий"
        : $"присутній, довжина {ApiKey.Length}";

    public string Currency => Options.Orders.Currency;

    /// <summary>
    /// Каталог user-secrets цього проєкту. Сам файл — поза репозиторієм, у профілі користувача ОС.
    /// </summary>
    public static string? UserSecretsFile()
    {
        string? id = Assembly.GetExecutingAssembly().GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;
        return id is null
            ? null
            : PathHelper.GetSecretsPathFromSecretsId(id);
    }

    private static bool IsDevelopment(string environment) =>
        string.Equals(environment, DefaultEnvironment, StringComparison.OrdinalIgnoreCase);

    // У конфігурацію йдуть лише аргументи виду --Ключ=значення. Прапорці (--file, --events) і
    // позиційні аргументи (export, шлях до файлу) провайдер командного рядка інакше прочитав би
    // як пари «ключ значення» або як ключ, що починається з "/".
    private static string[] ConfigArguments(string[] args) =>
        args.Where(argument => argument.StartsWith("--", StringComparison.Ordinal) && argument.Contains('='))
            .ToArray();
}
