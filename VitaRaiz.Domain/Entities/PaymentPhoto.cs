namespace VitaRaiz.Domain.Entities;

public class PaymentPhoto
{
    public int PhotoId { get; set; }
    public int PaymentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string PhotoType { get; set; } = string.Empty; // fachada, cliente, contrato, other
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.Now;
    
    // Navigation properties
    public Payment? Payment { get; set; }
}
