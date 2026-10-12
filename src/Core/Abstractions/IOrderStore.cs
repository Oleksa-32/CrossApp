using Core.Domain;

namespace Core.Abstractions;

/// <summary>
/// Контракт сховища замовлень: лише ті операції, які справді потрібні сервісу.
/// Тут немає ні Flush, ні SaveToFile — спосіб зберігання вирішує реалізація.
/// Замовлення зберігається як одне ціле разом зі своїми рядками (агрегат).
/// </summary>
public interface IOrderStore
{
    /// <summary>Усі замовлення. IReadOnlyList — щоб зовнішній код не змінював
    /// сховище в обхід Add/Update/Remove.</summary>
    IReadOnlyList<Order> List();

    /// <summary>Замовлення разом із рядками або null, якщо такого id немає.</summary>
    Order? GetById(string id);

    /// <summary>Додає нове замовлення; дубль id — InvalidOperationException.</summary>
    void Add(Order item);

    /// <summary>Зберігає змінений стан наявного замовлення; невідомий id — InvalidOperationException.</summary>
    void Update(Order item);

    /// <summary>true, якщо замовлення було і його видалено.</summary>
    bool Remove(string id);
}
