namespace Core;

/// <summary>
/// Порушення бізнес-правила домену: рядок у вже підтверджене замовлення, підтвердження
/// порожнього замовлення, запис імпорту з недопустимими даними. Некоректний аргумент
/// методу лишається <see cref="ArgumentException"/>, технічна помилка (диск, мережа) —
/// IOException і подібні: їх не маскують як доменні.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    /// <param name="inner">Початкова причина — зберігається, щоб не втратити деталі
    /// (наприклад, номер рядка і позицію з JsonException).</param>
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
