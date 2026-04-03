namespace VitaRaiz.Mobile.Models;

public class CatalogSaleStatus
{
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string ColorHex { get; set; } = "#999999";
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CatalogPaymentStatus
{
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string ColorHex { get; set; } = "#999999";
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public string StatusType { get; set; } = "WORKFLOW";
    public bool RequiresNote { get; set; }
    public bool RequiresPhoto { get; set; }
}

public class CatalogRiskStatus
{
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MinDays { get; set; }
    public int? MaxDays { get; set; }
    public string ColorHex { get; set; } = "#28A745";
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CatalogVisitAction
{
    public int StatusId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public bool RequiresNote { get; set; }
    public bool RequiresPhoto { get; set; }
    public string? Icon { get; set; }
    public string? ColorHex { get; set; }  // fue "Color" — renombrado para coincidir con JSON
    public int DisplayOrder { get; set; }
    /// <summary>NULL = acción raíz; STATUS_ID numérico del padre cuando es sub-acción</summary>
    public int? ParentActionId { get; set; }
}

/// <summary>Tema global de la aplicación (CATALOG_APP_THEMES).</summary>
public class CatalogAppTheme
{
    public string ThemeCode { get; set; } = string.Empty;
    public string ThemeName { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = "#28A745";
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
}

/// <summary>Tema específico por perfil/rol (PROFILE_THEMES).</summary>
public class ProfileThemeDto
{
    public int ThemeId { get; set; }
    public int RoleId { get; set; }
    public string ThemeName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public string PrimaryColor { get; set; } = "#28A745";
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public string? IconName { get; set; }
}

public class AllCatalogsResponse
{
    public List<CatalogSaleStatus> SaleStatuses { get; set; } = new();
    public List<CatalogPaymentStatus> PaymentStatuses { get; set; } = new();
    public List<CatalogRiskStatus> RiskStatuses { get; set; } = new();
    /// <summary>Tema resuelto (puede ser global o de rol según el contexto del token).</summary>
    public CatalogAppTheme? Theme { get; set; }
    public List<CatalogVisitAction> VisitActions { get; set; } = new();
}
