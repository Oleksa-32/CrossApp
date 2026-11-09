using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

/// <summary>
/// Файлове сховище: той самий контракт, інша механіка. Кеш у пам'яті,
/// завантаження файлу при першому зверненні, запис на диск після кожної зміни.
/// На диск ідуть OrderDto з рядками — сутність із приватними сетерами
/// серіалізувати напряму незручно, і формат файлу не повинен диктувати форму домену.
/// Лабораторна 9: шлях приходить ззовні (з конфігурації через StorageOptions.ResolveFullPath),
/// формат і атомарний запис (.tmp + File.Move) — спільні з експортом у <see cref="OrderJsonFile"/>.
/// </summary>
public sealed class FileOrderStore(string path) : IOrderStore
{
    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    public IReadOnlyList<Order> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Замовлення з id={item.Id} уже існує.");

        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (!_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Немає замовлення з id={item.Id}, оновлювати нічого.");

        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id))
            return false;

        Flush();
        return true;
    }

    public override string ToString() => $"{nameof(FileOrderStore)} ({_path})";

    // Файл читається один раз; OrderJsonFile.Read проганяє кожен запис через інваріанти,
    // тому пошкоджений файл не відновить, наприклад, підтвердженого порожнього замовлення.
    // Синтаксична помилка JSON чи порушений інваріант — для викликача одна ситуація:
    // InvalidDataException «файл сховища непридатний»; кеш не лишається напівзаповненим.
    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        if (File.Exists(_path))
        {
            _cache.Clear();
            foreach (Order order in OrderJsonFile.Read(_path))
                _cache[order.Id] = order;
        }

        _loaded = true;
    }

    private void Flush() => OrderJsonFile.Write(_path, _cache.Values);
}
