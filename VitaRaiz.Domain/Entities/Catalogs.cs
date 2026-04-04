namespace VitaRaiz.Domain.Entities;

public class CatalogSaleStatus
{
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public string? Icon { get; set; }
}

public class CatalogPaymentStatus
{
    /// <summary>ID numérico estable — preferir sobre STATUS_NAME en comparaciones de código</summary>
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public string? Icon { get; set; }
    /// <summary>WORKFLOW = estado de aprobación | VISIT_ACTION = acción de visita del cobrador</summary>
    public string StatusType { get; set; } = "WORKFLOW";
    public bool RequiresNote { get; set; }
    public bool RequiresPhoto { get; set; }
    /// <summary>NULL = estado raíz; STATUS_ID numérico del padre (solo VISIT_ACTION usa jerarquía)</summary>
    public int? ParentStatusId { get; set; }
}

public class CatalogRiskStatus
{
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MinDays { get; set; }
    public int MaxDays { get; set; }
    public string ColorHex { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Tema global de la aplicación (CATALOG_APP_THEMES)
/// </summary>
public class CatalogAppTheme
{
    public string ThemeCode { get; set; } = string.Empty;
    public string ThemeName { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = string.Empty;
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// Tema específico por rol (PROFILE_THEMES)
/// </summary>
public class ProfileTheme
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

public class CatalogNotificationTemplate
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string MessageBody { get; set; } = string.Empty;
    public string? Variables { get; set; }
}

public class CatalogAppSetting
{
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string? SettingType { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
}

public class CatalogVisitAction
{
    /// <summary>ID numérico estable</summary>
    public int StatusId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? ColorHex { get; set; }
    public bool RequiresNote { get; set; }
    public bool RequiresPhoto { get; set; }
    public int DisplayOrder { get; set; }
    /// <summary>NULL = acción raíz; STATUS_ID numérico del padre cuando es sub-acción</summary>
    public int? ParentStatusId { get; set; }
}
