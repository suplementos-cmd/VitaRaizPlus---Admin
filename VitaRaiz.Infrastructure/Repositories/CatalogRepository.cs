using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Domain.Entities;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class CatalogRepository : BaseOracleRepository, ICatalogRepository
{
    public CatalogRepository(VitaRaizDbContext context) : base(context)
    {
    }

    public async Task<List<CatalogSaleStatus>> GetSaleStatusesAsync()
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_sale_statuses");
        
        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);
        
        await command.ExecuteNonQueryAsync();
        
        var statuses = new List<CatalogSaleStatus>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();
        
        while (await reader.ReadAsync())
        {
            statuses.Add(new CatalogSaleStatus
            {
                StatusCode = reader.GetString("statusCode"),
                StatusName = reader.GetString("statusName"),
                Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                DisplayOrder = reader.GetInt32("displayOrder"),
                ColorHex = reader.IsDBNull("colorHex") ? null : reader.GetString("colorHex"),
                Icon = reader.IsDBNull("icon") ? null : reader.GetString("icon")
            });
        }
        
        return statuses;
    }

    public async Task<List<CatalogPaymentStatus>> GetPaymentStatusesAsync()
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_payment_statuses");
        
        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);
        
        await command.ExecuteNonQueryAsync();
        
        var statuses = new List<CatalogPaymentStatus>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();
        
        while (await reader.ReadAsync())
        {
            statuses.Add(new CatalogPaymentStatus
            {
                StatusCode = reader.GetString("statusCode"),
                StatusName = reader.GetString("statusName"),
                Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                DisplayOrder = reader.GetInt32("displayOrder"),
                ColorHex = reader.IsDBNull("colorHex") ? null : reader.GetString("colorHex"),
                Icon = reader.IsDBNull("icon") ? null : reader.GetString("icon")
            });
        }
        
        return statuses;
    }

    public async Task<List<CatalogRiskStatus>> GetRiskStatusesAsync()
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_risk_statuses");
        
        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);
        
        await command.ExecuteNonQueryAsync();
        
        var statuses = new List<CatalogRiskStatus>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();
        
        while (await reader.ReadAsync())
        {
            statuses.Add(new CatalogRiskStatus
            {
                StatusCode = reader.GetString("statusCode"),
                StatusName = reader.GetString("statusName"),
                Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                MinDays = reader.GetInt32("minDays"),
                MaxDays = reader.GetInt32("maxDays"),
                ColorHex = reader.GetString("colorHex"),
                Icon = reader.IsDBNull("icon") ? null : reader.GetString("icon"),
                DisplayOrder = reader.GetInt32("displayOrder")
            });
        }
        
        return statuses;
    }

    public async Task<CatalogAppTheme?> GetActiveThemeAsync()
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_active_theme");
        
        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);
        
        await command.ExecuteNonQueryAsync();
        
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();
        
        if (await reader.ReadAsync())
        {
            return new CatalogAppTheme
            {
                ThemeCode = reader.GetString("themeCode"),
                ThemeName = reader.GetString("themeName"),
                PrimaryColor = reader.GetString("primaryColor"),
                SecondaryColor = reader.IsDBNull("secondaryColor") ? null : reader.GetString("secondaryColor"),
                AccentColor = reader.IsDBNull("accentColor") ? null : reader.GetString("accentColor"),
                BackgroundColor = reader.IsDBNull("backgroundColor") ? null : reader.GetString("backgroundColor"),
                TextColor = reader.IsDBNull("textColor") ? null : reader.GetString("textColor")
            };
        }
        
        return null;
    }

    public async Task<List<CatalogNotificationTemplate>> GetNotificationTemplatesAsync(string? templateType = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_notification_templates");
        
        AddInputParameter(command, "p_template_type", templateType);
        
        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);
        
        await command.ExecuteNonQueryAsync();
        
        var templates = new List<CatalogNotificationTemplate>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();
        
        while (await reader.ReadAsync())
        {
            templates.Add(new CatalogNotificationTemplate
            {
                TemplateCode = reader.GetString("templateCode"),
                TemplateName = reader.GetString("templateName"),
                TemplateType = reader.GetString("templateType"),
                Subject = reader.IsDBNull("subject") ? null : reader.GetString("subject"),
                MessageBody = reader.GetString("messageBody"),
                Variables = reader.IsDBNull("variables") ? null : reader.GetString("variables")
            });
        }
        
        return templates;
    }

    public async Task<List<CatalogAppSetting>> GetAppSettingsAsync(string? category = null, bool isPublic = true)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_app_settings");
        
        AddInputParameter(command, "p_category", category);
        AddInputParameter(command, "p_is_public", isPublic ? 1 : 0);
        
        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);
        
        await command.ExecuteNonQueryAsync();
        
        var settings = new List<CatalogAppSetting>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();
        
        while (await reader.ReadAsync())
        {
            settings.Add(new CatalogAppSetting
            {
                SettingKey = reader.GetString("settingKey"),
                SettingValue = reader.GetString("settingValue"),
                SettingType = reader.IsDBNull("settingType") ? null : reader.GetString("settingType"),
                Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                Category = reader.IsDBNull("category") ? null : reader.GetString("category")
            });
        }
        
        return settings;
    }

    public async Task<List<CatalogVisitAction>> GetVisitActionsAsync()
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_visit_actions");
        
        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);
        
        await command.ExecuteNonQueryAsync();
        
        var actions = new List<CatalogVisitAction>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();
        
        while (await reader.ReadAsync())
        {
            actions.Add(new CatalogVisitAction
            {
                ActionCode = reader.GetString("actionCode"),
                ActionName = reader.GetString("actionName"),
                Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                Icon = reader.IsDBNull("icon") ? null : reader.GetString("icon"),
                ColorHex = reader.IsDBNull("colorHex") ? null : reader.GetString("colorHex"),
                RequiresNote = reader.GetInt32("requiresNote") == 1,
                RequiresPhoto = reader.GetInt32("requiresPhoto") == 1,
                DisplayOrder = reader.GetInt32("displayOrder")
            });
        }
        
        return actions;
    }

    public async Task<string?> GetSettingValueAsync(string settingKey)
    {
        var connection = await GetOpenConnectionAsync();
        var parameters = new Dictionary<string, object> { { "p_setting_key", settingKey } };
        
        using var command = CreatePackageStringFunctionCommand(connection, "fn_get_setting_value", parameters);
        await command.ExecuteNonQueryAsync();
        
        var resultParam = (OracleParameter)command.Parameters["result"];
        var resultValue = resultParam.Value;
        
        if (resultValue == null || resultValue == DBNull.Value)
            return null;
            
        return resultValue.ToString();
    }
}
