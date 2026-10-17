using RetailStock.Domain.Common;

namespace RetailStock.Domain.Catalog;

public sealed class Product : Entity
{
    private Product()
    {
    }

    public Product(Guid id, string sku, string name, string category, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Category = category.Trim();
        Price = price;
    }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public decimal Price { get; private set; }
}
