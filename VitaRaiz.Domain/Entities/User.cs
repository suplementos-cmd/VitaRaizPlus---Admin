namespace VitaRaiz.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int? ZoneId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLogin { get; set; }
    
    // Navigation properties
    public Role? Role { get; set; }
    public Zone? Zone { get; set; }
    public ICollection<Sale> SalesCreated { get; set; } = new List<Sale>();
    public ICollection<Payment> PaymentsCollected { get; set; } = new List<Payment>();
}
