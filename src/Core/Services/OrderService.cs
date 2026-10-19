using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

/// <summary>
/// Бізнес-операції над замовленнями. Сервіс не знає слів «File» і «Dictionary»:
/// обидві залежності приходять через конструктор, а конкретні об'єкти створює
/// лише composition root у Cli. Про зміни сервіс лише СПОВІЩАЄ подією Changed:
/// куди потрапить повідомлення — у консоль, файл, логер Api чи таблицю Blazor —
/// вирішує той, хто підписався.
/// </summary>
/// <param name="store">Будь-яка реалізація сховища: пам'ять, файл, згодом — БД.</param>
/// <param name="clock">Джерело поточного часу; тести восьмого тижня передадуть фіксований.</param>
public sealed class OrderService(IOrderStore store, TimeProvider clock)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly TimeProvider _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <summary>
    /// Піднімається рівно один раз після кожної УСПІШНОЇ зміни замовлення.
    /// Якщо сутність чи сховище кинули виняток, події немає.
    /// </summary>
    public event EventHandler<OrderChangedEventArgs>? Changed;

    public Order CreateOrder(string customerId)
    {
        // Інваріанти перевірив Order.Create; унікальність id — Add сховища.
        Order order = Order.Create(NewId(), customerId, _clock.GetLocalNow().DateTime);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.Created, delta: 0m);   // після зміни, не до
        return order;
    }

    public Order AddLine(string orderId, Product product, int quantity)
    {
        Order order = Get(orderId);
        decimal totalBefore = order.Total;
        order.AddLine(product, quantity);   // якщо інваріант кине виняток — події не буде
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded, order.Total - totalBefore);
        return order;
    }

    public Order Confirm(string orderId)
    {
        Order order = Get(orderId);
        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.Confirmed, delta: 0m);
        return order;
    }

    public Order Cancel(string orderId)
    {
        Order order = Get(orderId);
        order.Cancel();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.Cancelled, delta: 0m);
        return order;
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    /// <summary>
    /// Пошук за довільною умовою: критерій задає той, хто викликає. Тут потрібна
    /// відповідь «так/ні» для кожного замовлення, тому Func, а не подія.
    /// </summary>
    public IReadOnlyList<Order> Search(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    // Єдине місце, де подію викликають. ?. — підписників може не бути, і це нормально.
    private void OnChanged(Order order, OrderChangeKind kind, decimal delta) =>
        Changed?.Invoke(this, new OrderChangedEventArgs(
            order.Id, kind, order.Lines.Count, order.Total, delta, _clock.GetLocalNow()));

    // Для Find відсутність — нормальний результат (null), для операцій зміни — відмова.
    private Order Get(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");

    // Перші 8 шістнадцяткових цифр Guid: короткий id для консолі.
    private static string NewId() => $"O-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
