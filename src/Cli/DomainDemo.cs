using Core.Domain;
using Core.Dto;
using Core.Import;

namespace Cli;

/// <summary>
/// Демонстрація доменної моделі (лабораторна 4). Тут лише виклики доменних
/// операцій і вивід: жодної перевірки правил — їх виконують самі сутності.
/// </summary>
internal static class DomainDemo
{
    // Фіксована дата замість DateTime.Now: вивід однаковий за кожного запуску.
    private static readonly DateTime OrderDate = new(2026, 10, 5, 10, 30, 0);

    public static int Run(string catalogPath)
    {
        Console.WriteLine("=== Сценарій 1: успіх ===");

        Product laptop = Product.Create("p-001", "Ноутбук Lenovo IdeaPad 15", 24999.00m);
        Product mouse = Product.Create("P-002", "Миша бездротова Logitech M330", 899.50m);
        Console.WriteLine($"Товари: {laptop}; {mouse}");

        Order order = Order.Create("o-001", "c-001", OrderDate);
        order.AddLine(laptop, 1);
        order.AddLine(mouse, 2);
        order.AddLine(mouse, 1); // той самий товар за тією самою ціною — кількість у наявному рядку
        PrintOrder(order);

        // Ціна в каталозі змінилася, а в уже доданому рядку — ні: там ціна на момент продажу.
        mouse.ChangePrice(799.00m);
        order.Confirm();
        Console.WriteLine($"Після ChangePrice(799.00) і Confirm(): {mouse}");
        PrintOrder(order);

        Order cancelled = Order.Create("O-003", "C-002", OrderDate);
        cancelled.AddLine(mouse, 1);
        cancelled.Cancel();
        Console.WriteLine(cancelled);

        OrderDto dto = order.ToDto();
        Order restored = Order.FromDto(dto);
        Console.WriteLine($"ToDto → FromDto: {restored}");

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

        Order draft = Order.Create("O-002", "C-002", OrderDate);

        TryDo("порожній номер замовлення", () => Order.Create("   ", "C-001", OrderDate));
        TryDo("замовлення без клієнта", () => Order.Create("O-009", "", OrderDate));
        TryDo("від'ємна ціна товару", () => Product.Create("P-003", "Клавіатура Keychron K2", -4250m));
        TryDo("пробіл в ідентифікаторі товару", () => Product.Create("P 004", "Монітор Dell P2422H", 7890.90m));
        TryDo("нульова кількість у рядку", () => draft.AddLine(mouse, 0));
        TryDo("підтвердження порожнього замовлення", () => draft.Confirm());
        TryDo("рядок у підтверджене замовлення", () => order.AddLine(laptop, 1));
        TryDo("повторне підтвердження", () => order.Confirm());
        TryDo("підтвердження скасованого", () => cancelled.Confirm());
        TryDo("FromDto з нульовою кількістю", () =>
            Order.FromDto(dto with { Lines = [dto.Lines[0] with { Quantity = 0 }] }));
        TryDo("FromDto з невідомим станом", () => Order.FromDto(dto with { Status = "Shipped" }));
        TryDo("FromDto підтвердженого без рядків", () => Order.FromDto(dto with { Lines = [] }));

        Console.WriteLine("Стан після всіх відмов:");
        Console.WriteLine($"  {order}");
        Console.WriteLine($"  {draft}");

        Console.WriteLine();
        Console.WriteLine("=== Додатково: каталог із файлу через ті самі інваріанти ===");
        PrintCatalog(ProductCatalog.FromImport(ProductCsvImporter.Load(catalogPath)), catalogPath);

        return 0;
    }

    private static void PrintOrder(Order order)
    {
        Console.WriteLine(order);
        foreach (OrderLine line in order.Lines)
            Console.WriteLine($"  {line}");
    }

    private static void PrintCatalog(ImportResult<Product> catalog, string path)
    {
        Console.WriteLine($"Файл: {path}");
        Console.WriteLine($"Сутностей створено: {catalog.Items.Count}");
        foreach (Product product in catalog.Items)
            Console.WriteLine($"  {product}");

        Console.WriteLine($"Відхилено: {catalog.Errors.Count}");
        foreach (string error in catalog.Errors)
            Console.WriteLine($"  ! {error.ReplaceLineEndings(" ")}");
    }

    // Один обробник на всі сценарії відмови: на екран — тип і повідомлення, без стека.
    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
        }
        catch (Exception ex)
        {
            // ArgumentOutOfRangeException додає «Actual value was …» з нового рядка.
            Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message.ReplaceLineEndings(" ")}");
        }
    }
}
