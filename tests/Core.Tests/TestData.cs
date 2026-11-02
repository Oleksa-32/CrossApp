using Core.Domain;
using Microsoft.Extensions.Time.Testing;

namespace Core.Tests;

/// <summary>
/// Маленький фіксований набір, оголошений у проєкті тестів, а не взятий із SampleData:
/// зміна демо-даних Cli не повинна ламати тести. Кожен виклик створює нові об'єкти,
/// тож тести не ділять стану і не залежать від порядку запуску.
/// </summary>
internal static class TestData
{
    public static readonly DateTime Date = new(2026, 11, 1, 10, 0, 0);

    public static Product Laptop() => Product.Create("P-LAP", "Ноутбук", 10000m);
    public static Product Mouse() => Product.Create("P-MOU", "Миша", 2500m);
    public static Product Cable() => Product.Create("P-CAB", "Кабель", 100m);

    /// <summary>
    /// O-BIG 10000 і O-MID / O-TIE по 5000 — підтверджені; O-SMALL 100 — чернетка;
    /// O-EMPTY 0 — скасоване. Дві однакові суми перевіряють стабільний порядок топ-N.
    /// </summary>
    public static IReadOnlyList<Order> Orders() =>
    [
        Build("O-SMALL", "C-1", OrderStatus.Draft, (Cable(), 1)),
        Build("O-TIE", "C-2", OrderStatus.Confirmed, (Mouse(), 2)),
        Build("O-BIG", "C-1", OrderStatus.Confirmed, (Laptop(), 1)),
        Build("O-EMPTY", "C-3", OrderStatus.Cancelled),
        Build("O-MID", "C-3", OrderStatus.Confirmed, (Mouse(), 2))
    ];

    /// <summary>Фіксований час: подія і створення замовлення отримують наперед відому дату.</summary>
    public static FakeTimeProvider Clock() => new(new DateTimeOffset(Date, TimeSpan.Zero));

    public static Order Build(string id, string customerId, OrderStatus status, params (Product Product, int Quantity)[] lines)
    {
        Order order = Order.Create(id, customerId, Date);
        foreach ((Product product, int quantity) in lines)
            order.AddLine(product, quantity);

        if (status == OrderStatus.Confirmed)
            order.Confirm();
        else if (status == OrderStatus.Cancelled)
            order.Cancel();

        return order;
    }
}
