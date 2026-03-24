namespace VitaRaiz.Domain.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int SaleId { get; set; }
    public int CollectorId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public string Status { get; set; } = "pending";
    public string? Validation { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public Sale? Sale { get; set; }
    public User? Collector { get; set; }
    public ICollection<PaymentPhoto> PaymentPhotos { get; set; } = new List<PaymentPhoto>();
}
