using Core.Abstractions;
using Core.Domain;
using Core.Services;

namespace Cli;

/// <summary>
/// Сценарій лабораторної 5: додати, змінити, показати список, знайти за id,
/// і сценарій відмови. Отримує лише абстракції — яке саме сховище під ними,
/// вирішив composition root у Program.cs.
/// </summary>
internal static class OrderScenario
{
    private const decimal LargeOrderThreshold = 10000m;

    public static int Run(OrderService service, IOrderStore store, IReadOnlyList<Product> catalog)
    {
        Product laptop = catalog[0];
        Product mouse = catalog[1];

        Console.WriteLine($"Замовлень на старті: {service.All().Count}");
        Console.WriteLine();

        Console.WriteLine("=== Додати і змінити ===");
        Order created = service.CreateOrder("C-007");
        Console.WriteLine($"CreateOrder(\"C-007\") → {created.Id}");
        service.AddLine(created.Id, laptop, 1);
        service.AddLine(created.Id, mouse, 2);
        service.AddLine(created.Id, mouse, 1);   // той самий товар — кількість у наявному рядку
        service.Confirm(created.Id);
        Console.WriteLine("AddLine ×3, Confirm:");
        PrintOrder(created);

        Console.WriteLine();
        Console.WriteLine("=== Знайти за id ===");
        string lowerId = created.Id.ToLowerInvariant();   // ключ без урахування регістру
        const string missingId = "O-404";
        Console.WriteLine($"Find(\"{lowerId}\") → {service.Find(lowerId)?.ToString() ?? "не знайдено"}");
        Console.WriteLine($"Find(\"{missingId}\") → " +
                          $"{service.Find(missingId)?.ToString() ?? "не знайдено (null — нормальний результат пошуку)"}");

        Console.WriteLine();
        Console.WriteLine("=== Список ===");
        IReadOnlyList<Order> all = service.All();
        foreach (Order order in all)
            Console.WriteLine($"  {order}");
        Console.WriteLine($"Усього: {all.Count}");

        Console.WriteLine();
        Console.WriteLine($"=== Пошук: підтверджені на суму понад {LargeOrderThreshold:F0} грн ===");
        IReadOnlyList<Order> large = service.Search(order =>
            order.Status == OrderStatus.Confirmed && order.Total > LargeOrderThreshold);
        foreach (Order order in large)
            Console.WriteLine($"  {order}");
        Console.WriteLine($"Знайдено: {large.Count}");

        Console.WriteLine();
        Console.WriteLine("=== Сценарій відмови ===");
        TryDo($"Confirm(\"{missingId}\")", () => service.Confirm(missingId));
        TryDo($"AddLine(\"{missingId}\", ...)", () => service.AddLine(missingId, mouse, 1));
        TryDo("AddLine у підтверджене замовлення", () => service.AddLine(created.Id, laptop, 1));
        TryDo("AddLine з нульовою кількістю", () => service.AddLine(created.Id, mouse, 0));
        TryDo("CreateOrder без клієнта", () => service.CreateOrder(" "));
        // Унікальність id — контракт сховища, а не сутності: копія з тим самим id.
        TryDo("додати дубль id у сховище", () => store.Add(Order.FromDto(created.ToDto())));

        Console.WriteLine($"Після відмов: {service.Find(created.Id)}");
        return 0;
    }

    private static void PrintOrder(Order order)
    {
        Console.WriteLine($"  {order}");
        foreach (OrderLine line in order.Lines)
            Console.WriteLine($"    {line}");
    }

    // Користувач бачить зрозумілий текст замість стека виклику.
    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {title}: виняток НЕ спрацював!");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.WriteLine($"  {title}: помилка — {ex.Message.ReplaceLineEndings(" ")}");
        }
    }
}
