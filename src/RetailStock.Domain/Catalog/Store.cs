using RetailStock.Domain.Common;

namespace RetailStock.Domain.Catalog;

public sealed class Store : Entity
{
    private Store()
    {
    }

    public Store(Guid id, string code, string name, string city)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        City = city.Trim();
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;
}
