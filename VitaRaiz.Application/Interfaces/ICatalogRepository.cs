using VitaRaiz.Domain.Entities;

namespace VitaRaiz.Application.Interfaces;

public interface ICatalogRepository
{
    Task<List<CatalogSaleStatus>> GetSaleStatusesAsync();
    Task<List<CatalogPaymentStatus>> GetPaymentStatusesAsync();
    Task<List<CatalogRiskStatus>> GetRiskStatusesAsync();
    Task<CatalogAppTheme?> GetActiveThemeAsync();
    /// <summary>Obtiene el tema configurado para un rol específico desde PROFILE_THEMES.</summary>
    Task<ProfileTheme?> GetThemeByRoleAsync(int roleId);
    /// <summary>Guarda (insert o update) el tema de un rol en PROFILE_THEMES.</summary>
    Task SaveThemeByRoleAsync(ProfileTheme theme);
    Task<List<CatalogNotificationTemplate>> GetNotificationTemplatesAsync(string? templateType = null);
    Task<List<CatalogAppSetting>> GetAppSettingsAsync(string? category = null, bool isPublic = true);
    Task<List<CatalogVisitAction>> GetVisitActionsAsync();
    Task<string?> GetSettingValueAsync(string settingKey);
}
