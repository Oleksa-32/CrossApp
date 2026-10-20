using Core.Abstractions;
using Core.Domain;
using Core.Services;

namespace Cli;

/// <summary>
/// Додаткове завдання 1: ті самі операції, що й у EventScenario, але сповіщення
/// отримує INotificationSink, переданий сервісу через конструктор. Підписок,
/// += і -= тут немає: одержувач один і заданий під час створення сервісу.
/// </summary>
internal static class SinkScenario
{
    public static int Run(OrderService service, INotificationSink sink, IReadOnlyList<Product> catalog)
    {
        Product laptop = catalog[0];
        Product mouse = catalog[1];

        Console.WriteLine($"Одержувач сповіщень (через конструктор): {sink}");
        Console.WriteLine();

        Console.WriteLine("=== Операції ===");
        Order created = service.CreateOrder("C-013");
        service.AddLine(created.Id, laptop, 1);
        service.AddLine(created.Id, mouse, 2);
        service.Confirm(created.Id);

        Console.WriteLine();
        Console.WriteLine("=== Сценарій відмови: виняток є, сповіщення немає ===");
        OrderScenario.TryDo("AddLine у підтверджене замовлення", () => service.AddLine(created.Id, mouse, 1));

        Console.WriteLine();
        Console.WriteLine($"Підсумок: {service.Find(created.Id)}");
        return 0;
    }
}
