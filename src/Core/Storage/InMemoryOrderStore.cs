using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

/// <summary>
/// Сховище в пам'яті: словник за id. Дані живуть, доки працює процес, —
/// підходить для демо і для тестів восьмого тижня, яким не можна торкатися диска.
/// </summary>
public sealed class InMemoryOrderStore(IEnumerable<Order>? seed = null) : IOrderStore
{
    // Дубль id у початкових даних кине виняток уже тут, під час створення сховища.
    private readonly Dictionary<string, Order> _items =
        (seed ?? []).ToDictionary(order => order.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Order> List() => _items.Values.ToList();

    public Order? GetById(string id) => _items.GetValueOrDefault(id);

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Замовлення з id={item.Id} уже існує.");

        _items.Add(item.Id, item);
    }

    public void Update(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Немає замовлення з id={item.Id}, оновлювати нічого.");

        _items[item.Id] = item;
    }

    public bool Remove(string id) => _items.Remove(id);

    public override string ToString() => $"{nameof(InMemoryOrderStore)} (у пам'яті)";
}
