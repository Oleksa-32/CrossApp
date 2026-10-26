namespace Core.Reports;

/// <summary>Підсумок по клієнту: скільки підтверджених замовлень і на яку суму.</summary>
public sealed record CustomerTotal(string CustomerId, int Orders, decimal Total);
