using System.Runtime.CompilerServices;
using Core.Abstractions;
using Core.Domain;

namespace Core.Tests;

/// <summary>
/// Імпортер без диска: віддає заздалегідь підготовлені замовлення. Завдяки контракту
/// IOrderImporter сервіс тестується ізольовано від файлової системи.
/// </summary>
internal sealed class FakeOrderImporter(IReadOnlyList<Order> orders) : IOrderImporter, IOrderStreamImporter
{
    public int Calls { get; private set; }

    public Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        Calls++;
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(orders);
    }

    public async IAsyncEnumerable<Order> StreamAsync(string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        Calls++;
        foreach (Order order in orders)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();   // справжня асинхронність між записами
            yield return order;
        }
    }
}

/// <summary>Імпортер, що ніколи не завершується сам, — лише через скасування токена.</summary>
internal sealed class HangingImporter : IOrderImporter
{
    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        await Task.Delay(Timeout.Infinite, ct);
        return [];
    }
}

/// <summary>
/// Синхронний IProgress для тестів: Progress&lt;T&gt; викликав би обробник у пулі потоків
/// пізніше, і перевірка могла б виконатися раніше за останній звіт.
/// </summary>
internal sealed class RecordingProgress : IProgress<int>
{
    public List<int> Values { get; } = [];
    public void Report(int value) => Values.Add(value);
}
