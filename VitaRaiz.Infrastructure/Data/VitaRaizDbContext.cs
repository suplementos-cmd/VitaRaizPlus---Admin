using Microsoft.EntityFrameworkCore;
using VitaRaiz.Domain.Entities;

namespace VitaRaiz.Infrastructure.Data;

public class VitaRaizDbContext : DbContext
{
    public VitaRaizDbContext(DbContextOptions<VitaRaizDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Zone> Zones { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleDetail> SaleDetails { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentPhoto> PaymentPhotos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("USERS");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("USER_ID");
            entity.Property(e => e.Username).HasColumnName("USERNAME").HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasColumnName("PASSWORD_HASH");
            entity.Property(e => e.RoleId).HasColumnName("ROLE_ID");
            entity.Property(e => e.ZoneId).HasColumnName("ZONE_ID");
            entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT");
            entity.Property(e => e.LastLogin).HasColumnName("LAST_LOGIN");

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Zone)
                .WithMany(z => z.Users)
                .HasForeignKey(e => e.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("ROLES");
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleId).HasColumnName("ROLE_ID");
            entity.Property(e => e.RoleName).HasColumnName("ROLE_NAME").HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("DESCRIPTION");
        });

        // Zone configuration
        modelBuilder.Entity<Zone>(entity =>
        {
            entity.ToTable("ZONES");
            entity.HasKey(e => e.ZoneId);
            entity.Property(e => e.ZoneId).HasColumnName("ZONE_ID");
            entity.Property(e => e.ZoneName).HasColumnName("ZONE_NAME").HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("DESCRIPTION");
        });

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("CUSTOMERS");
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.CustomerId).HasColumnName("CUSTOMER_ID");
            entity.Property(e => e.CustomerName).HasColumnName("CUSTOMER_NAME").HasMaxLength(255);
            entity.Property(e => e.Phone).HasColumnName("PHONE").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("EMAIL").HasMaxLength(255);
            entity.Property(e => e.Address).HasColumnName("ADDRESS");
            entity.Property(e => e.ZoneId).HasColumnName("ZONE_ID");
            entity.Property(e => e.GpsLatitude).HasColumnName("GPS_LATITUDE").HasPrecision(10, 7);
            entity.Property(e => e.GpsLongitude).HasColumnName("GPS_LONGITUDE").HasPrecision(10, 7);
            entity.Property(e => e.IsBlacklisted).HasColumnName("IS_BLACKLISTED");
            entity.Property(e => e.IsGoldCustomer).HasColumnName("IS_GOLD_CUSTOMER");
            entity.Property(e => e.RegisteredAt).HasColumnName("REGISTERED_AT");
            entity.Property(e => e.Notes).HasColumnName("NOTES");

            entity.HasOne(e => e.Zone)
                .WithMany(z => z.Customers)
                .HasForeignKey(e => e.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("PRODUCTS");
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.ProductId).HasColumnName("PRODUCT_ID");
            entity.Property(e => e.ProductName).HasColumnName("PRODUCT_NAME").HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("DESCRIPTION");
            entity.Property(e => e.Price).HasColumnName("PRICE").HasPrecision(10, 2);
            entity.Property(e => e.Category).HasColumnName("CATEGORY").HasMaxLength(100);
            entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE");
        });

        // Sale configuration
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("SALES");
            entity.HasKey(e => e.SaleId);
            entity.Property(e => e.SaleId).HasColumnName("SALE_ID");
            entity.Property(e => e.CustomerId).HasColumnName("CUSTOMER_ID");
            entity.Property(e => e.SellerId).HasColumnName("SELLER_ID");
            entity.Property(e => e.TotalAmount).HasColumnName("TOTAL_AMOUNT").HasPrecision(10, 2);
            entity.Property(e => e.PaymentTerms).HasColumnName("PAYMENT_TERMS");
            entity.Property(e => e.SaleDate).HasColumnName("SALE_DATE");
            entity.Property(e => e.Status).HasColumnName("STATUS").HasMaxLength(50);
            entity.Property(e => e.AssignedCollectorId).HasColumnName("ASSIGNED_COLLECTOR_ID");
            entity.Property(e => e.Notes).HasColumnName("NOTES");

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SaleDetail configuration
        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.ToTable("SALE_DETAILS");
            entity.HasKey(e => e.DetailId);
            entity.Property(e => e.DetailId).HasColumnName("DETAIL_ID");
            entity.Property(e => e.SaleId).HasColumnName("SALE_ID");
            entity.Property(e => e.ProductId).HasColumnName("PRODUCT_ID");
            entity.Property(e => e.Quantity).HasColumnName("QUANTITY");
            entity.Property(e => e.UnitPrice).HasColumnName("UNIT_PRICE").HasPrecision(10, 2);
            entity.Property(e => e.Subtotal).HasColumnName("SUBTOTAL").HasPrecision(10, 2);

            entity.HasOne(e => e.Sale)
                .WithMany(s => s.SaleDetails)
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.SaleDetails)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("PAYMENTS");
            entity.HasKey(e => e.PaymentId);
            entity.Property(e => e.PaymentId).HasColumnName("PAYMENT_ID");
            entity.Property(e => e.SaleId).HasColumnName("SALE_ID");
            entity.Property(e => e.CollectorId).HasColumnName("COLLECTOR_ID");
            entity.Property(e => e.Amount).HasColumnName("AMOUNT").HasPrecision(10, 2);
            entity.Property(e => e.PaymentDate).HasColumnName("PAYMENT_DATE");
            entity.Property(e => e.GpsLatitude).HasColumnName("GPS_LATITUDE").HasPrecision(10, 7);
            entity.Property(e => e.GpsLongitude).HasColumnName("GPS_LONGITUDE").HasPrecision(10, 7);
            entity.Property(e => e.Status).HasColumnName("STATUS").HasMaxLength(50);
            entity.Property(e => e.Validation).HasColumnName("VALIDATION");
            entity.Property(e => e.Notes).HasColumnName("NOTES");

            entity.HasOne(e => e.Sale)
                .WithMany(s => s.Payments)
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PaymentPhoto configuration
        modelBuilder.Entity<PaymentPhoto>(entity =>
        {
            entity.ToTable("PAYMENT_PHOTOS");
            entity.HasKey(e => e.PhotoId);
            entity.Property(e => e.PhotoId).HasColumnName("PHOTO_ID");
            entity.Property(e => e.PaymentId).HasColumnName("PAYMENT_ID");
            entity.Property(e => e.FileName).HasColumnName("FILE_NAME").HasMaxLength(255);
            entity.Property(e => e.FilePath).HasColumnName("FILE_PATH");
            entity.Property(e => e.PhotoType).HasColumnName("PHOTO_TYPE").HasMaxLength(50);
            entity.Property(e => e.GpsLatitude).HasColumnName("GPS_LATITUDE").HasPrecision(10, 7);
            entity.Property(e => e.GpsLongitude).HasColumnName("GPS_LONGITUDE").HasPrecision(10, 7);
            entity.Property(e => e.CapturedAt).HasColumnName("CAPTURED_AT");

            entity.HasOne(e => e.Payment)
                .WithMany(p => p.PaymentPhotos)
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
