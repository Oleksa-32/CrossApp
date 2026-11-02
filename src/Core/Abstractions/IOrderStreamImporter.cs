using Core.Domain;

namespace Core.Abstractions;

/// <summary>
/// Потоковий імпорт (додаткове завдання 1): записи віддаються по одному, щойно
/// їх прочитано, тому весь файл не тримається в пам'яті, а скасування спрацьовує
/// між будь-якими двома записами.
/// </summary>
public interface IOrderStreamImporter
{
    /// <summary>
    /// Реалізація позначає параметр токена атрибутом [EnumeratorCancellation]: тоді
    /// токен, переданий через <c>WithCancellation(ct)</c> у await foreach, теж дійде до ітератора.
    /// </summary>
    IAsyncEnumerable<Order> StreamAsync(string path, CancellationToken ct = default);
}
