using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailStock.Domain.Catalog;
using RetailStock.Domain.Transfers;

namespace RetailStock.Infrastructure.Persistence.Configurations;

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers", table =>
        {
            table.HasCheckConstraint("ck_transfers_quantity", "quantity > 0");
            table.HasCheckConstraint("ck_transfers_distinct_stores", "source_store_id <> destination_store_id");
        });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Note).HasMaxLength(250);
        builder.Property(t => t.Version).IsRowVersion().HasColumnName("xmin").HasColumnType("xid");
        builder.HasIndex(t => t.Status);

        builder.HasOne<Store>().WithMany().HasForeignKey(t => t.SourceStoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Store>().WithMany().HasForeignKey(t => t.DestinationStoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(t => t.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(t => t.DomainEvents);
    }
}
