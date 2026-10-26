using System.Diagnostics;
using Core;
using Core.Domain;
using Core.Reports;

namespace Cli;

/// <summary>
/// Додаткове завдання 4: час звіту на 100 000 згенерованих замовлень. Кожен варіант
/// спершу виконується кілька разів «на прогрів» (багаторівнева JIT-компіляція доводить
/// гарячі методи до оптимізованого коду не з першого виклику), перед кожним виміром
/// збирається сміття попереднього, і друкується найкращий з кількох часів. Без цього
/// варіант, який запускався першим, систематично виходив повільнішим — незалежно від того,
/// який саме це був варіант.
/// </summary>
internal static class BenchmarkScenario
{
    private const int OrderCount = 100_000;
    private const int WarmUpRuns = 5;
    private const int Runs = 10;
    private const decimal Threshold = 30000m;

    public static int Run()
    {
        var generation = Stopwatch.StartNew();
        IReadOnlyList<Order> orders = SampleData.Generate(OrderCount);
        Console.WriteLine($"Згенеровано замовлень: {orders.Count} за {generation.ElapsedMilliseconds} мс");
        Console.WriteLine($"Звіт «дорожчі за {Threshold:F0} грн», найкращий час з {Runs} запусків:");

        var (streaming, streamingMs) = Measure(() => ReportService.ExpensiveOrders(orders, Threshold));
        var (midList, midListMs) = Measure(() => ReportVariants.ExpensiveOrdersWithMidList(orders, Threshold));
        var (sortFirst, sortFirstMs) = Measure(() => ReportVariants.ExpensiveOrdersSortFirst(orders, Threshold));

        PrintRow("Where → OrderBy → Select → ToList", streamingMs, streaming.Count);
        PrintRow("Where → ToList → OrderBy → Select → ToList", midListMs, midList.Count);
        PrintRow("OrderBy → Where → Select → ToList (помилка)", sortFirstMs, sortFirst.Count);
        Console.WriteLine($"  результати однакові: {streaming.SequenceEqual(midList) && streaming.SequenceEqual(sortFirst)}");

        Console.WriteLine("Агрегати (кількість, сума, середнє, максимум):");
        var (materialized, materializedMs) = Measure(() => ReportService.Totals(orders));
        var (deferred, deferredMs) = Measure(() => ReportVariants.TotalsDeferred(orders));
        PrintRow("один ToList, потім 4 агрегати", materializedMs, materialized.Orders);
        PrintRow("4 агрегати по відкладеному запиту", deferredMs, deferred.Orders);
        Console.WriteLine($"  результати однакові: {materialized == deferred}");

        return 0;
    }

    private static (T Result, double Milliseconds) Measure<T>(Func<T> report)
    {
        T result = report();
        for (int i = 1; i < WarmUpRuns; i++)
            result = report();

        double best = double.MaxValue;
        for (int i = 0; i < Runs; i++)
        {
            // Сміття попередніх запусків не повинне збиратися під час виміру наступного.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var stopwatch = Stopwatch.StartNew();
            result = report();
            stopwatch.Stop();
            best = Math.Min(best, stopwatch.Elapsed.TotalMilliseconds);
        }

        return (result, best);
    }

    private static void PrintRow(string variant, double milliseconds, int rows) =>
        Console.WriteLine($"  {variant,-45} {milliseconds,8:F2} мс   записів: {rows}");
}
