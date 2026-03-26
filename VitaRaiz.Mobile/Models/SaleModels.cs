namespace VitaRaiz.Mobile.Models;

/// <summary>
/// Enriched sale for list display. Mapped from API SaleDto.
/// Colors/labels resolved from catalogs, NOT hardcoded.
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
    public string Status { get; set; } = string.Empty;

    // Resolved from catalogs
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#999999";
    public string? StatusIcon { get; set; }
    public string RiskColor { get; set; } = "#28A745";

    // Calculated
    public double PaymentProgress => TotalAmount > 0
        ? (double)((TotalAmount - Balance) / TotalAmount)
        : 0;
    public string PaymentTerms { get; set; } = string.Empty;
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
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }

    // Customer info
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public decimal? CustomerGpsLatitude { get; set; }
    public decimal? CustomerGpsLongitude { get; set; }
    public bool CustomerIsGold { get; set; }
    public bool CustomerIsBlacklisted { get; set; }

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
    public List<SaleItemDetail> Items { get; set; } = new();
    public List<PaymentDetail> Payments { get; set; } = new();

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
