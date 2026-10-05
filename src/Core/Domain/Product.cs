using Core.Dto;

namespace Core.Domain;

/// <summary>
/// Товар каталогу. На відміну від <see cref="ProductDto"/>, який лише переносить
/// дані рядка файлу, сутність гарантує свої правила: створити товар із порожньою
/// назвою чи від'ємною ціною неможливо, а ціна змінюється лише через ChangePrice.
/// </summary>
public sealed class Product
{
    private decimal _price;

    public string Id { get; }
    public string Name { get; }
    public string? Note { get; }
    public decimal Price => _price;

    private Product(string id, string name, decimal price, string? note)
    {
        Id = id;
        Name = name;
        _price = price;
        Note = note;
    }

    // Єдиний спосіб створити товар: усі перевірки тут.
    public static Product Create(string id, string name, decimal price, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор товару обов'язковий", nameof(id));
        // Ідентифікатор потрапить в адресу Api (/products/{id}) на десятому тижні,
        // тому пробіли та інші розділові знаки в ньому недопустимі.
        if (!id.Trim().All(c => char.IsLetterOrDigit(c) || c == '-'))
            throw new ArgumentException(
                $"Ідентифікатор товару '{id}' може містити лише літери, цифри та дефіс", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Назва товару {id} не може бути порожньою", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price,
                $"Ціна товару {id} не може бути від'ємною");

        string? trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        return new Product(id.Trim().ToUpperInvariant(), name.Trim(), price, trimmedNote);
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), newPrice,
                $"Нова ціна товару {Id} не може бути від'ємною");
        _price = newPrice;
    }

    // Мапінг у формат тижня 3 і назад — знадобиться сховищу тижня 5.
    public ProductDto ToDto() => new(Id, Name, Price, Note);

    public static Product FromDto(ProductDto dto) =>
        Create(dto.Id, dto.Name, dto.Price, dto.Note);

    public override string ToString() => $"{Id} {Name} — {Price:F2} грн";
}
