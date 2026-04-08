namespace VitaRaiz.Mobile.Models;

/// <summary>
/// Enriched sale for list display. Mapped from API SaleDto.
/// Colors/labels resolved from catalogs, NOT hardcoded.
/// ALL properties are fully materialized (no computed), thread-safe, and null-safe for XAML binding.
/// </summary>
public class SaleListItem
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime SaleDate { get; set; }
    public DateTime? FirstPaymentDate { get; set; }  // Fecha del primer cobro
    public string Status { get; set; } = string.Empty;

    // Resolved from catalogs
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#999999";
    public string? StatusIcon { get; set; }
    public string RiskColor { get; set; } = "#28A745";

    // CRITICAL: Use simple stored property instead of computed property
    // Computed properties can cause crashes during XAML binding in WinUI
    public double PaymentProgressValue { get; set; }
    
    public string PaymentTerms { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public bool HasThumbnail { get; set; }
    
    // Nuevos campos para tarjetas de venta
    public string? ProductName { get; set; }
    public string? SellerName { get; set; }
    
    /// <summary>
    /// Factory method to create safe SaleListItem with all calculations pre-computed
    /// </summary>
    public static SaleListItem CreateSafe(int saleId, string customerName, decimal totalAmount,
        decimal paidAmount, decimal balance, DateTime saleDate, string status,
        string statusLabel, string statusColor, string? statusIcon, string paymentTerms,
        string? thumbnailPath = null, string? customerAddress = null, 
        string? productName = null, string? sellerName = null, DateTime? firstPaymentDate = null)
    {
        // Pre-calculate PaymentProgress safely
        double progress = 0.0;
        try
        {
            if (totalAmount > 0)
            {
                progress = (double)((totalAmount - balance) / totalAmount);
                progress = Math.Min(Math.Max(progress, 0.0), 1.0);
            }
        }
        catch
        {
            progress = 0.0;
        }

        return new SaleListItem
        {
            SaleId = saleId,
            CustomerName = customerName ?? string.Empty,
            CustomerAddress = customerAddress,
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,
            Balance = balance,
            SaleDate = saleDate,
            FirstPaymentDate = firstPaymentDate,
            Status = status ?? string.Empty,
            StatusLabel = statusLabel ?? string.Empty,
            StatusColor = statusColor ?? "#999999",
            StatusIcon = statusIcon,
            PaymentTerms = paymentTerms ?? string.Empty,
            PaymentProgressValue = progress,
            ThumbnailPath = thumbnailPath,
            HasThumbnail = !string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath),
            ProductName = productName,
            SellerName = sellerName
        };
    }
}

/// <summary>
/// Full sale detail for the pivot "Gestión de Cartera" view.
/// Includes items, payments, customer info, and risk metrics.
/// </summary>
public class SaleFullDetail
{
    // Sale header
    public int SaleId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime SaleDate { get; set; }
    public string Status { get; set; } = string.Empty;
    
    // Structured payment fields
    public string? PaymentTerm { get; set; } // SEMANAL, QUINCENAL, MENSUAL, CONTADO
    public string? CollectionDay { get; set; } // LUN, MAR, MIE, JUE, VIE, SAB, DOM
    public DateTime? FirstCollectionDate { get; set; } // Fecha del primer cobro programado
    public decimal DownPayment { get; set; } // Enganche/pago inicial
    
    // Legacy/Additional
    public string? PaymentTerms { get; set; } // Legacy text field (e.g., "7 dias")
    public string? Notes { get; set; } // Additional notes only

    // Customer info
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public decimal? CustomerGpsLatitude { get; set; }
    public decimal? CustomerGpsLongitude { get; set; }
    public bool CustomerIsGold { get; set; }
    public bool CustomerIsBlacklisted { get; set; }
    public string? ZoneName { get; set; }

    // Seller / Collector
    public int SellerId { get; set; }
    public string? SellerName { get; set; }
    public int? AssignedCollectorId { get; set; }
    public string? CollectorName { get; set; }

    // Risk & progress metrics
    public string RiskStatus { get; set; } = "VERDE";
    public decimal PaymentPercentage { get; set; }
    public int DaysSinceLastPayment { get; set; }

    // Details
    public List<SaleItemDetail>  Items   { get; set; } = new();
    public List<PaymentDetail>   Payments { get; set; } = new();
    public List<SalePhotoDto>    Photos   { get; set; } = new();

    // Resolved from catalogs (set by CatalogService)
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#999999";
    public string RiskColor { get; set; } = "#28A745";
    public string RiskLabel { get; set; } = string.Empty;

    // Calculated
    public double PaymentProgress => TotalAmount > 0
        ? (double)(PaidAmount / TotalAmount)
        : 0;
}

public class SaleItemDetail
{
    public int SaleDetailId { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class PaymentDetail
{
    public int PaymentId { get; set; }
    public int SaleId { get; set; }
    public int CollectorId { get; set; }
    public string? CollectorName { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }

    // Pending amount (calculated: total - paid at that point)
    public decimal PendingAmount { get; set; }

    // Resolved from catalogs
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#999999";
}
