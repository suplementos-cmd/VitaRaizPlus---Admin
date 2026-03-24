namespace VitaRaiz.Domain.Entities;

public class SaleDetail
{
    public int DetailId { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    
    // Navigation properties
    public Sale? Sale { get; set; }
    public Product? Product { get; set; }
}
