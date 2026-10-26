namespace Core.Reports;

/// <summary>
/// Рядок звіту про одне замовлення. Звіт повертає цей record, а не Order: контракт
/// звіту (відповідь Api на тижні 10, таблиця Blazor на тижні 12) не залежить від
/// того, як влаштована сутність усередині, — змінити доведеться лише проєкцію.
/// </summary>
/// <param name="Status">Назва стану рядком, як в OrderDto: формат не прив'язаний до enum домену.</param>
public sealed record OrderRow(
    string Id,
    string CustomerId,
    DateTime CreatedOn,
    string Status,
    int LinesCount,
    decimal Total);
