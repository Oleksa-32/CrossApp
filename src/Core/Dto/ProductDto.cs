namespace Core.Dto;

/// <summary>
/// Рядок прайса: товар, доступний до замовлення. Це record, а не class, бо тип
/// представляє дані одного рядка вхідного файлу — без поведінки й життєвого циклу.
/// Сутність <c>Product</c> з інваріантами з'явиться окремо в Core/Domain.
/// </summary>
/// <param name="Id">Ідентифікатор виду "P-001" — рядок, бо саме такий ключ очікують сховища.</param>
/// <param name="Name">Назва товару; порожньою бути не може, тому не nullable.</param>
/// <param name="Price">Ціна за одиницю; парситься лише з InvariantCulture.</param>
/// <param name="Note">Примітка — єдине поле, якого у файлі справді може не бути.</param>
public sealed record ProductDto(
    string Id,
    string Name,
    decimal Price,
    string? Note = null);
