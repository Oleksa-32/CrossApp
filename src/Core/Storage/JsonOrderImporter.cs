using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

/// <summary>
/// Імпорт замовлень із JSON-масиву OrderDto. Справжнє асинхронне введення-виведення:
/// FileStream з useAsync: true і JsonSerializer.DeserializeAsync, без Task.Run навколо
/// синхронного File.ReadAllText. Кожен запис проходить Order.FromDto, тобто ті самі
/// інваріанти, що й замовлення, створене в застосунку.
/// </summary>
/// <param name="progress">Необов'язковий одержувач відсотка прочитаного файлу
/// (додаткове завдання 3).</param>
public sealed class JsonOrderImporter(IProgress<int>? progress = null) : IOrderImporter, IOrderStreamImporter
{
    private const int BufferSize = 4096;

    // Регістр імен не важливий: приймається і "customerId" (camelCase), і "CustomerId",
    // як у файлі сховища FileOrderStore.
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        // Перевірка до відкриття файлу: скасована операція не торкається диска.
        ct.ThrowIfCancellationRequested();
        await using FileStream stream = OpenRead(path);
        return await LoadAsync(stream, Path.GetFileName(path), ct);
    }

    /// <summary>Той самий імпорт із будь-якого потоку — тести передають MemoryStream і не торкаються диска.</summary>
    public async Task<IReadOnlyList<Order>> LoadAsync(Stream source, string sourceName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<OrderDto?> dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<OrderDto?>>(Track(source), Options, ct)
                   ?? throw new DomainException($"Файл {sourceName} не містить списку замовлень.");
        }
        catch (JsonException ex)
        {
            throw NotJson(sourceName, ex);
        }

        var result = new List<Order>(dtos.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(ToOrder(dtos[i], i + 1));
        }

        return result;
    }

    public async IAsyncEnumerable<Order> StreamAsync(string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await using FileStream stream = OpenRead(path);
        await foreach (Order order in StreamAsync(stream, Path.GetFileName(path), ct))
            yield return order;
    }

    /// <summary>
    /// Записи віддаються по одному в міру читання. [EnumeratorCancellation] об'єднує
    /// токен параметра з токеном із WithCancellation: скасування працює за будь-якого
    /// способу передачі.
    /// </summary>
    public async IAsyncEnumerable<Order> StreamAsync(
        Stream source, string sourceName, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        IAsyncEnumerable<OrderDto?> dtos = JsonSerializer.DeserializeAsyncEnumerable<OrderDto>(Track(source), Options, ct);
        await using IAsyncEnumerator<OrderDto?> enumerator = dtos.GetAsyncEnumerator(ct);

        int number = 0;
        while (true)
        {
            // yield return не можна ставити в try з catch, тому в try лише крок читання.
            try
            {
                if (!await enumerator.MoveNextAsync())
                    yield break;
            }
            catch (JsonException ex)
            {
                throw NotJson(sourceName, ex);
            }

            ct.ThrowIfCancellationRequested();
            yield return ToOrder(enumerator.Current, ++number);
        }
    }

    private static FileStream OpenRead(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);

    private Stream Track(Stream source) =>
        progress is null || !source.CanSeek ? source : new ProgressReadStream(source, progress);

    /// <summary>
    /// Порушений інваріант запису — це вже не «некоректний аргумент методу», а відхилені
    /// дані імпорту, тому ArgumentException стає DomainException з номером запису.
    /// Початковий виняток лишається в InnerException.
    /// </summary>
    private static Order ToOrder(OrderDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожній елемент (null) замість замовлення.");

        try
        {
            return Order.FromDto(dto);
        }
        catch (Exception ex) when (ex is ArgumentException or DomainException)
        {
            throw new DomainException($"Запис {number}: {ex.Message.ReplaceLineEndings(" ")}", ex);
        }
    }

    // Файл, що не є JSON-масивом замовлень, — відхилені вхідні дані, а не збій інфраструктури.
    // JsonException зберігається як InnerException: у ньому номер рядка і позиція помилки.
    private static DomainException NotJson(string sourceName, JsonException ex) =>
        new($"Файл {sourceName} не є коректним JSON-масивом замовлень" +
            (ex.LineNumber is long line ? $" (рядок {line + 1}, позиція {ex.BytePositionInLine + 1})." : "."), ex);
}
