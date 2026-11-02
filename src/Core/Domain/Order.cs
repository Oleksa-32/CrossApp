using Core.Dto;

namespace Core.Domain;

/// <summary>
/// Замовлення клієнта — головна сутність домену. Стан змінюється лише через
/// AddLine, Confirm і Cancel; кожен метод спершу перевіряє ВСІ умови і лише
/// потім змінює поля, тому виняток ніколи не залишає замовлення напівзміненим.
/// Некоректний аргумент — ArgumentException; дія, недопустима в поточному стані
/// замовлення (бізнес-правило), — <see cref="DomainException"/> (тиждень 8).
/// </summary>
public sealed class Order
{
    // Колекція захищена: назовні — лише читання, додавати рядки можна тільки через AddLine.
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public DateTime CreatedOn { get; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(line => line.LineTotal);

    private Order(string id, string customerId, DateTime createdOn)
    {
        Id = id;
        CustomerId = customerId;
        CreatedOn = createdOn;
        Status = OrderStatus.Draft;
    }

    // Дата приходить ззовні, а не з DateTime.Now: так її фіксують один раз,
    // а тести восьмого тижня зможуть передати наперед відоме значення.
    public static Order Create(string id, string customerId, DateTime createdOn)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Номер замовлення обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException($"Замовлення {id}: ідентифікатор клієнта обов'язковий", nameof(customerId));
        if (createdOn == default)
            throw new ArgumentException($"Замовлення {id}: дата створення не задана", nameof(createdOn));

        return new Order(id.Trim().ToUpperInvariant(), customerId.Trim().ToUpperInvariant(), createdOn);
    }

    /// <summary>
    /// Додає товар у замовлення. Повторне додавання того самого товару за тією
    /// самою ціною збільшує кількість у наявному рядку замість дублювання рядка.
    /// </summary>
    public void AddLine(string productId, string productName, decimal unitPrice, int quantity)
    {
        // Аргументи перевіряє фабрика рядка; стан — EnsureDraft. Лише після цього — зміна.
        OrderLine candidate = OrderLine.Create(productId, productName, unitPrice, quantity);
        EnsureDraft("додавати рядки");

        OrderLine? existing = _lines.Find(line =>
            line.ProductId == candidate.ProductId && line.UnitPrice == candidate.UnitPrice);

        if (existing is null)
            _lines.Add(candidate);
        else
            existing.Increase(candidate.Quantity);
    }

    // Ціна копіюється з товару в момент додавання — це і є «ціна на момент продажу».
    public void AddLine(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        AddLine(product.Id, product.Name, product.Price, quantity);
    }

    public void Confirm()
    {
        EnsureCanMoveTo(OrderStatus.Confirmed);
        if (_lines.Count == 0)
            throw new DomainException(
                $"Замовлення {Id} порожнє: підтвердити можна лише замовлення з рядками");

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        EnsureCanMoveTo(OrderStatus.Cancelled);
        Status = OrderStatus.Cancelled;
    }

    // Мапінг у формат зберігання і назад — знадобиться сховищу тижня 5.
    public OrderDto ToDto() =>
        new(Id, CustomerId, CreatedOn, Status.ToString(), _lines.Select(line => line.ToDto()).ToList());

    /// <summary>
    /// Відновлює замовлення через ті самі Create, AddLine і Confirm/Cancel, що й
    /// у звичайному житті об'єкта, тому пошкоджений файл не дасть, наприклад,
    /// підтвердженого порожнього замовлення чи рядка з нульовою кількістю.
    /// </summary>
    public static Order FromDto(OrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        OrderStatus status = dto.Status switch
        {
            nameof(OrderStatus.Draft) => OrderStatus.Draft,
            nameof(OrderStatus.Confirmed) => OrderStatus.Confirmed,
            nameof(OrderStatus.Cancelled) => OrderStatus.Cancelled,
            _ => throw new ArgumentException(
                $"Замовлення {dto.Id}: невідомий стан '{dto.Status}', очікую Draft, Confirmed або Cancelled",
                nameof(dto))
        };

        Order order = Create(dto.Id, dto.CustomerId, dto.CreatedOn);
        // Анотації nullable — лише обіцянка: з JSON може прийти "lines": null або null-елемент.
        foreach (OrderLineDto? line in dto.Lines ?? [])
        {
            if (line is null)
                throw new ArgumentException($"Замовлення {dto.Id}: порожній елемент у списку рядків", nameof(dto));
            order.AddLine(line.ProductId, line.ProductName, line.UnitPrice, line.Quantity);
        }

        if (status == OrderStatus.Confirmed)
            order.Confirm();
        else if (status == OrderStatus.Cancelled)
            order.Cancel();

        return order;
    }

    public override string ToString() =>
        $"{Id} клієнт {CustomerId} від {CreatedOn:yyyy-MM-dd} — {Describe(Status)}, " +
        $"рядків {_lines.Count}, сума {Total:F2} грн";

    // Таблиця допустимих переходів: усе, чого тут немає, заборонено.
    private void EnsureCanMoveTo(OrderStatus target)
    {
        bool allowed = (Status, target) switch
        {
            (OrderStatus.Draft, OrderStatus.Confirmed) => true,
            (OrderStatus.Draft, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            _ => false
        };

        if (allowed)
            return;

        throw Status == target
            ? new DomainException($"Замовлення {Id} вже {DescribeDone(Status)}")
            : new DomainException($"Замовлення {Id}: перехід «{Describe(Status)}» → «{Describe(target)}» неможливий");
    }

    private void EnsureDraft(string action)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException(
                $"Замовлення {Id} вже {DescribeDone(Status)}: {action} можна лише в чернетці");
    }

    // «вже підтверджено» — текст бізнес-правила варіанта «Замовлення».
    private static string DescribeDone(OrderStatus status) => status switch
    {
        OrderStatus.Confirmed => "підтверджено",
        OrderStatus.Cancelled => "скасовано",
        _ => Describe(status)
    };

    private static string Describe(OrderStatus status) => status switch
    {
        OrderStatus.Draft => "чернетка",
        OrderStatus.Confirmed => "підтверджене",
        OrderStatus.Cancelled => "скасоване",
        _ => status.ToString()
    };
}
