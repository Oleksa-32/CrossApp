using Core.Domain;

namespace Cli;

/// <summary>
/// Демонстрація відкладеного виконання (крок 3). service.All() щоразу повертає
/// новий знімок, тому джерелом тут є локальний список, який можна змінити між
/// побудовою запиту і його перебиранням. Лічильник у лямбді показує, скільки
/// разів запит реально пройшов джерело.
/// </summary>
internal static class DeferredScenario
{
    private const decimal Threshold = 10000m;

    public static int Run(IReadOnlyList<Order> orders, IReadOnlyList<Product> catalog)
    {
        var source = orders.ToList();
        int checks = 0;

        IEnumerable<Order> query = source.Where(order =>
        {
            checks++;
            return order.Total > Threshold;
        });
        Console.WriteLine($"Запит побудовано, умову перевірено разів: {checks}");

        Console.WriteLine($"Дорожчих за {Threshold:F0} до змін: {query.Count()} (перевірок: {checks})");

        Order extra = Order.Create("O-999", "C-008", new DateTime(2026, 11, 20, 12, 0, 0));
        extra.AddLine(catalog[0], 1);
        source.Add(extra);
        Console.WriteLine($"Дорожчих за {Threshold:F0} після source.Add: {query.Count()} (перевірок: {checks})");

        var snapshot = query.ToList();                      // результат зафіксовано
        int checksAfterSnapshot = checks;
        source.Clear();
        Console.WriteLine($"Після source.Clear(): query.Count() = {query.Count()}, " +
                          $"у знімку лишилось: {snapshot.Count}");
        Console.WriteLine($"snapshot.Count — властивість: перевірок після ToList не додалося ({checks - checksAfterSnapshot})");

        return 0;
    }
}
