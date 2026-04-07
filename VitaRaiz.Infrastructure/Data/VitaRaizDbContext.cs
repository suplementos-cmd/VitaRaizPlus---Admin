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
    public DbSet<SalePhoto> SalePhotos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("USERS");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("USER_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.Username).HasColumnName("USERNAME").HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasColumnName("PASSWORD_HASH");
            entity.Property(e => e.RoleId).HasColumnName("ROLE_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.ZoneId).HasColumnName("ZONE_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE")
                .HasColumnType("NUMBER(1)")
                .HasConversion(
                    v => v ? 1 : 0,
                    v => v == 1);
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
            entity.Property(e => e.RoleId).HasColumnName("ROLE_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.RoleName).HasColumnName("ROLE_NAME").HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("ROLE_DESCRIPTION");
            entity.Property(e => e.DefaultThemeColor).HasColumnName("DEFAULT_THEME_COLOR").HasMaxLength(7);
            entity.Property(e => e.DefaultThemeLight).HasColumnName("DEFAULT_THEME_LIGHT").HasMaxLength(7);
            entity.Property(e => e.DefaultThemeLighter).HasColumnName("DEFAULT_THEME_LIGHTER").HasMaxLength(7);
        });

        // Zone configuration
        modelBuilder.Entity<Zone>(entity =>
        {
            entity.ToTable("ZONES");
            entity.HasKey(e => e.ZoneId);
            entity.Property(e => e.ZoneId).HasColumnName("ZONE_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.ZoneName).HasColumnName("ZONE_NAME").HasMaxLength(100);
            entity.Property(e => e.ZoneCode).HasColumnName("ZONE_CODE").HasMaxLength(10);
            entity.Property(e => e.Description).HasColumnName("DESCRIPTION");
            entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE")
                .HasColumnType("CHAR(1)")
                .HasConversion(
                    v => v ? "1" : "0",
                    v => v == "1");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT");
        });

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("CUSTOMERS");
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.CustomerId).HasColumnName("CUSTOMER_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.CustomerName).HasColumnName("CUSTOMER_NAME").HasMaxLength(255);
            entity.Property(e => e.Phone).HasColumnName("PHONE").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("EMAIL").HasMaxLength(255);
            entity.Property(e => e.Address).HasColumnName("ADDRESS");
            entity.Property(e => e.ZoneId).HasColumnName("ZONE_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.GpsLatitude).HasColumnName("GPS_LATITUDE").HasPrecision(10, 7);
            entity.Property(e => e.GpsLongitude).HasColumnName("GPS_LONGITUDE").HasPrecision(10, 7);
            entity.Property(e => e.IsBlacklisted).HasColumnName("IS_BLACKLISTED")
                .HasColumnType("NUMBER(1)")
                .HasConversion(
                    v => v ? 1 : 0,
                    v => v == 1);
            entity.Property(e => e.IsGoldCustomer).HasColumnName("IS_GOLD_CUSTOMER")
                .HasColumnType("NUMBER(1)")
                .HasConversion(
                    v => v ? 1 : 0,
                    v => v == 1);
            entity.Property(e => e.RegisteredAt).HasColumnName("CREATED_AT");
            entity.Ignore(e => e.Notes); // Columna NO existe en Oracle

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
            entity.Property(e => e.ProductId).HasColumnName("PRODUCT_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.ProductName).HasColumnName("PRODUCT_NAME").HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("DESCRIPTION");
            entity.Property(e => e.Price).HasColumnName("UNIT_PRICE").HasPrecision(10, 2);
            entity.Property(e => e.Stock).HasColumnName("STOCK_QUANTITY")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.Category).HasColumnName("CATEGORY").HasMaxLength(100);
            entity.Property(e => e.PhotoUrl).HasColumnName("PHOTO_URL").HasMaxLength(500);
            entity.Ignore(e => e.UnitPrice); // Propiedad computada, no es columna
            entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE")
                .HasColumnType("NUMBER(1)")
                .HasConversion(
                    v => v ? 1 : 0,
                    v => v == 1);
        });

        // Sale configuration
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("SALES");
            entity.HasKey(e => e.SaleId);
            entity.Property(e => e.SaleId).HasColumnName("SALE_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.CustomerId).HasColumnName("CUSTOMER_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.SellerId).HasColumnName("SELLER_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.TotalAmount).HasColumnName("TOTAL_AMOUNT").HasPrecision(10, 2);
            entity.Ignore(e => e.PaidAmount); // Columna NO existe - se calcula desde PAYMENTS
            
            // Términos de pago estructurados
            entity.Property(e => e.PaymentTerm).HasColumnName("PAYMENT_TERM").HasMaxLength(20);
            entity.Property(e => e.CollectionDay).HasColumnName("COLLECTION_DAY").HasMaxLength(10);
            entity.Property(e => e.FirstCollectionDate).HasColumnName("FIRST_COLLECTION_DATE");
            entity.Property(e => e.DownPayment).HasColumnName("DOWN_PAYMENT").HasPrecision(10, 2);
            
            // Legacy/Complementarios
            entity.Property(e => e.PaymentTerms).HasColumnName("PAYMENT_TERMS");
            entity.Property(e => e.PaymentTermDays).HasColumnName("NUMBER_OF_PAYMENTS")
                .HasColumnType("NUMBER(10)");
            
            entity.Property(e => e.SaleDate).HasColumnName("SALE_DATE");
            entity.Property(e => e.Status).HasColumnName("STATUS").HasMaxLength(50);
            entity.Property(e => e.AssignedCollectorId).HasColumnName("ASSIGNED_COLLECTOR_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.Notes).HasColumnName("NOTES");
            entity.Ignore(e => e.DueDate); // Computed property

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con User como Seller
            entity.HasOne(e => e.Seller)
                .WithMany(u => u.SalesCreated)
                .HasForeignKey(e => e.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con User como AssignedCollector
            entity.HasOne(e => e.AssignedCollector)
                .WithMany() // Sin navegación inversa en User
                .HasForeignKey(e => e.AssignedCollectorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SaleDetail configuration
        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.ToTable("SALE_ITEMS");
            entity.HasKey(e => e.DetailId);
            entity.Property(e => e.DetailId).HasColumnName("SALE_ITEM_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.SaleId).HasColumnName("SALE_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.ProductId).HasColumnName("PRODUCT_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.Quantity).HasColumnName("QUANTITY")
                .HasColumnType("NUMBER(10)");
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
            entity.Property(e => e.PaymentId).HasColumnName("PAYMENT_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.SaleId).HasColumnName("SALE_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.CollectorId).HasColumnName("COLLECTOR_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.Amount).HasColumnName("AMOUNT").HasPrecision(10, 2);
            entity.Property(e => e.PaymentDate).HasColumnName("PAYMENT_DATE");
            entity.Property(e => e.GpsLatitude).HasColumnName("GPS_LATITUDE").HasPrecision(10, 7);
            entity.Property(e => e.GpsLongitude).HasColumnName("GPS_LONGITUDE").HasPrecision(10, 7);  
            entity.Property(e => e.StatusId).HasColumnName("STATUS");
            entity.Ignore(e => e.Validation);  // Columna NO existe en Oracle
            entity.Ignore(e => e.UpdatedAt);   // Sin columna UPDATED_AT en PAYMENTS
            entity.Ignore(e => e.UpdatedBy);   // Sin columna UPDATED_BY en PAYMENTS
            entity.Property(e => e.CollectionActionId).HasColumnName("COLLECTION_ACTION_ID");
            entity.Property(e => e.CollectionSubId).HasColumnName("COLLECTION_SUB_ID");
            entity.Property(e => e.Notes).HasColumnName("NOTES");

            entity.HasOne(e => e.Sale)
                .WithMany(s => s.Payments)
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación con User como Collector
            entity.HasOne(e => e.Collector)
                .WithMany(u => u.PaymentsCollected)
                .HasForeignKey(e => e.CollectorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PaymentPhoto configuration
        modelBuilder.Entity<PaymentPhoto>(entity =>
        {
            entity.ToTable("PAYMENT_PHOTOS");
            entity.HasKey(e => e.PhotoId);
            entity.Property(e => e.PhotoId).HasColumnName("PHOTO_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.PaymentId).HasColumnName("PAYMENT_ID")
                .HasColumnType("NUMBER(10)");
            entity.Ignore(e => e.FileName); // Columna NO existe en Oracle
            entity.Property(e => e.FilePath).HasColumnName("FILE_PATH");
            entity.Property(e => e.PhotoType).HasColumnName("PHOTO_TYPE").HasMaxLength(50);
            entity.Property(e => e.GpsLatitude).HasColumnName("GPS_LATITUDE").HasPrecision(10, 7);
            entity.Property(e => e.GpsLongitude).HasColumnName("GPS_LONGITUDE").HasPrecision(10, 7);
            entity.Ignore(e => e.CapturedAt); // Columna NO existe en Oracle

            entity.HasOne(e => e.Payment)
                .WithMany(p => p.PaymentPhotos)
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SalePhoto configuration
        modelBuilder.Entity<SalePhoto>(entity =>
        {
            entity.ToTable("SALE_PHOTOS");
            entity.HasKey(e => e.PhotoId);
            entity.Property(e => e.PhotoId).HasColumnName("PHOTO_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.SaleId).HasColumnName("SALE_ID")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.PhotoType).HasColumnName("PHOTO_TYPE")
                .HasMaxLength(20).IsRequired();
            entity.Property(e => e.FilePath).HasColumnName("FILE_PATH")
                .HasMaxLength(500).IsRequired();
            entity.Property(e => e.ThumbnailPath).HasColumnName("THUMBNAIL_PATH")
                .HasMaxLength(500);
            entity.Property(e => e.GpsLatitude).HasColumnName("GPS_LATITUDE")
                .HasPrecision(10, 7);
            entity.Property(e => e.GpsLongitude).HasColumnName("GPS_LONGITUDE")
                .HasPrecision(10, 7);
            entity.Property(e => e.FileSize).HasColumnName("FILE_SIZE")
                .HasColumnType("NUMBER");
            entity.Property(e => e.UploadedAt).HasColumnName("UPLOADED_AT");
            entity.Property(e => e.UploadedBy).HasColumnName("UPLOADED_BY")
                .HasColumnType("NUMBER(10)");
            entity.Property(e => e.Synced).HasColumnName("SYNCED")
                .HasMaxLength(1)
                .HasConversion(
                    v => v ? "1" : "0",
                    v => v == "1");

            entity.HasOne(e => e.Sale)
                .WithMany(s => s.SalePhotos)
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UploadedByUser)
                .WithMany()
                .HasForeignKey(e => e.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
