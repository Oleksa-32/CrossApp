using Core.Domain;

namespace Core.Reports;

/// <summary>
/// Варіанти тих самих звітів, що відрізняються лише місцем матеріалізації або
/// порядком операторів (додаткове завдання 4). Результат у кожної пари однаковий —
/// різниться тільки час, який Cli вимірює Stopwatch на 100 000 записів.
/// </summary>
public static class ReportVariants
{
    // Еталон — ReportService.ExpensiveOrders: один потік від Where до ToList.

    /// <summary>Той самий звіт, але після Where результат копіюється в проміжний список.</summary>
    public static IReadOnlyList<OrderRow> ExpensiveOrdersWithMidList(IEnumerable<Order> source, decimal threshold) =>
        source.Where(order => order.Total > threshold)
              .ToList()                                    // зайвий буфер посередині ланцюжка
              .OrderByDescending(order => order.Total).ThenBy(order => order.Id, StringComparer.Ordinal)
              .Select(order => new OrderRow(order.Id, order.CustomerId, order.CreatedOn,
                  order.Status.ToString(), order.Lines.Count, order.Total))
              .ToList();

    /// <summary>Типова помилка: спершу сортування всього джерела, потім фільтр.</summary>
    public static IReadOnlyList<OrderRow> ExpensiveOrdersSortFirst(IEnumerable<Order> source, decimal threshold) =>
        source.OrderByDescending(order => order.Total).ThenBy(order => order.Id, StringComparer.Ordinal)
              .Where(order => order.Total > threshold)
              .Select(order => new OrderRow(order.Id, order.CustomerId, order.CreatedOn,
                  order.Status.ToString(), order.Lines.Count, order.Total))
              .ToList();

    /// <summary>
    /// Агрегати без матеріалізації: query — відкладений запит, і кожен із чотирьох
    /// завершальних операторів проходить усе джерело заново, щоразу перераховуючи Order.Total.
    /// Порівнювати з ReportService.Totals, який робить один ToList.
    /// </summary>
    public static (int Orders, decimal Revenue, decimal AverageCheck, decimal MaxOrder) TotalsDeferred(IEnumerable<Order> source)
    {
        IEnumerable<decimal> query = source.Where(order => order.Status == OrderStatus.Confirmed)
                                           .Select(order => order.Total);
        return query.Any()
            ? (query.Count(), query.Sum(), query.Average(), query.Max())
            : (0, 0m, 0m, 0m);
    }
}
