namespace Core.Reports;

/// <summary>
/// Підсумок однієї групи: ключ (стан, місяць), кількість замовлень у групі і їхня сума.
/// </summary>
public sealed record GroupSummary(string Key, int Count, decimal Total);
