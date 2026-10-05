namespace Core.Domain;

/// <summary>
/// Стан замовлення. Допустимі переходи описано в Order.EnsureCanMoveTo:
/// Draft → Confirmed, Draft → Cancelled, Confirmed → Cancelled.
/// </summary>
public enum OrderStatus
{
    Draft,
    Confirmed,
    Cancelled
}
