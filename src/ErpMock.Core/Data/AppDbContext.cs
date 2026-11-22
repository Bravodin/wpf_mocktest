using ErpMock.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Document).HasMaxLength(18).IsRequired();
            e.HasIndex(x => x.Document).IsUnique();
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.City).HasMaxLength(80);
            e.Property(x => x.State).HasMaxLength(2);
        });

        b.Entity<Product>(e =>
        {
            e.Property(x => x.Sku).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Category).HasMaxLength(60);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.Cost).HasPrecision(18, 2);
            e.Ignore(x => x.IsLowStock);
            e.Ignore(x => x.Margin);
        });

        b.Entity<Sale>(e =>
        {
            e.HasIndex(x => x.Number).IsUnique();
            e.HasIndex(x => x.Date);
            e.Property(x => x.Discount).HasPrecision(18, 2);
            e.HasOne(x => x.Customer).WithMany(c => c.Sales)
                .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(i => i.Sale!)
                .HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(x => x.Subtotal);
            e.Ignore(x => x.Total);
        });

        b.Entity<SaleItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasOne(x => x.Product).WithMany()
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.Total);
        });
    }
}
