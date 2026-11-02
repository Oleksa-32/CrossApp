using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

/// <summary>
/// Бізнес-операції над замовленнями. Сервіс не знає слів «File» і «Dictionary»:
/// обидві залежності приходять через конструктор, а конкретні об'єкти створює
/// лише composition root у Cli. Про зміни сервіс лише СПОВІЩАЄ подією Changed:
/// куди потрапить повідомлення — у консоль, файл, логер Api чи таблицю Blazor —
/// вирішує той, хто підписався.
/// </summary>
/// <param name="store">Будь-яка реалізація сховища: пам'ять, файл, згодом — БД.</param>
/// <param name="clock">Джерело поточного часу і таймерів таймауту; тести передають FakeTimeProvider.</param>
/// <param name="sink">Необов'язковий одержувач сповіщень (додаткове завдання 1):
/// альтернатива події, що приходить через конструктор.</param>
public sealed class OrderService(IOrderStore store, TimeProvider clock, INotificationSink? sink = null)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly TimeProvider _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly INotificationSink? _sink = sink;

    /// <summary>
    /// Піднімається рівно один раз після кожної УСПІШНОЇ зміни замовлення.
    /// Якщо сутність чи сховище кинули виняток, події немає.
    /// </summary>
    public event EventHandler<OrderChangedEventArgs>? Changed;

    public Order CreateOrder(string customerId)
    {
        // Інваріанти перевірив Order.Create; унікальність id — Add сховища.
        Order order = Order.Create(NewId(), customerId, _clock.GetLocalNow().DateTime);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.Created, delta: 0m);   // після зміни, не до
        return order;
    }

    public Order AddLine(string orderId, Product product, int quantity)
    {
        Order order = Get(orderId);
        decimal totalBefore = order.Total;
        order.AddLine(product, quantity);   // якщо інваріант кине виняток — події не буде
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded, order.Total - totalBefore);
        return order;
    }

    public Order Confirm(string orderId)
    {
        Order order = Get(orderId);
        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.Confirmed, delta: 0m);
        return order;
    }

    public Order Cancel(string orderId)
    {
        Order order = Get(orderId);
        order.Cancel();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.Cancelled, delta: 0m);
        return order;
    }

    /// <summary>
    /// Асинхронний імпорт (тиждень 8): імпортер приходить параметром — ручний DI тижня 5.
    /// Токен передається далі в LoadAsync і перевіряється в циклі; подія Changed —
    /// для кожного доданого запису (тиждень 6).
    /// </summary>
    /// <returns>Кількість доданих замовлень; наявні id пропускаються.</returns>
    public async Task<int> ImportAsync(IOrderImporter importer, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);

        IReadOnlyList<Order> items = await importer.LoadAsync(path, ct);
        int added = 0;
        foreach (Order item in items)
        {
            ct.ThrowIfCancellationRequested();
            if (TryAddImported(item))
                added++;
        }

        return added;
    }

    /// <summary>
    /// Потоковий імпорт (додаткове завдання 1): записи додаються в міру читання файлу,
    /// без проміжного списку. Скасування між записами залишає вже додані — як і ImportAsync.
    /// </summary>
    public async Task<int> ImportStreamAsync(IOrderStreamImporter importer, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);

        int added = 0;
        await foreach (Order item in importer.StreamAsync(path, ct))
        {
            ct.ThrowIfCancellationRequested();
            if (TryAddImported(item))
                added++;
        }

        return added;
    }

    /// <summary>
    /// Виконує операцію з таймаутом (додаткове завдання 2). Токен користувача (Ctrl+C) і
    /// токен таймауту зв'язуються: операція бачить один токен і зупиняється від будь-якого
    /// з двох. Щоб викликач розрізнив причину, спрацювання саме таймауту стає TimeoutException.
    /// </summary>
    public async Task<T> WithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> operation, TimeSpan timeout, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        // Таймер від TimeProvider сервісу: у тесті час просуває FakeTimeProvider, без реального очікування.
        using var timeoutCts = new CancellationTokenSource(timeout, _clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        try
        {
            return await operation(linked.Token);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Операція не завершилася за {timeout.TotalSeconds:0.###} с.", ex);
        }
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    /// <summary>
    /// Пошук за довільною умовою: критерій задає той, хто викликає. Тут потрібна
    /// відповідь «так/ні» для кожного замовлення, тому Func, а не подія.
    /// </summary>
    public IReadOnlyList<Order> Search(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    // Єдине місце, де подію викликають. ?. — ні sink, ні підписників може не бути, і це нормально.
    private void OnChanged(Order order, OrderChangeKind kind, decimal delta)
    {
        var change = new OrderChangedEventArgs(
            order.Id, kind, order.Lines.Count, order.Total, delta, _clock.GetLocalNow());
        _sink?.Notify(change);
        Changed?.Invoke(this, change);
    }

    // Ідемпотентність імпорту: повторний запуск того самого файлу нічого не дублює.
    private bool TryAddImported(Order item)
    {
        if (_store.GetById(item.Id) is not null)
            return false;

        _store.Add(item);
        OnChanged(item, OrderChangeKind.Imported, item.Total);
        return true;
    }

    // Для Find відсутність — нормальний результат (null), для операцій зміни — порушення
    // бізнес-правила «змінювати можна лише наявне замовлення».
    private Order Get(string id) =>
        _store.GetById(id) ?? throw new DomainException($"Немає замовлення з id={id}.");

    // Перші 8 шістнадцяткових цифр Guid: короткий id для консолі.
    private static string NewId() => $"O-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
