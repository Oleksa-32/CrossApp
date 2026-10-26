using System.Globalization;
using Core.Domain;
using Core.Dto;

namespace Core.Reports;

/// <summary>
/// Аналітичні звіти по замовленнях засобами LINQ to Objects. Кожен метод — чиста
/// функція: приймає IEnumerable (а не сховище чи сервіс), тому його легко
/// перевірити тестом тижня 8 і викликати з будь-якого джерела — пам'яті, файлу,
/// згенерованих 100 000 записів. Повертає IReadOnlyList DTO, а не Order.
/// </summary>
/// <remarks>
/// <para>Коли виконується запит. Where, OrderBy, GroupBy і Select лише описують
/// запит: доки його не перебрали, джерело не читається. Виконання запускає
/// останній оператор кожного методу — ToList (або Sum/Average/Max у Totals), тому
/// звіт повертає вже готовий знімок, а повторний Count у Cli нічого не перераховує.</para>
/// <para>Чому в демо report-deferred два виклики Count() дали різні числа: query
/// там — не список, а рецепт «пройти source і перевірити умову». Кожен Count()
/// виконує рецепт заново над тим, що лежить у source саме зараз, тож після
/// source.Add новий елемент теж пройшов перевірку. Знімок після ToList — окремий
/// список, і source.Clear() його вже не змінює.</para>
/// </remarks>
public static class ReportService
{
    /// <summary>Гроші приносять лише підтверджені замовлення: чернетки ще не оформлені, скасовані — повернуто.</summary>
    private static bool IsConfirmed(Order order) => order.Status == OrderStatus.Confirmed;

    // 1) фільтр + сортування + проєкція: замовлення, дорожчі за поріг
    public static IReadOnlyList<OrderRow> ExpensiveOrders(IEnumerable<Order> source, decimal threshold) =>
        source.Where(order => order.Total > threshold)
              .OrderByDescending(order => order.Total).ThenBy(order => order.Id, StringComparer.Ordinal)
              .Select(ToRow)
              .ToList();

    // 2) топ-N клієнтів за сумою підтверджених замовлень: GroupBy + власний TopBy
    public static IReadOnlyList<CustomerTotal> TopCustomers(IEnumerable<Order> source, int n) =>
        source.Where(IsConfirmed)
              .GroupBy(order => order.CustomerId, StringComparer.OrdinalIgnoreCase)
              .Select(g => new CustomerTotal(g.Key, g.Count(), g.Sum(order => order.Total)))
              .TopBy(customer => customer.Total, customer => customer.CustomerId, n)
              .ToList();

    // 3) топ-N замовлень за сумою зі стабільним порядком
    public static IReadOnlyList<OrderRow> TopOrders(IEnumerable<Order> source, int n) =>
        source.TopBy(order => order.Total, order => order.Id, n)
              .Select(ToRow)
              .ToList();

    // 4) групування за станом з підрахунком і сумою
    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(order => order.Status.ToString())
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(order => order.Total)))
              .OrderByDescending(summary => summary.Total).ThenBy(summary => summary.Key, StringComparer.Ordinal)
              .ToList();

    /// <summary>
    /// Те саме групування за станом у синтаксисі запитів (додаткове завдання 3).
    /// Компілятор перекладає його на ті самі GroupBy / OrderByDescending / Select.
    /// </summary>
    public static IReadOnlyList<GroupSummary> ByStatusQuery(IEnumerable<Order> source) =>
        (from order in source
         group order by order.Status.ToString() into g
         let total = g.Sum(order => order.Total)
         orderby total descending, g.Key
         select new GroupSummary(g.Key, g.Count(), total))
        .ToList();

    // 5) виручка за місяцями: спершу фільтр, потім групування за значенням (рік, місяць)
    public static IReadOnlyList<GroupSummary> ByMonth(IEnumerable<Order> source) =>
        source.Where(IsConfirmed)
              .GroupBy(order => (order.CreatedOn.Year, order.CreatedOn.Month))
              .OrderBy(g => g.Key)
              .Select(g => new GroupSummary(
                  new DateOnly(g.Key.Year, g.Key.Month, 1).ToString("yyyy-MM", CultureInfo.InvariantCulture),
                  g.Count(),
                  g.Sum(order => order.Total)))
              .ToList();

    // 6) топ-N товарів: SelectMany розгортає рядки всіх замовлень в одну послідовність
    public static IReadOnlyList<ProductSales> TopProducts(IEnumerable<Order> source, int n) =>
        source.Where(IsConfirmed)
              .SelectMany(order => order.Lines)
              .GroupBy(line => line.ProductId, StringComparer.OrdinalIgnoreCase)
              .Select(g => new ProductSales(
                  g.Key, g.First().ProductName, g.Sum(line => line.Quantity), g.Sum(line => line.LineTotal)))
              .TopBy(product => product.Revenue, product => product.ProductId, n)
              .ToList();

    /// <summary>
    /// 7) агрегати з обробкою порожньої послідовності. Суми замовлень матеріалізуються
    /// один раз: Order.Total рахується з рядків при кожному зверненні, а Count, Sum,
    /// Average і Max по відкладеному запиту пройшли б джерело чотири рази.
    /// </summary>
    public static (int Orders, decimal Revenue, decimal AverageCheck, decimal MaxOrder) Totals(IEnumerable<Order> source)
    {
        List<decimal> totals = source.Where(IsConfirmed).Select(order => order.Total).ToList();
        return totals.Count == 0
            ? (0, 0m, 0m, 0m)                       // Average і Max на порожньому кинули б InvalidOperationException
            : (totals.Count, totals.Sum(), totals.Average(), totals.Max());
    }

    /// <summary>
    /// 8) GroupJoin довідника клієнтів із замовленнями (додаткове завдання 1): кожен
    /// клієнт отримує групу своїх замовлень, і клієнт без замовлень теж лишається у звіті.
    /// </summary>
    public static IReadOnlyList<CustomerActivity> CustomerActivity(
        IEnumerable<CustomerDto> customers, IEnumerable<Order> orders) =>
        customers.GroupJoin(orders,
                     customer => customer.Id,
                     order => order.CustomerId,
                     (customer, customerOrders) => new CustomerActivity(
                         customer.Id,
                         customer.Name,
                         customerOrders.Count(),
                         customerOrders.Where(IsConfirmed).Sum(order => order.Total)),
                     StringComparer.OrdinalIgnoreCase)
                 .OrderByDescending(activity => activity.Revenue)
                 .ThenBy(activity => activity.CustomerId, StringComparer.Ordinal)
                 .ToList();

    /// <summary>
    /// 9) Join замовлень із довідником клієнтів: ім'я замість ідентифікатора. На
    /// відміну від GroupJoin, внутрішнє з'єднання відкидає замовлення, клієнта яких
    /// немає в довіднику, і клієнтів без замовлень.
    /// </summary>
    public static IReadOnlyList<CustomerOrderRow> OrdersWithCustomers(
        IEnumerable<CustomerDto> customers, IEnumerable<Order> orders, decimal threshold) =>
        orders.Where(order => order.Total > threshold)
              .Join(customers,
                  order => order.CustomerId,
                  customer => customer.Id,
                  (order, customer) => new CustomerOrderRow(order.Id, customer.Name, order.Status.ToString(), order.Total),
                  StringComparer.OrdinalIgnoreCase)
              .OrderByDescending(row => row.Total).ThenBy(row => row.OrderId, StringComparer.Ordinal)
              .ToList();

    // Єдине місце проєкції Order → OrderRow: зміниться сутність — зміниться лише цей метод.
    private static OrderRow ToRow(Order order) =>
        new(order.Id, order.CustomerId, order.CreatedOn, order.Status.ToString(), order.Lines.Count, order.Total);
}
