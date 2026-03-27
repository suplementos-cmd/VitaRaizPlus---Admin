namespace VitaRaiz.Domain.Entities;

public class Sale
{
    public int SaleId { get; set; }
    public int CustomerId { get; set; }
    public int SellerId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public string? PaymentTerms { get; set; }
    public int? PaymentTermDays { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "active";
    public int? AssignedCollectorId { get; set; }
    public string? Notes { get; set; }
    public DateTime DueDate => SaleDate.AddDays(PaymentTermDays ?? 30);
    
    // Navigation properties
    public Customer? Customer { get; set; }
    public User? Seller { get; set; }
    public User? AssignedCollector { get; set; }
    public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();    public ICollection<SalePhoto> SalePhotos { get; set; } = new List<SalePhoto>();}
