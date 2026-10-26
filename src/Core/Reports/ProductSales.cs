namespace Core.Reports;

/// <summary>Продажі товару за підтвердженими замовленнями: штук і виручка.</summary>
public sealed record ProductSales(string ProductId, string Name, int Quantity, decimal Revenue);
