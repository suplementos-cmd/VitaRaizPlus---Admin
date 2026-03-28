namespace VitaRaiz.Application.DTOs;

public class SaleDto
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }  // Colonia/Dirección
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime SaleDate { get; set; }
    public DateTime? FirstPaymentDate { get; set; }  // Fecha del primer cobro aprobado
    public string Status { get; set; } = string.Empty;
    public string? PaymentTerms { get; set; }
    public string? ProductName { get; set; }  // Primer producto de la venta
    public string? SellerName { get; set; }   // Nombre del vendedor
}

/// <summary>
/// Full sale detail with items, payments, customer info and risk metrics.
/// Used as the pivot view for the "Gestión de Cartera" screen.
/// </summary>
public class SaleFullDto
{
    // Sale header
    public int SaleId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime SaleDate { get; set; }
    public string Status { get; set; } = string.Empty;
    
    // Payment terms (structured fields)
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

    // Risk & progress metrics (from Oracle functions)
    public string RiskStatus { get; set; } = "VERDE";
    public decimal PaymentPercentage { get; set; }
    public int DaysSinceLastPayment { get; set; }

    // Sale items
    public List<SaleDetailDto> Items { get; set; } = new();

    // Payments
    public List<PaymentDto> Payments { get; set; } = new();
}
