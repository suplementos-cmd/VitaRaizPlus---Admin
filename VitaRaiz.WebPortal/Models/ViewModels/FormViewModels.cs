namespace VitaRaiz.WebPortal.Models.ViewModels;

public class LoginViewModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Usuario requerido")]
    public string Username { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Contraseña requerida")]
    public string Password { get; set; } = string.Empty;
}

public class UserFormViewModel
{
    public int    UserId   { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string FullName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email    { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public int    RoleId   { get; set; }

    public int?   ZoneId   { get; set; }
    public bool   IsActive { get; set; } = true;

    // Only required on create
    [System.ComponentModel.DataAnnotations.MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
    public string? Password { get; set; }
}

public class CustomerFormViewModel
{
    public int    CustomerId    { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Nombre requerido")]
    public string CustomerName  { get; set; } = string.Empty;

    public string PhoneNumber   { get; set; } = string.Empty;
    public string Email         { get; set; } = string.Empty;
    public string Address       { get; set; } = string.Empty;
    public int?   ZoneId        { get; set; }
    public bool   IsGoldCustomer { get; set; }
    public bool   IsBlacklisted  { get; set; }
    public string Notes          { get; set; } = string.Empty;
}

public class ProductFormViewModel
{
    public int     ProductId   { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string  ProductName { get; set; } = string.Empty;

    public string  Description { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(0.01, double.MaxValue, ErrorMessage = "Precio debe ser positivo")]
    public decimal UnitPrice   { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
    public int     Stock       { get; set; }

    public string  Category    { get; set; } = string.Empty;
    public bool    IsActive    { get; set; } = true;
}

public class ZoneFormViewModel
{
    public int    ZoneId      { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string ZoneName    { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string ZoneCode    { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public bool   IsActive    { get; set; } = true;
}
