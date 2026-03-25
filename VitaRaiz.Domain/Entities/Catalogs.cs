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
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public string? Icon { get; set; }
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

public class CatalogAppTheme
{
    public string ThemeCode { get; set; } = string.Empty;
    public string ThemeName { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = string.Empty;
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
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
    public string ActionCode { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? ColorHex { get; set; }
    public bool RequiresNote { get; set; }
    public bool RequiresPhoto { get; set; }
    public int DisplayOrder { get; set; }
}
