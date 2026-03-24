namespace VitaRaiz.Domain.Entities;

public class Zone
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
