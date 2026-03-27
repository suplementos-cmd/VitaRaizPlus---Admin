using SQLite;

namespace VitaRaiz.Mobile.Data;

[Table("SyncQueue")]
public class SyncQueueItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string EntityType { get; set; } = string.Empty; // "Payment", "Sale", "Customer"
    public string Operation { get; set; } = string.Empty; // "Create", "Update", "Delete"
    public string JsonData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSynced { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
}

[Table("LocalCustomers")]
public class LocalCustomer
{
    [PrimaryKey]
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? ZoneId { get; set; }
    public string? GpsLatitude { get; set; }
    public string? GpsLongitude { get; set; }
    public bool IsGoldCustomer { get; set; }
    public bool IsBlacklisted { get; set; }
    public DateTime LastSync { get; set; } = DateTime.UtcNow;
}

[Table("LocalSales")]
public class LocalSale
{
    [PrimaryKey]
    public int SaleId { get; set; }

    public int CustomerId { get; set; }
    public int SellerId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string Status { get; set; } = string.Empty; // Active, Completed, Overdue
    public int PaymentTermDays { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
    public DateTime LastSync { get; set; } = DateTime.UtcNow;
}

[Table("LocalPayments")]
public class LocalPayment
{
    [PrimaryKey, AutoIncrement]
    public int LocalPaymentId { get; set; }

    public int? PaymentId { get; set; } // Null si aún no se sincroniza
    public int SaleId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public int CollectedBy { get; set; }
    public string? GpsLatitude { get; set; }
    public string? GpsLongitude { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Synced, Error
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSync { get; set; } = DateTime.UtcNow;
}

[Table("LocalPaymentPhotos")]
public class LocalPaymentPhoto
{
    [PrimaryKey, AutoIncrement]
    public int LocalPhotoId { get; set; }

    public int? PhotoId { get; set; } // Null si aún no se sincroniza
    public int LocalPaymentId { get; set; }
    public string LocalPath { get; set; } = string.Empty;
    public string PhotoType { get; set; } = string.Empty; // Fachada, Cliente, Contrato
    public string? GpsLatitude { get; set; }
    public string? GpsLongitude { get; set; }
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;
    public bool IsSynced { get; set; } = false;
}

[Table("LocalSalePhotos")]
public class LocalSalePhoto
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int SaleId { get; set; }
    public string PhotoType { get; set; } = string.Empty; // Fachada, Cliente, Contrato, Adicional
    public string LocalPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
