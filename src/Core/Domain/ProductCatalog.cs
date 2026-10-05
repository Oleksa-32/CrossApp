using Core.Dto;

namespace Core.Domain;

/// <summary>
/// Перетворення результату імпорту тижня 3 на доменні сутності (додаткові завдання 1 і 2).
/// Ідея та сама, що й в імпортері: «дані + помилки», один поганий запис не зупиняє решту.
/// </summary>
public static class ProductCatalog
{
    /// <summary>
    /// Пропускає кожен ProductDto через Product.FromDto. Помилки формату з імпорту
    /// переносяться як є, до них додаються записи, що не пройшли інваріанти сутності,
    /// і дублікати ідентифікатора.
    /// </summary>
    public static ImportResult<Product> FromImport(ImportResult<ProductDto> imported)
    {
        ArgumentNullException.ThrowIfNull(imported);

        var products = new List<Product>();
        var errors = new List<string>(imported.Errors);

        // Унікальність ідентифікатора — правило про ДВА і більше товарів: окремий
        // Product не знає про інших, тому перевірити його може лише той, хто бачить
        // увесь набір. На п'ятому тижні це робитиме сервіс через сховище.
        var seenIds = new HashSet<string>();

        foreach (ProductDto dto in imported.Items)
        {
            Product product;
            try
            {
                product = Product.FromDto(dto);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"товар '{dto.Id}': {ex.Message}");
                continue;
            }

            if (!seenIds.Add(product.Id))
            {
                errors.Add($"товар '{dto.Id}': ідентифікатор {product.Id} уже належить іншому товару");
                continue;
            }

            products.Add(product);
        }

        return new ImportResult<Product>(products, errors);
    }
}
