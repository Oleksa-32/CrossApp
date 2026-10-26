namespace Core.Reports;

/// <summary>Замовлення з іменем клієнта замість ідентифікатора (Join, додаткове завдання 1).</summary>
public sealed record CustomerOrderRow(string OrderId, string CustomerName, string Status, decimal Total);
