namespace Core.Reports;

/// <summary>
/// Клієнт із довідника разом з його замовленнями (GroupJoin, додаткове завдання 1).
/// Клієнт без жодного замовлення теж потрапляє у звіт — з нулями.
/// </summary>
/// <param name="Orders">Усі замовлення клієнта, незалежно від стану.</param>
/// <param name="Revenue">Сума лише підтверджених замовлень.</param>
public sealed record CustomerActivity(string CustomerId, string Name, int Orders, decimal Revenue);
