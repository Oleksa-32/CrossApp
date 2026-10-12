using Core.Domain;

namespace Core;

/// <summary>
/// Початкові дані для сховища в пам'яті: демо п'ятого тижня і матеріал для
/// звітів сьомого. Усе створюється через Product.Create і Order.Create/AddLine/
/// Confirm/Cancel, тому зразки проходять ті самі інваріанти, що й реальні дані.
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

    /// <summary>16 замовлень шести клієнтів у різних станах.</summary>
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
            ("C-004", OrderStatus.Confirmed, "P-005×1 P-008×1")
        ];

        var orders = new List<Order>(rows.Length);
        for (int i = 0; i < rows.Length; i++)
        {
            (string customer, OrderStatus status, string lines) = rows[i];
            Order order = Order.Create($"O-{i + 1:D3}", customer, FirstOrderDate.AddDays(i * 2).AddHours(i % 5));

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
}
