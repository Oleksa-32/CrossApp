using Core.Domain;

namespace Core.Abstractions;

/// <summary>
/// Асинхронне джерело замовлень для імпорту (тиждень 8). Сервіс отримує імпортер
/// параметром і не знає, звідки дані: файл JSON, HTTP-відповідь (тиждень 13) чи
/// заздалегідь підготовлений список у тесті.
/// </summary>
public interface IOrderImporter
{
    /// <summary>
    /// Читає всі записи і повертає їх уже доменними сутностями. Запис, що порушує
    /// правила, — <see cref="DomainException"/> з номером запису; скасування —
    /// <see cref="OperationCanceledException"/>, яке проходить наскрізь без обгортання.
    /// </summary>
    Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default);
}
