using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

/// <summary>
/// Декоратор (додаткове завдання 1): приймає будь-який інший IOrderStore і
/// реалізує той самий контракт, додаючи кеш читань. Про конкретне внутрішнє
/// сховище нічого не знає — його обирає composition root.
/// </summary>
public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Order?> _byId = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Order>? _all;

    /// <summary>Скільки читань обслужено з кешу без звернення до внутрішнього сховища.</summary>
    public int Hits { get; private set; }

    /// <summary>Скільки читань довелося передати внутрішньому сховищу.</summary>
    public int Misses { get; private set; }

    public IReadOnlyList<Order> List()
    {
        if (_all is not null)
        {
            Hits++;
            return _all;
        }

        Misses++;
        return _all = _inner.List();
    }

    // Кешується і відсутність запису (null): повторний пошук неіснуючого id теж не йде вглиб.
    public Order? GetById(string id)
    {
        if (_byId.TryGetValue(id, out Order? cached))
        {
            Hits++;
            return cached;
        }

        Misses++;
        return _byId[id] = _inner.GetById(id);
    }

    // Кожна зміна спершу йде у внутрішнє сховище, і лише після успіху скидає кеш.
    public void Add(Order item)
    {
        _inner.Add(item);
        Invalidate(item.Id);
    }

    public void Update(Order item)
    {
        _inner.Update(item);
        Invalidate(item.Id);
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        Invalidate(id);
        return removed;
    }

    public override string ToString() => $"{nameof(CachingOrderStore)} → {_inner}";

    private void Invalidate(string id)
    {
        _byId.Remove(id);
        _all = null;
    }
}
