namespace Core.Dto;

/// <summary>
/// Клієнт, який оформлює замовлення. Другий record-тип домену: потрібен, щоб
/// показати, що <see cref="ImportResult{T}"/> не прив'язаний до одного типу даних.
/// </summary>
/// <param name="Id">Ідентифікатор виду "C-001".</param>
/// <param name="Name">Ім'я клієнта; обов'язкове.</param>
/// <param name="Email">Пошта; обов'язкова, бо за нею надсилають підтвердження.</param>
/// <param name="Phone">Телефон — необов'язковий, тому nullable.</param>
public sealed record CustomerDto(
    string Id,
    string Name,
    string Email,
    string? Phone = null);
