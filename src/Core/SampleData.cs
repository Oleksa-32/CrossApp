using Core.Domain;
using Core.Dto;

namespace Core;

/// <summary>
/// Початкові дані для сховища в пам'яті: демо п'ятого тижня і матеріал для
/// звітів сьомого. Усе створюється через Product.Create і Order.Create/AddLine/
/// Confirm/Cancel, тому зразки проходять ті самі інваріанти, що й реальні дані.
/// Для звітів дані різноманітні: три стани, три місяці, сім клієнтів, суми від
/// порожніх замовлень до десятків тисяч гривень.
/// </summary>
public static class SampleData
{
    // Фіксована точка відліку: вивід однаковий за кожного запуску.
    private static readonly DateTime FirstOrderDate = new(2026, 9, 1, 9, 0, 0);

    public static IReadOnlyList<Product> Products() =>
    [
        Product.Create("P-001", "Ноутбук Lenovo IdeaPad 15", 24999.00m),
        Product.Create("P-002", "Миша бездротова Logitech M330", 899.50m),
        Product.Create("P-003", "Клавіатура механічна Keychron K2", 4250.00m),
        Product.Create("P-004", "Монітор Dell P2422H 24\"", 7890.90m),
        Product.Create("P-005", "SSD Samsung 980 1TB", 3199.99m),
        Product.Create("P-006", "Навушники Sony WH-1000XM4", 11499.00m),
        Product.Create("P-007", "Веб-камера Logitech C920", 2450.00m),
        Product.Create("P-008", "Кабель USB-C 2 м", 249.90m),
        Product.Create("P-009", "Док-станція Anker 7-in-1", 3780.00m),
        Product.Create("P-010", "Сумка для ноутбука 15.6\"", 1150.00m)
    ];

    /// <summary>
    /// Довідник клієнтів для Join / GroupJoin (тиждень 7, додаткове завдання 1).
    /// C-008 навмисно без замовлень: GroupJoin його покаже, Join — ні.
    /// </summary>
    public static IReadOnlyList<CustomerDto> Customers() =>
    [
        new("C-001", "Іван Петренко", "ivan.petrenko@example.com", "+380671234567"),
        new("C-002", "Олена Коваль", "olena.koval@example.com"),
        new("C-003", "Андрій Шевчук", "andrii.shevchuk@example.com", "+380501112233"),
        new("C-004", "Марія Бондар", "maria.bondar@example.com"),
        new("C-005", "Тарас Мельник", "taras.melnyk@example.com", "+380939876543"),
        new("C-006", "Софія Ткаченко", "sofiia.tkachenko@example.com"),
        new("C-007", "Дмитро Кравець", "dmytro.kravets@example.com"),
        new("C-008", "Ірина Савчук", "iryna.savchuk@example.com", "+380661234000")
    ];

    /// <summary>20 замовлень семи клієнтів у різних станах, вересень — листопад.</summary>
    public static IReadOnlyList<Order> Orders()
    {
        Dictionary<string, Product> products = Products().ToDictionary(product => product.Id);

        // Кожен рядок таблиці: клієнт, стан, рядки як «товар×кількість».
        (string Customer, OrderStatus Status, string Lines)[] rows =
        [
            ("C-001", OrderStatus.Confirmed, "P-001×1 P-002×1 P-010×1"),
            ("C-002", OrderStatus.Confirmed, "P-006×1"),
            ("C-003", OrderStatus.Draft,     "P-003×1 P-008×2"),
            ("C-001", OrderStatus.Confirmed, "P-004×2 P-009×1"),
            ("C-004", OrderStatus.Cancelled, "P-005×1"),
            ("C-005", OrderStatus.Confirmed, "P-007×1 P-008×3"),
            ("C-002", OrderStatus.Draft,     "P-002×2"),
            ("C-006", OrderStatus.Confirmed, "P-001×2 P-005×2"),
            ("C-003", OrderStatus.Confirmed, "P-006×1 P-008×1"),
            ("C-004", OrderStatus.Draft,     ""),
            ("C-005", OrderStatus.Cancelled, "P-004×1"),
            ("C-001", OrderStatus.Confirmed, "P-003×1 P-002×1"),
            ("C-006", OrderStatus.Confirmed, "P-009×1 P-007×1 P-008×2"),
            ("C-002", OrderStatus.Confirmed, "P-010×2"),
            ("C-003", OrderStatus.Cancelled, ""),
            ("C-004", OrderStatus.Confirmed, "P-005×1 P-008×1"),
            ("C-007", OrderStatus.Confirmed, "P-006×2 P-003×1"),
            ("C-005", OrderStatus.Draft,     "P-008×1"),
            ("C-002", OrderStatus.Confirmed, "P-001×1 P-004×1"),
            ("C-006", OrderStatus.Cancelled, "P-002×1")
        ];

        var orders = new List<Order>(rows.Length);
        for (int i = 0; i < rows.Length; i++)
        {
            (string customer, OrderStatus status, string lines) = rows[i];
            // Крок 4 дні: замовлення розподіляються на три місяці (8 / 8 / 4) — звіту за місяцями є що групувати.
            Order order = Order.Create($"O-{i + 1:D3}", customer, FirstOrderDate.AddDays(i * 4).AddHours(i % 5));

            foreach (string line in lines.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = line.Split('×');
                order.AddLine(products[parts[0]], int.Parse(parts[1]));
            }

            if (status == OrderStatus.Confirmed)
                order.Confirm();
            else if (status == OrderStatus.Cancelled)
                order.Cancel();

            orders.Add(order);
        }

        return orders;
    }

    /// <summary>
    /// Великий набір для вимірювань Stopwatch (додаткове завдання 4): <paramref name="count"/>
    /// замовлень із випадковими клієнтами, рядками і станами. Фіксоване зерно Random дає
    /// той самий набір за кожного запуску, тож час різних варіантів звіту можна порівнювати.
    /// </summary>
    public static IReadOnlyList<Order> Generate(int count, int seed = 2026)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        const int CustomerCount = 500;
        const int MaxLines = 4;
        const int MaxQuantity = 3;

        var random = new Random(seed);
        IReadOnlyList<Product> products = Products();
        var orders = new List<Order>(count);

        for (int i = 0; i < count; i++)
        {
            Order order = Order.Create($"G-{i + 1:D6}", $"C-{random.Next(1, CustomerCount + 1):D4}",
                FirstOrderDate.AddMinutes(i * 7));

            int lines = random.Next(1, MaxLines + 1);
            for (int line = 0; line < lines; line++)
                order.AddLine(products[random.Next(products.Count)], random.Next(1, MaxQuantity + 1));

            // Приблизно 70 % підтверджених, 15 % скасованих, решта — чернетки.
            int roll = random.Next(100);
            if (roll < 70)
                order.Confirm();
            else if (roll < 85)
                order.Cancel();

            orders.Add(order);
        }

        return orders;
    }
}
