namespace VitaRaiz.Domain.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int SaleId { get; set; }
    public int CollectorId { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Fecha/hora de registro — equivale a CREATED_AT</summary>
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    /// <summary>Estado de aprobación: FK numérica → CATALOG_PAYMENT_STATUSES.STATUS_ID (1=PENDING, 2=CONFIRMADO, 3=RECHAZADO)</summary>
    public int StatusId { get; set; } = 1;
    public string? Validation { get; set; }
    public string? Notes { get; set; }
    /// <summary>Acción de visita del cobrador: FK numérica → CATALOG_PAYMENT_STATUSES.STATUS_ID (VISIT_ACTION raíz)</summary>
    public int? CollectionActionId { get; set; }
    /// <summary>Sub-acción/detalle de visita: FK numérica → STATUS_ID (hijo de CollectionActionId)</summary>
    public int? CollectionSubId { get; set; }
    /// <summary>Fecha/hora de la última modificación (aprobación, rechazo)</summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>Usuario que realizó la última modificación (FK → USERS.USER_ID)</summary>
    public int? UpdatedBy { get; set; }

    // Navigation properties
    public Sale? Sale { get; set; }
    public User? Collector { get; set; }
    public ICollection<PaymentPhoto> PaymentPhotos { get; set; } = new List<PaymentPhoto>();
}
