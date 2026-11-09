using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

/// <summary>
/// Формат JSON-файлу колекції замовлень (лабораторна 9): масив OrderDto з рядками.
/// Один набір JsonSerializerOptions для запису і читання — сховище, експорт та імпорт
/// бачать той самий формат.
/// </summary>
public static class OrderJsonFile
{
    /// <summary>
    /// camelCase-імена властивостей; при читанні регістр не важливий, тому відкриваються і файли
    /// попередніх лабораторних, записані з "Id"/"CustomerId". UnsafeRelaxedJsonEscaping лишає
    /// кирилицю як є ("Ноутбук", а не "Н..."): файл читає людина, у HTML він не вбудовується.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Атомарний запис: спершу повний вміст у сусідній .tmp, потім File.Move з перезаписом.
    /// Якщо процес упаде посеред запису, пошкодженим буде лише .tmp, а основний файл
    /// лишиться попередньою цілою версією. .tmp — у тому самому каталозі, щоб Move був
    /// перейменуванням у межах одного диска, а не копіюванням.
    /// </summary>
    public static void Write(string path, IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        string fullPath = Path.GetFullPath(path);
        string tmp = fullPath + ".tmp";
        List<OrderDto> dtos = orders.Select(order => order.ToDto()).ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(tmp, JsonSerializer.Serialize(dtos, Options));
        File.Move(tmp, fullPath, overwrite: true);
    }

    /// <summary>
    /// Читає колекцію і проганяє кожен запис через Order.FromDto, тобто через інваріанти.
    /// Будь-яка проблема вмісту — синтаксис JSON, невідповідний тип, порушене правило —
    /// стає одним InvalidDataException із назвою файлу і номером запису, без стека виклику для користувача.
    /// </summary>
    public static IReadOnlyList<Order> Read(string path)
    {
        string fullPath = Path.GetFullPath(path);
        List<OrderDto?> dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<OrderDto?>>(File.ReadAllText(fullPath), Options) ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Файл {fullPath} не є коректним JSON-масивом замовлень: {ex.Message.ReplaceLineEndings(" ")}", ex);
        }

        var orders = new List<Order>(dtos.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            try
            {
                OrderDto dto = dtos[i] ?? throw new ArgumentException("порожній запис (null)");
                orders.Add(Order.FromDto(dto));
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new InvalidDataException(
                    $"Файл {fullPath}, запис {i + 1}: {ex.Message.ReplaceLineEndings(" ")}", ex);
            }
        }

        return orders;
    }
}
