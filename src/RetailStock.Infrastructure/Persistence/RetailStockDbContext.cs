using Microsoft.EntityFrameworkCore;
using Npgsql;
using RetailStock.Application.Abstractions;
using RetailStock.Application.Common;
using RetailStock.Domain.Catalog;
using RetailStock.Domain.Stock;
using RetailStock.Domain.Transfers;

namespace RetailStock.Infrastructure.Persistence;

public sealed class RetailStockDbContext(DbContextOptions<RetailStockDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Store> Stores => Set<Store>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Transfer> Transfers => Set<Transfer>();

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The data was modified by another operation.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two operations created the same stock line at the same time: retrying will load the existing one.
            throw new ConcurrencyConflictException("A concurrent operation created the same record.", ex);
        }
    }

    public void DiscardChanges() => ChangeTracker.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RetailStockDbContext).Assembly);
}
