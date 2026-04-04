using System.Text.Json.Serialization;

namespace VitaRaiz.WebPortal.Models;

/// <summary>Returned by GET api/auth/me</summary>
public class AuthMeDto
{
    public int    UserId   { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role     { get; set; } = string.Empty;
}

public record LoginRequest(string Username, string Password);

public class AuthResponse
{
    public bool   Success  { get; set; } = true;   // not returned by API; defaults true
    public string Message  { get; set; } = string.Empty;
    public string Error    { get; set; } = string.Empty;  // API 401 body: { "error": "..." }
    public string Token    { get; set; } = string.Empty;
    public int    UserId   { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role     { get; set; } = string.Empty;
    public int    RoleId   { get; set; }
}

public class CurrentUser
{
    public int    UserId   { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role     { get; set; } = string.Empty;
    public int    RoleId   { get; set; }
    public string Token    { get; set; } = string.Empty;

    public bool IsAdmin      => Role.Contains("admin",      StringComparison.OrdinalIgnoreCase);
    public bool IsSupervisor => Role.Contains("supervisor", StringComparison.OrdinalIgnoreCase);
    public bool IsCobrador   => Role.Contains("cobrador",   StringComparison.OrdinalIgnoreCase);
    public bool IsVendedor   => Role.Contains("vendedor",   StringComparison.OrdinalIgnoreCase);
    public bool IsManagerOrAbove => IsAdmin || IsSupervisor;
}

public class ZoneDto
{
    public int    ZoneId      { get; set; }
    public string ZoneName    { get; set; } = string.Empty;
    public string ZoneCode    { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool   IsActive    { get; set; } = true;
}

public class ProductDto
{
    public int     ProductId   { get; set; }
    public string  ProductName { get; set; } = string.Empty;
    public string  Description { get; set; } = string.Empty;
    public decimal UnitPrice   { get; set; }
    public int     Stock       { get; set; }
    public string  Category    { get; set; } = string.Empty;
    public bool    IsActive    { get; set; } = true;
}

public class RoleDto
{
    public int    RoleId      { get; set; }
    public string RoleName    { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultThemeColor   { get; set; } = "#607D8B";
}

public class UserDto
{
    public int    UserId   { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public int    RoleId   { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int?   ZoneId   { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public bool   IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class CustomerDto
{
    public int      CustomerId     { get; set; }
    public string   CustomerName   { get; set; } = string.Empty;
    public string?  PhoneNumber    { get; set; }
    public string?  Email          { get; set; }
    public string?  Address        { get; set; }
    public int?     ZoneId         { get; set; }
    public string?  ZoneName       { get; set; }
    // API sends decimal coordinates — must match to avoid JSON deserialization failure
    public decimal? GpsLatitude    { get; set; }
    public decimal? GpsLongitude   { get; set; }
    public bool     IsGoldCustomer { get; set; }
    public bool     IsBlacklisted  { get; set; }
    public DateTime? RegisteredAt  { get; set; }
    public string?  Notes          { get; set; }
}

public class SaleDto
{
    public int      SaleId          { get; set; }
    public int      CustomerId      { get; set; }
    public string   CustomerName    { get; set; } = string.Empty;
    public int      SellerId        { get; set; }
    public string   SellerName      { get; set; } = string.Empty;
    public decimal  TotalAmount     { get; set; }
    public decimal  PaidAmount      { get; set; }
    public decimal  Balance         { get; set; }
    public DateTime SaleDate        { get; set; }
    public string   Status          { get; set; } = string.Empty;
    public string   PaymentTerm     { get; set; } = string.Empty;
    public int      PaymentTermDays { get; set; }
    public string   CollectionDay   { get; set; } = string.Empty;
    public decimal  DownPayment     { get; set; }
    public string   Notes           { get; set; } = string.Empty;
    public string   RiskStatus      { get; set; } = "VERDE";
    public double   PaymentProgress => TotalAmount > 0 ? (double)(PaidAmount / TotalAmount) : 0;
}

public class SaleDetailDto
{
    public int     DetailId    { get; set; }
    public int     SaleId      { get; set; }
    public int     ProductId   { get; set; }
    public string  ProductName { get; set; } = string.Empty;
    public int     Quantity    { get; set; }
    public decimal UnitPrice   { get; set; }
    public decimal Subtotal    { get; set; }
}

public class SaleFullDto : SaleDto
{
    public string?  CustomerPhone        { get; set; }
    public string?  CustomerAddress      { get; set; }
    // API property names differ from WebPortal names — map with JsonPropertyName
    [JsonPropertyName("customerIsGold")]        public bool     IsGoldCustomer       { get; set; }
    [JsonPropertyName("customerIsBlacklisted")] public bool     IsBlacklisted        { get; set; }
    [JsonPropertyName("zoneName")]              public string   CustomerZone         { get; set; } = string.Empty;
    public int?     AssignedCollectorId  { get; set; }
    public string?  CollectorName        { get; set; }
    public decimal  PaymentPercentage    { get; set; }
    public int      DaysSinceLastPayment { get; set; }
    public DateTime? FirstCollectionDate { get; set; }
    public List<SaleDetailDto> Items    { get; set; } = new();
    public List<PaymentDto>    Payments { get; set; } = new();
}

public class PaymentDto
{
    public int      PaymentId     { get; set; }
    public int      SaleId        { get; set; }
    public int      CollectorId   { get; set; }
    public string   CustomerName  { get; set; } = string.Empty;
    public string   CollectorName { get; set; } = string.Empty;
    public decimal  Amount        { get; set; }
    public DateTime PaymentDate   { get; set; }
    public decimal? GpsLatitude   { get; set; }
    public decimal? GpsLongitude  { get; set; }
    public string   Status        { get; set; } = string.Empty;
    public string   Validation    { get; set; } = string.Empty;
    public string   Notes         { get; set; } = string.Empty;
    public bool     HasGps        => GpsLatitude.HasValue && GpsLongitude.HasValue;
}

public class ProfileThemeDto
{
    public int    ThemeId        { get; set; }
    public int    RoleId         { get; set; }
    public string ThemeName      { get; set; } = string.Empty;
    public string PrimaryColor   { get; set; } = "#607D8B";
    public string SecondaryColor { get; set; } = "#B0BEC5";
    public string AccentColor    { get; set; } = "#ECEFF1";
}

public class CatalogAppSetting
{
    public string SettingKey   { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string SettingType  { get; set; } = "string";
    public string Description  { get; set; } = string.Empty;
    public string Category     { get; set; } = string.Empty;
}

public class PermissionsResponse
{
    public List<string> Permissions { get; set; } = new();
}

public class DashboardStats
{
    public int     TotalSalesToday     { get; set; }
    public decimal TotalCollectedToday { get; set; }
    public decimal TotalPending        { get; set; }
    public int     ActiveCustomers     { get; set; }
    public int     PendingApprovals    { get; set; }
    public int     OverdueAccounts     { get; set; }
    public decimal MonthlyRevenue      { get; set; }
}
