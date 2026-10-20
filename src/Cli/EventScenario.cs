using Core.Domain;
using Core.Events;
using Core.Services;

namespace Cli;

/// <summary>
/// Сценарій лабораторної 6: два підписники на OrderService.Changed, відмова без
/// події, відписка і її видимий ефект, пошук через Func. Як саме обробити
/// сповіщення (консоль + файл), вирішує цей клас, а не Core.
/// </summary>
internal static class EventScenario
{
    private const string MissingId = "O-404";
    private const decimal SmallOrderThreshold = 2000m;

    public static int Run(OrderService service, IReadOnlyList<Product> catalog, string logPath)
    {
        Product laptop = catalog[0];
        Product mouse = catalog[1];

        ChangeLog.TryAppend(logPath, $"--- запуск {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} ---");

        int changes = 0;
        int changesSeenByJournal = 0;

        // Підписник 1 — журнал. Локальна функція має стале ім'я, тому той самий
        // делегат можна передати і в +=, і в -=. Помилку запису у файл перехоплює
        // TryAppend: виняток у підписнику не повинен ламати операцію сервісу.
        void OnOrderChanged(object? sender, OrderChangedEventArgs e)
        {
            changesSeenByJournal = changes;
            string line = ChangeLog.Format(e);
            Console.WriteLine(line);
            ChangeLog.TryAppend(logPath, line);
        }

        Console.WriteLine($"Журнал: {logPath}");
        Console.WriteLine();
        Console.WriteLine("=== Підписка: журнал (локальна функція), потім лічильник (лямбда) ===");
        service.Changed += OnOrderChanged;
        service.Changed += (_, _) => changes++;   // підписник 2: замикання над changes

        Order created = service.CreateOrder("C-012");
        service.AddLine(created.Id, laptop, 1);
        service.AddLine(created.Id, mouse, 2);
        Console.WriteLine($"Подій отримано: {changes}");
        Console.WriteLine($"Порядок: журнал під час останньої події бачив лічильник = {changesSeenByJournal}, " +
                          "лямбда збільшила його вже після журналу");

        Console.WriteLine();
        Console.WriteLine("=== Сценарій відмови: виняток є, рядка події немає ===");
        int changesBeforeFailures = changes;
        OrderScenario.TryDo("AddLine з нульовою кількістю", () => service.AddLine(created.Id, mouse, 0));
        OrderScenario.TryDo($"AddLine(\"{MissingId}\", ...)", () => service.AddLine(MissingId, mouse, 1));
        OrderScenario.TryDo($"Confirm(\"{MissingId}\")", () => service.Confirm(MissingId));
        Console.WriteLine($"Подій отримано: {changes} (нових після відмов: {changes - changesBeforeFailures})");

        Console.WriteLine();
        Console.WriteLine("=== Відписка: service.Changed -= OnOrderChanged ===");
        service.Changed -= OnOrderChanged;
        service.Confirm(created.Id);              // операція виконується, журнал мовчить
        Console.WriteLine($"Подій отримано: {changes} (+1 лише від лямбди-лічильника)");
        Console.WriteLine($"Дані змінилися: {service.Find(created.Id)}");

        // Нова лямбда з тим самим текстом — інший екземпляр делегата: підписку не знято.
        service.Changed -= (_, _) => changes++;
        service.Cancel(created.Id);
        Console.WriteLine($"Після «-= (_, _) => changes++» і Cancel: {changes} — лічильник досі підписаний");

        Console.WriteLine();
        Console.WriteLine($"=== Func замість події: Search(order => order.Total < {SmallOrderThreshold:F0}) ===");
        IReadOnlyList<Order> small = service.Search(order => order.Total < SmallOrderThreshold);
        foreach (Order order in small)
            Console.WriteLine($"  {order}");
        Console.WriteLine($"Знайдено: {small.Count}");

        return 0;
    }
}
