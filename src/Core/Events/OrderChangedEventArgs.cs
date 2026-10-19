namespace Core.Events;

/// <summary>
/// Дані події «замовлення змінилося»: що сталося, з яким замовленням і який
/// у нього стан уже ПІСЛЯ зміни. Підписнику не треба йти в сховище, щоб
/// дізнатися нову суму чи кількість рядків.
/// </summary>
/// <param name="at">Момент зміни. Приходить із TimeProvider сервісу, а не з
/// DateTimeOffset.Now: тести восьмого тижня зможуть перевірити і час.</param>
public sealed class OrderChangedEventArgs(
    string orderId, OrderChangeKind kind, int linesCount, decimal total, decimal delta, DateTimeOffset at) : EventArgs
{
    public string OrderId { get; } = orderId;
    public OrderChangeKind Kind { get; } = kind;
    public int LinesCount { get; } = linesCount;
    public decimal Total { get; } = total;      // нова сума замовлення
    public decimal Delta { get; } = delta;      // на скільки змінилася сума
    public DateTimeOffset At { get; } = at;
}
