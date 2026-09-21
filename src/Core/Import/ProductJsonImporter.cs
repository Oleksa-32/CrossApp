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

    public static ImportResult<ProductDto> Load(string path)
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
            return new ImportResult<ProductDto>([], [$"файл не є коректним JSON: {ex.Message}"]);
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
                    errors.Add($"елемент {i + 1}: {reason}");
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
