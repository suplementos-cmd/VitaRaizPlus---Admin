namespace VitaRaiz.Application.DTOs;

public class SaleDto
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime SaleDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentTerms { get; set; }
}
