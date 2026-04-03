namespace VitaRaiz.Domain.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Color primario por defecto para este rol (columna DEFAULT_THEME_COLOR)</summary>
    public string? DefaultThemeColor { get; set; }
    /// <summary>Color claro por defecto para este rol (columna DEFAULT_THEME_LIGHT)</summary>
    public string? DefaultThemeLight { get; set; }
    /// <summary>Color más claro por defecto para este rol (columna DEFAULT_THEME_LIGHTER)</summary>
    public string? DefaultThemeLighter { get; set; }
    
    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}
