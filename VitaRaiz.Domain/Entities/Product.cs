namespace VitaRaiz.Domain.Entities;

public class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal UnitPrice => Price;
    public int Stock { get; set; } = 0;
    public int? CategoryId { get; set; }
    public string? Category { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
}
