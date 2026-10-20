using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

/// <summary>
/// Другий імпортер на тих самих типах: показує, що формат файлу — деталь,
/// а ImportResult і ProductDto від нього не залежать (додаткове завдання 1).
/// </summary>
public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <param name="onSkipped">Як у ProductCsvImporter.Load: опис кожного пропущеного елемента.</param>
    public static ImportResult<ProductDto> Load(string path, Action<string>? onSkipped = null)
    {
        string json = File.ReadAllText(path, Encoding.UTF8);

        List<ProductDto> parsed;
        try
        {
            // ?? [] — колекційний вираз замість null: файл "null" дає порожній список.
            parsed = JsonSerializer.Deserialize<List<ProductDto>>(json, Options) ?? [];
        }
        catch (JsonException ex)
        {
            // Пошкоджений JSON неподільний: на відміну від CSV, тут немає «рядка»,
            // який можна пропустити, тому весь файл стає однією помилкою.
            string error = $"файл не є коректним JSON: {ex.Message}";
            onSkipped?.Invoke(error);
            return new ImportResult<ProductDto>([], [error]);
        }

        var items = new List<ProductDto>();
        var errors = new List<string>();

        for (int i = 0; i < parsed.Count; i++)
        {
            switch (Validate(parsed[i]))
            {
                case null:
                    items.Add(parsed[i]);
                    break;
                case string reason:
                    string error = $"елемент {i + 1}: {reason}";
                    errors.Add(error);
                    onSkipped?.Invoke(error);
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    // Ті самі правила, що й у CSV, але патерни вже не списків, а властивостей:
    // форму даних гарантував десеріалізатор, перевіряти лишилося значення.
    private static string? Validate(ProductDto product) => product switch
    {
        { Id: null or "" } or { Name: null or "" } => "ідентифікатор або назва порожні",
        { Price: < 0 } => $"ціна '{product.Price}' не є невід'ємним числом",
        _ => null
    };
}
