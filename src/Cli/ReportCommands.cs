using Core;
using Core.Domain;
using Core.Reports;
using Core.Services;

namespace Cli;

/// <summary>
/// Підкоманди звітів лабораторної 7. Тут лише вибір звіту і вивід: жодного Where
/// чи GroupBy — усі запити живуть у Core.Reports.ReportService.
/// </summary>
internal static class ReportCommands
{
    private const int TopCount = 5;
    private const decimal ExpensiveThreshold = 10000m;

    public const string Usage =
        "report-top | report-orders | report-expensive | report-groups | report-months | report-products | report-sum\n" +
        "додатково: report-customers | report-join | report-query | report-deferred | report-bench";

    public static int Run(string command, OrderService service)
    {
        IReadOnlyList<Order> items = service.All();

        switch (command)
        {
            case "report-top":
                Console.WriteLine($"Топ-{TopCount} клієнтів за сумою підтверджених замовлень:");
                foreach (CustomerTotal c in ReportService.TopCustomers(items, TopCount))
                    Console.WriteLine($"  {c.CustomerId,-6} замовлень: {c.Orders,3}   сума: {c.Total,10:F2} грн");
                break;
            case "report-orders":
                Console.WriteLine($"Топ-{TopCount} замовлень за сумою:");
                Print(ReportService.TopOrders(items, TopCount));
                break;
            case "report-expensive":
                Console.WriteLine($"Замовлення дорожчі за {ExpensiveThreshold:F0} грн:");
                Print(ReportService.ExpensiveOrders(items, ExpensiveThreshold));
                break;
            case "report-groups":
                Console.WriteLine("Замовлення за станом:");
                PrintGroups(ReportService.ByStatus(items));
                break;
            case "report-months":
                Console.WriteLine("Виручка за місяцями (підтверджені замовлення):");
                PrintGroups(ReportService.ByMonth(items));
                break;
            case "report-products":
                Console.WriteLine($"Топ-{TopCount} товарів за виручкою:");
                foreach (ProductSales p in ReportService.TopProducts(items, TopCount))
                    Console.WriteLine($"  {p.ProductId,-6} {p.Name,-32} {p.Quantity,4} шт {p.Revenue,10:F2} грн");
                break;
            case "report-sum":
                var t = ReportService.Totals(items);
                Console.WriteLine($"  підтверджених {t.Orders}, виручка {t.Revenue:F2} грн, " +
                                  $"середній чек {t.AverageCheck:F2} грн, макс {t.MaxOrder:F2} грн");
                break;
            case "report-customers":
                Console.WriteLine("Клієнти довідника та їхні замовлення (GroupJoin):");
                foreach (CustomerActivity a in ReportService.CustomerActivity(SampleData.Customers(), items))
                    Console.WriteLine($"  {a.CustomerId,-6} {a.Name,-16} замовлень: {a.Orders,3}   виручка: {a.Revenue,10:F2} грн");
                break;
            case "report-join":
                Console.WriteLine($"Замовлення дорожчі за {ExpensiveThreshold:F0} грн з іменами клієнтів (Join):");
                foreach (CustomerOrderRow r in ReportService.OrdersWithCustomers(SampleData.Customers(), items, ExpensiveThreshold))
                    Console.WriteLine($"  {r.CustomerName,-16} {r.OrderId,-6} {r.Status,-10} {r.Total,10:F2} грн");
                break;
            case "report-query":
                return CompareQuerySyntax(items);
            case "report-deferred":
                return DeferredScenario.Run(items, SampleData.Products());
            case "report-bench":
                return BenchmarkScenario.Run();
            default:
                Console.WriteLine(Usage);
                return command == "report" ? 0 : 1;
        }

        return 0;
    }

    private static int CompareQuerySyntax(IReadOnlyList<Order> items)
    {
        IReadOnlyList<GroupSummary> methods = ReportService.ByStatus(items);
        IReadOnlyList<GroupSummary> query = ReportService.ByStatusQuery(items);

        Console.WriteLine("Синтаксис методів (ByStatus):");
        PrintGroups(methods);
        Console.WriteLine("Синтаксис запитів (ByStatusQuery):");
        PrintGroups(query);
        // record порівнюються за значенням, тому SequenceEqual перевіряє і ключі, і числа.
        Console.WriteLine($"Результати збігаються: {methods.SequenceEqual(query)}");
        return 0;
    }

    private static void Print(IReadOnlyList<OrderRow> rows)
    {
        foreach (OrderRow r in rows)
            Console.WriteLine($"  {r.Id,-6} {r.CustomerId,-6} {r.CreatedOn:yyyy-MM-dd} {r.Status,-10} " +
                              $"рядків {r.LinesCount,2} {r.Total,10:F2} грн");
        if (rows.Count == 0)
            Console.WriteLine("  (порожньо)");
    }

    private static void PrintGroups(IReadOnlyList<GroupSummary> groups)
    {
        foreach (GroupSummary g in groups)
            Console.WriteLine($"  {g.Key,-10} замовлень: {g.Count,3}   сумарно: {g.Total,10:F2} грн");
        if (groups.Count == 0)
            Console.WriteLine("  (порожньо)");
    }
}
