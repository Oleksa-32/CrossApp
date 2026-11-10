using Core.Domain;
using Core.Storage;

namespace Cli;

/// <summary>
/// Підкоманди лабораторної 9: config, export, import. Шлях до файлу сховища приходить
/// з конфігурації; формат JSON і атомарний запис — у Core (<see cref="OrderJsonFile"/>).
/// </summary>
/// <remarks>
/// config                — звідки взялися налаштування і куди піде файл даних
/// export [шлях]         — уся колекція у файл даних (або додатково в довільний файл)
/// import                — колекція з файлу даних; файлу немає — колекція порожня, 0 записів
/// import --sample       — демо-замовлення (samples/orders-import.json біля Cli.dll) у сховище
/// import шлях           — замовлення з довільного JSON-файлу у сховище; наявні id пропускаються
/// </remarks>
internal static class ExchangeCommands
{
    public static readonly string[] Names = ["config", "export", "import"];
    private const int PreviewRows = 5;

    public static bool IsCommand(string? name) =>
        name is not null && Names.Contains(name, StringComparer.OrdinalIgnoreCase);

    public static int Run(string command, string[] args, AppConfiguration config, AppLog log)
    {
        // Позиційний аргумент після назви команди — шлях до файлу (export / import).
        string? path = args.Where(argument => !argument.StartsWith("--", StringComparison.Ordinal)).Skip(1).FirstOrDefault();

        PrintHeader(config, log);
        try
        {
            return command.ToLowerInvariant() switch
            {
                "export" => Export(config, path),
                "import" when path is not null => Import(config, path),
                "import" when args.Contains("--sample", StringComparer.OrdinalIgnoreCase) =>
                    Import(config, Samples.PathOf("orders-import.json")),
                "import" => Load(config),
                _ => PrintConfig(config)
            };
        }
        // Пошкоджений JSON, порушений інваріант, відсутній файл чи заборонений каталог —
        // зрозуміле повідомлення і код 1 замість необробленого стека виклику.
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"Дані відхилено: {ex.Message}");
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Файл не знайдено: {ex.FileName}");
            return 1;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Помилка доступу до файлу: {ex.Message}");
            return 1;
        }
    }

    // Експорт: поточна колекція сховища записується у файл даних. Порожнє сховище дає
    // порожній масив [] — дані нізвідки не підставляються.
    private static int Export(AppConfiguration config, string? target)
    {
        IReadOnlyList<Order> orders = new FileOrderStore(config.DataFile).List();
        OrderJsonFile.Write(config.DataFile, orders);

        if (target is not null)
        {
            // Шлях з аргументу — відносно поточного каталогу, як у будь-якої консольної утиліти.
            string fullTarget = Path.GetFullPath(target);
            OrderJsonFile.Write(fullTarget, orders);
            Console.WriteLine($"Копія експорту : {fullTarget}");
        }

        Console.WriteLine($"Експортовано {orders.Count} {Records(orders.Count)}.");
        PrintOrders(orders, config.Currency);
        return 0;
    }

    // Імпорт колекції з файлу даних. Видалений файл чи каталог data/ — не помилка, а порожня
    // колекція: FileOrderStore без файлу повертає порожній список, каталог уже створено конфігурацією.
    private static int Load(AppConfiguration config)
    {
        if (!File.Exists(config.DataFile))
            Console.WriteLine("Файл даних відсутній — колекція порожня.");

        IReadOnlyList<Order> orders = new FileOrderStore(config.DataFile).List();
        Console.WriteLine($"Імпортовано {orders.Count} {Records(orders.Count)}.");
        PrintOrders(orders, config.Currency);
        return 0;
    }

    private static int Import(AppConfiguration config, string source)
    {
        // Шлях з аргументу — відносно поточного каталогу, як у будь-якої консольної утиліти.
        string fullSource = Path.GetFullPath(source);
        Console.WriteLine($"Джерело       : {fullSource}");
        if (!File.Exists(fullSource))
        {
            Console.WriteLine($"Файл не знайдено: {fullSource}");
            return 1;
        }

        IReadOnlyList<Order> incoming = OrderJsonFile.Read(fullSource);
        var store = new FileOrderStore(config.DataFile);
        int added = 0;
        foreach (Order order in incoming)
        {
            if (store.GetById(order.Id) is not null)
                continue;   // імпорт ідемпотентний: повторний запуск нічого не дублює

            store.Add(order);
            added++;
        }

        Console.WriteLine($"Прочитано {incoming.Count} {Records(incoming.Count)}, імпортовано {added}, " +
                          $"пропущено (id уже є) {incoming.Count - added}.");
        Console.WriteLine($"У сховищі тепер {store.List().Count} {Records(store.List().Count)}.");
        return 0;
    }

    private static int PrintConfig(AppConfiguration config)
    {
        Row("EnvironmentName", config.Options.EnvironmentName);
        Row("Logging:MinimumLevel", config.Options.Logging.MinimumLevel.ToString());
        Row("Storage:Location", config.Storage.Location.ToString());
        Row("Storage:DataPath", config.Storage.DataPath +
                                          (config.DataPathOverride is null ? "" : $"  (з {AppConfiguration.DataPathVariable})"));
        Row("Storage:FileName", config.Storage.FileName);
        Row("Orders:Currency", config.Currency);
        Row("user-secrets", AppConfiguration.UserSecretsFile() ?? "UserSecretsId не задано");
        return 0;

        static void Row(string key, string value) => Console.WriteLine($"  {key,-22} = {value}");
    }

    private static void PrintHeader(AppConfiguration config, AppLog log)
    {
        log.Debug($"Середовище: {config.Environment} ({AppConfiguration.EnvironmentVariable})");
        log.Debug($"Поточний каталог : {Environment.CurrentDirectory}");
        log.Debug($"Каталог застосунку: {AppContext.BaseDirectory}");
        Console.WriteLine($"Файл даних : {config.DataFile}");
        Console.WriteLine($"ApiKey: {config.DescribeApiKey()}");
    }

    private static void PrintOrders(IReadOnlyList<Order> orders, string currency)
    {
        foreach (Order order in orders.Take(PreviewRows))
            Console.WriteLine($"  {order.Id,-6} {order.CustomerId,-6} {order.Status,-10} " +
                              $"рядків {order.Lines.Count}, сума {order.Total,10:F2} {currency}");
        if (orders.Count > PreviewRows)
            Console.WriteLine($"  … ще {orders.Count - PreviewRows}");
    }

    // Українська форма іменника після числа: 1 запис, 3 записи, 5 записів, 11 записів.
    private static string Records(int count) => (count % 100, count % 10) switch
    {
        ( >= 11 and <= 14, _) => "записів",
        (_, 1) => "запис",
        (_, >= 2 and <= 4) => "записи",
        _ => "записів"
    };
}
