namespace Core.Dto;

/// <summary>
/// Результат імпорту: прочитані записи РАЗОМ із переліком помилок. Один
/// пошкоджений рядок не має переривати обробку решти файлу, тому імпортер
/// не кидає виняток, а повертає обидва списки.
/// Тип узагальнений: той самий шаблон обслуговує і ProductDto, і CustomerDto.
/// </summary>
public sealed record ImportResult<T>(
    IReadOnlyList<T> Items,
    IReadOnlyList<string> Errors)
{
    /// <summary>Скільки рядків з даними було розглянуто (прийняті + пропущені).</summary>
    public int TotalRows => Items.Count + Errors.Count;

    /// <summary>Частка пропущених рядків у відсотках; для порожнього файлу — 0.</summary>
    public double ErrorRate => TotalRows == 0 ? 0 : (double)Errors.Count * 100 / TotalRows;
}
