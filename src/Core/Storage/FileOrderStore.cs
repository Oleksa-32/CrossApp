using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

/// <summary>
/// Файлове сховище: той самий контракт, інша механіка. Кеш у пам'яті,
/// завантаження файлу при першому зверненні, запис на диск після кожної зміни.
/// На диск ідуть OrderDto з рядками — сутність із приватними сетерами
/// серіалізувати напряму незручно, і формат файлу не повинен диктувати форму домену.
/// </summary>
public sealed class FileOrderStore(string path) : IOrderStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

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

    // Файл читається один раз; Order.FromDto проганяє кожен запис через інваріанти,
    // тому пошкоджений файл не відновить, наприклад, підтвердженого порожнього замовлення.
    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        if (File.Exists(_path))
        {
            try
            {
                List<OrderDto> dtos = JsonSerializer.Deserialize<List<OrderDto>>(File.ReadAllText(_path), Options) ?? [];
                foreach (OrderDto dto in dtos)
                {
                    Order order = Order.FromDto(dto);
                    _cache[order.Id] = order;
                }
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException or DomainException)
            {
                // Синтаксична помилка JSON чи порушений інваріант — для викликача це одна
                // ситуація: файл сховища непридатний. Кеш не лишається напівзаповненим.
                _cache.Clear();
                throw new InvalidDataException(
                    $"Файл {_path} не вдалося прочитати: {ex.Message.ReplaceLineEndings(" ")}", ex);
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path,
            JsonSerializer.Serialize(_cache.Values.Select(order => order.ToDto()).ToList(), Options));
    }
}
