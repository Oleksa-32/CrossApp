using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class OrderServiceTests
{
    [Fact]   // 1) інваріант варіанта «Замовлення»: доменна відмова і незмінний стан
    public void AddLine_ToConfirmedOrder_ThrowsDomainException_AndKeepsTotal()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store, TestData.Clock());
        Order order = service.CreateOrder("C-1");
        service.AddLine(order.Id, TestData.Laptop(), 1);
        service.Confirm(order.Id);

        var ex = Assert.Throws<DomainException>(() => service.AddLine(order.Id, TestData.Mouse(), 1));

        Assert.Contains("вже підтверджено", ex.Message);
        Assert.Equal(10000m, store.GetById(order.Id)!.Total);
    }

    [Fact]   // 2) некоректний аргумент — ArgumentException, а не DomainException
    public void AddLine_WithZeroQuantity_ThrowsArgumentOutOfRange_AndKeepsLines()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store, TestData.Clock());
        Order order = service.CreateOrder("C-1");

        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddLine(order.Id, TestData.Mouse(), 0));

        Assert.Empty(store.GetById(order.Id)!.Lines);
    }

    [Fact]
    public void Confirm_EmptyOrder_ThrowsDomainException()
    {
        var service = new OrderService(new InMemoryOrderStore(), TestData.Clock());
        Order order = service.CreateOrder("C-1");

        Assert.Throws<DomainException>(() => service.Confirm(order.Id));
    }

    [Fact]   // 3) подія тижня 6 як спосіб перевірки: ні консолі, ні диска
    public void CreateOrder_RaisesChangedExactlyOnce()
    {
        var service = new OrderService(new InMemoryOrderStore(), TestData.Clock());
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        Order order = service.CreateOrder("C-1");

        OrderChangedEventArgs single = Assert.Single(events);
        Assert.Equal(OrderChangeKind.Created, single.Kind);
        Assert.Equal(order.Id, single.OrderId);
    }

    [Fact]
    public void FailedOperation_RaisesNoEvent()
    {
        var service = new OrderService(new InMemoryOrderStore(), TestData.Clock());
        Order order = service.CreateOrder("C-1");
        int events = 0;
        service.Changed += (_, _) => events++;

        Assert.Throws<DomainException>(() => service.Confirm(order.Id));

        Assert.Equal(0, events);
    }

    // 4) варіант «Замовлення»: сума після додавання рядків. Той самий товар двічі —
    // кількість у наявному рядку, а не новий рядок.
    [Theory]
    [InlineData(1, 0, 10000)]
    [InlineData(1, 2, 15000)]
    [InlineData(2, 3, 27500)]
    public void Total_AfterAddingLines_IsSumOfLineTotals(int laptops, int mice, int expected)
    {
        var service = new OrderService(new InMemoryOrderStore(), TestData.Clock());
        Order order = service.CreateOrder("C-1");

        service.AddLine(order.Id, TestData.Laptop(), laptops);
        for (int i = 0; i < mice; i++)
            service.AddLine(order.Id, TestData.Mouse(), 1);

        Assert.Equal(expected, service.Find(order.Id)!.Total);
    }

    [Fact]
    public void Confirm_MissingOrder_ThrowsDomainException()
    {
        var service = new OrderService(new InMemoryOrderStore(), TestData.Clock());

        Assert.Throws<DomainException>(() => service.Confirm("O-404"));
    }
}
