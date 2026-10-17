using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RetailStock.Domain.Catalog;
using RetailStock.Domain.Stock;

namespace RetailStock.Infrastructure.Persistence;

/// <summary>Applies pending migrations and, on an empty database, loads a demo store network.</summary>
public sealed partial class DatabaseInitializer(RetailStockDbContext db, TimeProvider clock, ILogger<DatabaseInitializer> logger)
{
    // The .NET 8 [LoggerMessage] generator needs an ILogger field (primary constructor parameters are not supported).
    private readonly ILogger<DatabaseInitializer> _logger = logger;

    public async Task InitializeAsync(bool seedDemoData, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (!seedDemoData || await db.Stores.AnyAsync(cancellationToken))
        {
            return;
        }

        var stores = new[]
        {
            new Store(Guid.NewGuid(), "PAR-RIV", "Paris Rivoli", "Paris"),
            new Store(Guid.NewGuid(), "LYO-PDI", "Lyon Part-Dieu", "Lyon"),
            new Store(Guid.NewGuid(), "MRS-TDP", "Marseille Terrasses du Port", "Marseille"),
            new Store(Guid.NewGuid(), "BDX-STC", "Bordeaux Sainte-Catherine", "Bordeaux"),
            new Store(Guid.NewGuid(), "LIL-EUR", "Lille Euralille", "Lille"),
        };

        var products = new[]
        {
            new Product(Guid.NewGuid(), "TSH-001", "T-shirt coton bio", "Textile", 19.90m),
            new Product(Guid.NewGuid(), "JEA-002", "Jean slim brut", "Textile", 59.90m),
            new Product(Guid.NewGuid(), "SWE-003", "Sweat a capuche", "Textile", 44.90m),
            new Product(Guid.NewGuid(), "VES-004", "Veste en jean", "Textile", 79.90m),
            new Product(Guid.NewGuid(), "CHE-005", "Chemise en lin", "Textile", 49.90m),
            new Product(Guid.NewGuid(), "SNK-006", "Sneakers cuir blanc", "Chaussures", 89.90m),
            new Product(Guid.NewGuid(), "BOT-007", "Bottines chelsea", "Chaussures", 119.00m),
            new Product(Guid.NewGuid(), "SAC-008", "Sac cabas toile", "Accessoires", 34.90m),
            new Product(Guid.NewGuid(), "CEI-009", "Ceinture cuir", "Accessoires", 29.90m),
            new Product(Guid.NewGuid(), "CAS-010", "Casquette brodee", "Accessoires", 24.90m),
        };

        db.Stores.AddRange(stores);
        db.Products.AddRange(products);

        var random = new Random(2024);
        var now = clock.GetUtcNow();
        foreach (var store in stores)
        {
            foreach (var product in products)
            {
                var item = new StockItem(store.Id, product.Id, now, alertThreshold: random.Next(3, 8));
                var quantity = random.Next(0, 40);
                if (quantity > 0)
                {
                    item.Receive(quantity, "Stock initial", now);
                }

                db.StockMovements.AddRange(item.DequeueDomainEvents().OfType<StockChanged>().Select(StockMovement.From));
                db.StockItems.Add(item);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        LogSeeded(stores.Length, products.Length);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo data seeded: {StoreCount} stores, {ProductCount} products")]
    private partial void LogSeeded(int storeCount, int productCount);
}
