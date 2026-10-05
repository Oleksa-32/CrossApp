using Core.Dto;

namespace Core.Domain;

/// <summary>
/// Рядок замовлення: товар, кількість і ціна НА МОМЕНТ ПРОДАЖУ. Ціну скопійовано
/// з товару, тому пізніша зміна ціни в каталозі не змінює вже оформлене замовлення.
/// Рядок не існує сам по собі: створює і змінює його лише <see cref="Order"/>.
/// </summary>
public sealed class OrderLine
{
    public string ProductId { get; }
    public string ProductName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    private OrderLine(string productId, string productName, decimal unitPrice, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    // internal: фабрика доступна Order у межах Core, але не Cli.
    internal static OrderLine Create(string productId, string productName, decimal unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Ідентифікатор товару в рядку обов'язковий", nameof(productId));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException($"Назва товару {productId} у рядку не може бути порожньою", nameof(productName));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice,
                $"Ціна товару {productId} у рядку не може бути від'ємною");
        EnsurePositive(productId, quantity);

        return new OrderLine(productId.Trim().ToUpperInvariant(), productName.Trim(), unitPrice, quantity);
    }

    internal void Increase(int amount)
    {
        EnsurePositive(ProductId, amount);
        Quantity += amount;
    }

    internal static void EnsurePositive(string productId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity,
                $"Кількість товару {productId} у рядку має бути більшою за нуль");
    }

    public OrderLineDto ToDto() => new(ProductId, ProductName, UnitPrice, Quantity);

    public override string ToString() =>
        $"{ProductId,-6} {ProductName,-32} {Quantity,3} × {UnitPrice,9:F2} = {LineTotal,10:F2} грн";
}
