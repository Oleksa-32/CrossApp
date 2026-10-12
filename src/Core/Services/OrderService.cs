using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

/// <summary>
/// Бізнес-операції над замовленнями. Сервіс не знає слів «File» і «Dictionary»:
/// обидві залежності приходять через конструктор, а конкретні об'єкти створює
/// лише composition root у Cli.
/// </summary>
/// <param name="store">Будь-яка реалізація сховища: пам'ять, файл, згодом — БД.</param>
/// <param name="clock">Джерело поточного часу; тести восьмого тижня передадуть фіксований.</param>
public sealed class OrderService(IOrderStore store, TimeProvider clock)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly TimeProvider _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public Order CreateOrder(string customerId)
    {
        // Інваріанти перевірив Order.Create; унікальність id — Add сховища.
        Order order = Order.Create(NewId(), customerId, _clock.GetLocalNow().DateTime);
        _store.Add(order);
        return order;
    }

    public Order AddLine(string orderId, Product product, int quantity)
    {
        Order order = Get(orderId);
        order.AddLine(product, quantity);   // метод сутності з тижня 4
        _store.Update(order);
        return order;
    }

    public Order Confirm(string orderId)
    {
        Order order = Get(orderId);
        order.Confirm();
        _store.Update(order);
        return order;
    }

    public Order Cancel(string orderId)
    {
        Order order = Get(orderId);
        order.Cancel();
        _store.Update(order);
        return order;
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    /// <summary>Пошук за довільною умовою (додаткове завдання 2, підготовка до LINQ тижня 7).</summary>
    public IReadOnlyList<Order> Search(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    // Для Find відсутність — нормальний результат (null), для операцій зміни — відмова.
    private Order Get(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");

    // Перші 8 шістнадцяткових цифр Guid: короткий id для консолі.
    private static string NewId() => $"O-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
