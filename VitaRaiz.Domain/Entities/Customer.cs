namespace VitaRaiz.Domain.Entities;

public class Customer
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhoneNumber => Phone;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? ZoneId { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public bool IsBlacklisted { get; set; } = false;
    public bool IsGoldCustomer { get; set; } = false;
    public DateTime RegisteredAt { get; set; } = DateTime.Now;
    public DateTime CreatedAt => RegisteredAt;
    public string? Notes { get; set; }
    
    // Navigation properties
    public Zone? Zone { get; set; }
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
