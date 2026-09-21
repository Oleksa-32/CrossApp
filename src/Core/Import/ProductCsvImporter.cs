using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

/// <summary>
/// Розбір прайса з CSV. Логіка живе в Core, а не в Program.cs, бо з п'ятого
/// тижня її викликатиме сховище, а з восьмого — тести.
/// </summary>
public static class ProductCsvImporter
{
    // Роздільник — крапка з комою: не конфліктує з комою в назвах товарів
    // і з десятковою комою в локалях, де вона є роздільником дробової частини.
    private const char Separator = ';';

    // Мінімальна і максимальна кількість колонок рядка даних: id;name;price.
    private const int ColumnCount = 3;

    /// <summary>
    /// Читає файл повністю і повертає прочитані товари разом із переліком помилок.
    /// Виняток не кидається: пошкоджений рядок стає записом у Errors.
    /// </summary>
    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        // Кодування задано явно: файл може бути створений в іншій ОС.
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            // Порожні рядки й коментарі — не дані і не помилки.
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Заголовок допустимий, але не обов'язковий: файл без нього теж коректний.
            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    /// <summary>
    /// Розбір одного рядка. Гілки перевіряються зверху вниз, тому конкретні
    /// перевірки стоять перед універсальними.
    /// </summary>
    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            // Патерн властивостей + реляційний патерн: «масив, у якому менше трьох елементів».
            { Length: < ColumnCount }
                => new ParseFailed($"очікую {ColumnCount} колонки, отримав {parts.Length}"),

            // Патерни списків із константним патерном "" та логічним патерном or.
            ["", _, _] or [_, "", _]
                => new ParseFailed("ідентифікатор або назва порожні"),

            // Охоронна умова when: форму перевірив патерн, значення — вираз праворуч.
            [_, _, var price] when !decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p) || p < 0
                => new ParseFailed($"ціна '{price}' не є невід'ємним числом"),

            // Патерн списку з іменами: довжина вже гарантована, індекси не потрібні.
            [var id, var name, var price]
                => new ParseOk(new ProductDto(id, name, decimal.Parse(price, NumberStyles.Number, CultureInfo.InvariantCulture))),

            // Гілка «усе інше»: сюди потрапляють лише рядки з зайвими колонками.
            _ => new ParseFailed($"занадто багато колонок: {parts.Length}")
        };
    }

    // Внутрішня кухня розбору: назовні клас віддає лише ImportResult<ProductDto>.
    private abstract record ParseOutcome;
    private sealed record ParseOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
