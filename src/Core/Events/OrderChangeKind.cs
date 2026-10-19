namespace Core.Events;

/// <summary>
/// Вид зміни замовлення, про яку сповіщає <see cref="Services.OrderService.Changed"/>.
/// Кожна константа — одна успішна операція сервісу, що змінила стан замовлення.
/// </summary>
public enum OrderChangeKind
{
    Created,
    LineAdded,
    Confirmed,
    Cancelled
}
