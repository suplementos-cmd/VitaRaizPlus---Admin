using VitaRaiz.Domain.Entities;

namespace VitaRaiz.Application.Interfaces;

public interface ICatalogRepository
{
    Task<List<CatalogSaleStatus>> GetSaleStatusesAsync();
    Task<List<CatalogPaymentStatus>> GetPaymentStatusesAsync();
    Task<List<CatalogRiskStatus>> GetRiskStatusesAsync();
    Task<CatalogAppTheme?> GetActiveThemeAsync();
    Task<List<CatalogNotificationTemplate>> GetNotificationTemplatesAsync(string? templateType = null);
    Task<List<CatalogAppSetting>> GetAppSettingsAsync(string? category = null, bool isPublic = true);
    Task<List<CatalogVisitAction>> GetVisitActionsAsync();
    Task<string?> GetSettingValueAsync(string settingKey);
}
