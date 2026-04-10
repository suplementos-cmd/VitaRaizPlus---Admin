namespace VitaRaiz.Application.DTOs;

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public bool IsGoldCustomer { get; set; }
    public bool IsBlacklisted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public int? CategoryId { get; set; }
    public string? Category { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class ZoneDto
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string? ZoneCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
    public int? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class RoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultThemeColor { get; set; }
}

public class SaleDetailDto
{
    public int SaleDetailId { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>
/// Tema resuelto para un perfil/rol específico. Devuelto por GET /api/catalogs/theme/role/{roleId}
/// </summary>
public class ProfileThemeDto
{
    public int ThemeId { get; set; }
    public int RoleId { get; set; }
    public string ThemeName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public string PrimaryColor { get; set; } = string.Empty;
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? BackgroundColor  { get; set; }
    public string? TextColor         { get; set; }
    public string? TitleTextColor    { get; set; }
    public string? FormTextColor     { get; set; }
    public string? MenuTextColor     { get; set; }
    public string? IconName          { get; set; }
}
