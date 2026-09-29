namespace Core.Dto;

/// <summary>
/// Результат імпорту файлу з різнорідними рядками: один прохід дає одразу
/// два різні типи даних плюс спільний перелік помилок (додаткове завдання 2).
/// Узагальнений ImportResult&lt;T&gt; тут не підходить: T мав би бути один,
/// а рядки файлу описують і товари, і клієнтів.
/// </summary>
public sealed record MixedImportResult(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<CustomerDto> Customers,
    IReadOnlyList<string> Errors)
{
    /// <summary>Скільки прийнято записів обох типів разом.</summary>
    public int Accepted => Products.Count + Customers.Count;

    /// <summary>Скільки рядків з даними було розглянуто (прийняті + пропущені).</summary>
    public int TotalRows => Accepted + Errors.Count;

    /// <summary>Частка пропущених рядків у відсотках; для порожнього файлу — 0.</summary>
    public double ErrorRate => TotalRows == 0 ? 0 : (double)Errors.Count * 100 / TotalRows;
}
