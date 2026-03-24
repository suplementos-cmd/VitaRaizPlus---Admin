namespace VitaRaiz.Application.DTOs;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int SaleId { get; set; }
    public int CollectorId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Validation { get; set; }
    public string? Notes { get; set; }
}
