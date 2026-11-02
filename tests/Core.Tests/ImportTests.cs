using System.Text;
using System.Text.Json;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    private const string ValidJson = """
        [
          {"id":"O-1","customerId":"C-1","createdOn":"2026-11-01T10:00:00","status":"Confirmed",
           "lines":[{"productId":"P-1","productName":"Ноутбук","unitPrice":100.5,"quantity":2}]},
          {"id":"O-2","customerId":"C-2","createdOn":"2026-11-02T10:00:00","status":"Draft","lines":[]},
          {"id":"O-3","customerId":"C-3","createdOn":"2026-11-03T10:00:00","status":"Cancelled","lines":[]}
        ]
        """;

    private static MemoryStream InMemory(string json) => new(Encoding.UTF8.GetBytes(json));

    [Fact]   // 7) скасування: заздалегідь скасований токен — файл навіть не відкривається
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new OrderService(new InMemoryOrderStore(), TestData.Clock());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonOrderImporter(), "any.json", cts.Token));
    }

    [Fact]   // 8) єдиний тест із диском: тимчасовий файл, видаляється у finally
    public async Task ImportAsync_FileWithInvalidRecord_ThrowsDomainException_WithRecordNumber()
    {
        string path = Path.Combine(Path.GetTempPath(), $"crossapp-test-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, """
                [
                  {"id":"O-1","customerId":"C-1","createdOn":"2026-11-01T10:00:00","status":"Draft","lines":[]},
                  {"id":"O-2","customerId":"C-2","createdOn":"2026-11-01T10:00:00","status":"Draft",
                   "lines":[{"productId":"P-1","productName":"Миша","unitPrice":10,"quantity":0}]}
                ]
                """);
            var store = new InMemoryOrderStore();
            var service = new OrderService(store, TestData.Clock());

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonOrderImporter(), path));

            Assert.StartsWith("Запис 2:", ex.Message);
            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
            Assert.Empty(store.List());   // LoadAsync відхилив файл до того, як сервіс щось додав
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_MalformedJson_WrapsJsonException()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => new JsonOrderImporter().LoadAsync(InMemory("[{\"id\": "), "broken.json"));

        Assert.IsAssignableFrom<JsonException>(ex.InnerException);
    }

    [Fact]
    public async Task LoadAsync_ValidJson_ReturnsOrdersThroughInvariants()
    {
        IReadOnlyList<Order> orders = await new JsonOrderImporter().LoadAsync(InMemory(ValidJson), "valid.json");

        Assert.Equal(new[] { "O-1", "O-2", "O-3" }, orders.Select(order => order.Id));
        Assert.Equal(201m, orders[0].Total);
        Assert.Equal(OrderStatus.Cancelled, orders[2].Status);
    }

    [Fact]   // подія для кожного доданого запису і ідемпотентність повторного імпорту
    public async Task ImportAsync_RaisesEventPerOrder_AndSecondRunAddsNothing()
    {
        var importer = new FakeOrderImporter(TestData.Orders());
        var service = new OrderService(new InMemoryOrderStore(), TestData.Clock());
        var kinds = new List<OrderChangeKind>();
        service.Changed += (_, e) => kinds.Add(e.Kind);

        int first = await service.ImportAsync(importer, "fake");
        int second = await service.ImportAsync(importer, "fake");

        Assert.Equal((5, 0), (first, second));
        Assert.Equal(Enumerable.Repeat(OrderChangeKind.Imported, 5), kinds);
    }

    [Fact]   // токен перевіряється в циклі сервісу, а не лише передається в LoadAsync
    public async Task ImportAsync_CancelledMidway_StopsAfterCurrentRecord()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store, TestData.Clock());
        using var cts = new CancellationTokenSource();
        service.Changed += (_, _) => cts.Cancel();   // скасування після першого доданого запису

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new FakeOrderImporter(TestData.Orders()), "fake", cts.Token));

        Assert.Single(store.List());
    }

    // ── Додаткове завдання 1: IAsyncEnumerable + [EnumeratorCancellation] ──

    [Fact]
    public async Task StreamAsync_YieldsOrdersInFileOrder()
    {
        var ids = new List<string>();
        await foreach (Order order in new JsonOrderImporter().StreamAsync(InMemory(ValidJson), "valid.json"))
            ids.Add(order.Id);

        Assert.Equal(new[] { "O-1", "O-2", "O-3" }, ids);
    }

    [Fact]   // токен переданий лише через WithCancellation — до ітератора його доносить атрибут
    public async Task StreamAsync_TokenFromWithCancellation_StopsEnumeration()
    {
        using var cts = new CancellationTokenSource();
        var ids = new List<string>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (Order order in new JsonOrderImporter()
                               .StreamAsync(InMemory(ValidJson), "valid.json")
                               .WithCancellation(cts.Token))
            {
                ids.Add(order.Id);
                await cts.CancelAsync();
            }
        });

        Assert.Equal(new[] { "O-1" }, ids);
    }

    [Fact]
    public async Task ImportStreamAsync_AddsEveryStreamedOrder()
    {
        var service = new OrderService(new InMemoryOrderStore(), TestData.Clock());

        int added = await service.ImportStreamAsync(new FakeOrderImporter(TestData.Orders()), "fake");

        Assert.Equal(5, added);
    }

    // ── Додаткове завдання 2: зв'язані токени користувача і таймауту ──

    [Fact]   // FakeTimeProvider: таймаут настає без реального очікування
    public async Task WithTimeoutAsync_WhenTimeoutElapses_ThrowsTimeoutException()
    {
        var clock = TestData.Clock();
        var service = new OrderService(new InMemoryOrderStore(), clock);

        Task<int> import = service.WithTimeoutAsync(
            token => service.ImportAsync(new HangingImporter(), "slow", token), TimeSpan.FromSeconds(2));
        clock.Advance(TimeSpan.FromSeconds(2));

        var ex = await Assert.ThrowsAsync<TimeoutException>(() => import);
        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
    }

    [Fact]   // контрольний випадок: скасував користувач — це не таймаут
    public async Task WithTimeoutAsync_WhenUserCancels_ThrowsOperationCanceled_NotTimeout()
    {
        var clock = TestData.Clock();
        var service = new OrderService(new InMemoryOrderStore(), clock);
        using var user = new CancellationTokenSource();

        Task<int> import = service.WithTimeoutAsync(
            token => service.ImportAsync(new HangingImporter(), "slow", token), TimeSpan.FromSeconds(2), user.Token);
        await user.CancelAsync();

        Exception ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => import);
        Assert.IsNotType<TimeoutException>(ex);
    }

    // ── Додаткове завдання 3: IProgress<int> ──

    [Fact]
    public async Task LoadAsync_ReportsGrowingProgress_EndingAt100()
    {
        var progress = new RecordingProgress();

        await new JsonOrderImporter(progress).LoadAsync(InMemory(ValidJson), "valid.json");

        Assert.NotEmpty(progress.Values);
        Assert.Equal(progress.Values.Order(), progress.Values);
        Assert.Equal(100, progress.Values[^1]);
    }
}
