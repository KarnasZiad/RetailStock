using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RetailStock.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> to create migrations without starting the API.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RetailStockDbContext>
{
    public RetailStockDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<RetailStockDbContext>()
            .UseNpgsql("Host=localhost;Database=retailstock;Username=retailstock;Password=retailstock")
            .UseSnakeCaseNamingConvention()
            .Options);
}
