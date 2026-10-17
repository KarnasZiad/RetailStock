using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailStock.Domain.Catalog;
using RetailStock.Domain.Stock;

namespace RetailStock.Infrastructure.Persistence.Configurations;

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items", table =>
            table.HasCheckConstraint("ck_stock_items_quantities", "on_hand >= 0 AND reserved >= 0 AND reserved <= on_hand"));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.HasIndex(s => new { s.StoreId, s.ProductId }).IsUnique();

        // PostgreSQL system column xmin: optimistic concurrency without an extra column.
        builder.Property(s => s.Version).IsRowVersion().HasColumnName("xmin").HasColumnType("xid");

        builder.HasOne<Store>().WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Reservations).WithOne().HasForeignKey(r => r.StockItemId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Reservations).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(s => s.Available);
        builder.Ignore(s => s.Level);
        builder.Ignore(s => s.DomainEvents);
    }
}
