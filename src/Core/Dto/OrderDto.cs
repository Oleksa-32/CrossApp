namespace Core.Dto;

/// <summary>
/// Замовлення у форматі зберігання. Це лише форма даних для файлу чи сховища:
/// правил тут немає, їх перевіряє сутність <c>Core.Domain.Order</c> під час FromDto.
/// </summary>
/// <param name="Id">Номер замовлення виду "O-001".</param>
/// <param name="CustomerId">Ідентифікатор клієнта виду "C-001".</param>
/// <param name="CreatedOn">Момент створення, зафіксований один раз.</param>
/// <param name="Status">Стан рядком ("Draft", "Confirmed", "Cancelled"): формат файлу
/// не залежить від enum доменної моделі.</param>
/// <param name="Lines">Рядки замовлення.</param>
public sealed record OrderDto(
    string Id,
    string CustomerId,
    DateTime CreatedOn,
    string Status,
    IReadOnlyList<OrderLineDto> Lines);

/// <summary>Рядок замовлення у форматі зберігання.</summary>
/// <param name="ProductId">Ідентифікатор товару.</param>
/// <param name="ProductName">Назва товару на момент продажу.</param>
/// <param name="UnitPrice">Ціна за одиницю на момент продажу.</param>
/// <param name="Quantity">Кількість одиниць.</param>
public sealed record OrderLineDto(
    string ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);
