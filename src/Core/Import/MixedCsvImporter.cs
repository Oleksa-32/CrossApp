using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

/// <summary>
/// Розбір файлу, у якому рядки різних типів розрізняються префіксом у першій
/// колонці: "P" — товар, "C" — клієнт. Один switch expression повертає два
/// різні типи результату (додаткове завдання 2).
/// </summary>
public static class MixedCsvImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var customers = new List<CustomerDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Три гілки замість двох: патерн типу розводить результати по своїх списках.
            switch (ParseLine(line))
            {
                case ParseProduct product:
                    products.Add(product.Value);
                    break;
                case ParseCustomer customer:
                    customers.Add(customer.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new MixedImportResult(products, customers, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            // Префікс типу — константний патерн на першій позиції списку.
            ["P", "", _, _] or ["P", _, "", _]
                => new ParseFailed("товар: ідентифікатор або назва порожні"),

            ["P", var id, var name, var price]
                when decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) && value >= 0
                => new ParseProduct(new ProductDto(id, name, value)),

            ["C", "", ..] or ["C", _, "", ..] or ["C", _, _, "", ..]
                => new ParseFailed("клієнт: ідентифікатор, ім'я або пошта порожні"),

            // Телефон необов'язковий, тому для клієнта прийнятні дві довжини рядка.
            ["C", var id, var name, var email, var phone]
                => new ParseCustomer(new CustomerDto(id, name, email, phone)),

            ["C", var id, var name, var email]
                => new ParseCustomer(new CustomerDto(id, name, email)),

            // Колонок рівно чотири, але попередня гілка не спрацювала — отже, річ у ціні.
            ["P", _, _, var price]
                => new ParseFailed($"товар: ціна '{price}' не є невід'ємним числом"),

            // Зріз .. — «префікс правильний, але кількість колонок не та».
            ["P", ..] => new ParseFailed($"товар: очікую 4 колонки (P;id;назва;ціна), отримав {parts.Length}"),
            ["C", ..] => new ParseFailed($"клієнт: очікую 4 або 5 колонок (C;id;ім'я;пошта[;телефон]), отримав {parts.Length}"),

            [var tag, ..] => new ParseFailed($"невідомий префікс типу '{tag}': очікую P або C"),

            _ => new ParseFailed("порожній рядок")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseProduct(ProductDto Value) : ParseOutcome;
    private sealed record ParseCustomer(CustomerDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
